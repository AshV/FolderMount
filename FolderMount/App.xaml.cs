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
                // Another instance is already running — it has been signalled
                // to show its window. Exit this second instance immediately.
                Shutdown();
                return;
            }

            bool isStartupRun = e.Args.Contains("/startup", StringComparer.OrdinalIgnoreCase);

            _tray = new TrayManager(
                onOpen:       ShowMainWindow,
                onMountAll:   () => { 
                    var mappings = Services.MappingStore.Load();
                    Services.SubstService.MountAll(mappings);
                    foreach(var m in mappings) m.MountOnLoad = true;
                    Services.MappingStore.Save(mappings);
                    RefreshMainIfOpen(); 
                },
                onUnmountAll: () => { 
                    var mappings = Services.MappingStore.Load();
                    Services.SubstService.UnmountAll(mappings);
                    foreach(var m in mappings) m.MountOnLoad = false;
                    Services.MappingStore.Save(mappings);
                    RefreshMainIfOpen(); 
                },
                onSettings:   ShowSettings,
                onExit:       ExitApp
            );

            System.Threading.Tasks.Task.Run(() => 
            {
                var loadedMappings = Services.MappingStore.Load();
                Services.SubstService.MountAll(loadedMappings.Where(m => m.MountOnLoad));
                Services.SubstService.UnmountAll(loadedMappings.Where(m => !m.MountOnLoad));
                
                Application.Current.Dispatcher.Invoke(() => RefreshMainIfOpen());
            });

            if (!isStartupRun)
                ShowMainWindow();
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

        private void ShowSettings()
        {
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
