using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace FolderMount
{
    /// <summary>
    /// App entry point — handles startup args, creates the tray icon, manages window lifecycle.
    /// System.Drawing and System.Windows.Forms usage is in TrayManager.cs (not the XAML partial).
    /// </summary>
    public partial class App : Application
    {
        private TrayManager _tray;
        private MainWindow  _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            bool isStartupRun = e.Args.Contains("/startup", StringComparer.OrdinalIgnoreCase);

            _tray = new TrayManager(
                onOpen:       ShowMainWindow,
                onMountAll:   () => { Services.SubstService.MountAll(Services.MappingStore.Load()); RefreshMainIfOpen(); },
                onUnmountAll: () => { Services.SubstService.UnmountAll(Services.MappingStore.Load()); RefreshMainIfOpen(); },
                onSettings:   ShowSettings,
                onExit:       ExitApp
            );

            if (isStartupRun)
                Services.SubstService.MountAll(Services.MappingStore.Load());
            else
                ShowMainWindow();
        }

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
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _tray?.Dispose();
            base.OnExit(e);
        }
    }
}
