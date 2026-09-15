using System;
using System.Linq;
using System.Windows;

namespace FolderMount
{
    /// <summary>
    /// App entry point — handles startup args, enforces single instance,
    /// creates the tray icon, and manages window lifecycle.
    /// </summary>
    public partial class App : Application
    {
        private TrayManager _tray;
        private MainWindow  _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ── Single-instance guard ─────────────────────────────────────────
            if (!SingleInstance.TryClaimInstance())
            {
                Shutdown();
                return;
            }

            bool isStartupRun = e.Args.Contains("/startup", StringComparer.OrdinalIgnoreCase);

            _tray = new TrayManager(
                onOpen:       ShowMainWindow,
                onMountAll:   HandleMountAll,
                onUnmountAll: HandleUnmountAll,
                onSettings:   ShowSettings,
                onExit:       ExitApp
            );

            // Mount/unmount on a background thread for fast startup
            System.Threading.Tasks.Task.Run(() =>
            {
                var loadedMappings = Services.MappingStore.Load();
                Services.SubstService.MountAll(loadedMappings.Where(m => m.MountOnLoad));
                Services.SubstService.UnmountAll(loadedMappings.Where(m => !m.MountOnLoad));

                Current.Dispatcher.Invoke(RefreshMainIfOpen);
            });

            if (!isStartupRun)
                ShowMainWindow();
        }

        // ── Tray handlers ─────────────────────────────────────────────────────
        // Delegate to the ViewModel when the main window is open so state stays
        // in sync. Fall back to loading from disk when no window exists.

        private void HandleMountAll()
        {
            if (_mainWindow?.ViewModel != null)
            {
                _mainWindow.ViewModel.DoMountAll();
                return;
            }

            var mappings = Services.MappingStore.Load();
            Services.SubstService.MountAll(mappings);
            foreach (var m in mappings) m.MountOnLoad = true;
            Services.MappingStore.Save(mappings);
        }

        private void HandleUnmountAll()
        {
            if (_mainWindow?.ViewModel != null)
            {
                _mainWindow.ViewModel.DoUnmountAll();
                return;
            }

            var mappings = Services.MappingStore.Load();
            Services.SubstService.UnmountAll(mappings);
            foreach (var m in mappings) m.MountOnLoad = false;
            Services.MappingStore.Save(mappings);
        }

        // ── Window management ─────────────────────────────────────────────────

        internal void ShowMainWindow()
        {
            if (_mainWindow == null || !_mainWindow.IsLoaded)
            {
                _mainWindow = new MainWindow();
                _mainWindow.Closed += (_, __) => _mainWindow = null;
            }
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }

        internal void ShowSettings()
        {
            // Ensure the main window is available as Owner before opening settings
            ShowMainWindow();
            var win = new SettingsWindow { Owner = _mainWindow };
            win.ShowDialog();
        }

        private void RefreshMainIfOpen()
            => _mainWindow?.ViewModel?.RefreshStatus();

        private void ExitApp()
        {
            _tray?.Dispose();
            SingleInstance.Release();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SingleInstance.Release();
            _tray?.Dispose();
            base.OnExit(e);
        }
    }
}
