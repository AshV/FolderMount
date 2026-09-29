using System;
using System.Diagnostics;
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

        internal TrayManager Tray => _tray;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ── Single-instance guard ─────────────────────────────────────────
            SingleInstance.ArgsReceived += HandleArgs;
            if (!SingleInstance.TryClaimInstance(e.Args))
            {
                Shutdown();
                return;
            }

            bool isStartupRun = e.Args.Contains("/startup", StringComparer.OrdinalIgnoreCase);

            // ── Windows Startup sync (enabled by default) ────────────────────
            Services.StartupService.SyncStartupState();

            // ── Theme initialization (follows Windows default setting) ──────
            Services.ThemeService.Initialize();

            _tray = new TrayManager(
                onOpen:         ShowMainWindow,
                onMountAll:     HandleMountAll,
                onUnmountAll:   HandleUnmountAll,
                onActualDrives: ShowActualDrives,
                onSettings:     ShowSettings,
                onExit:         ExitApp
            );

            // Mount/unmount on a background thread for fast startup
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var loadedMappings = Services.MappingStore.Load();
                    Services.SubstService.MountAll(loadedMappings.Where(m => m.MountOnLoad));
                    Services.SubstService.UnmountAll(loadedMappings.Where(m => !m.MountOnLoad));

                    Current.Dispatcher.Invoke(RefreshMainIfOpen);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Startup task failed: {ex}");
                }
            });

            if (!isStartupRun)
            {
                HandleArgs(e.Args);
            }
        }

        private void HandleArgs(string[] args)
        {
            if (args == null) return;

            int addIdx = Array.FindIndex(args, a => a.Equals("/add", StringComparison.OrdinalIgnoreCase));
            if (addIdx >= 0 && addIdx + 1 < args.Length)
            {
                string path = args[addIdx + 1];
                ShowAddMappingDialog(path);
                return;
            }

            ShowMainWindow();
        }

        internal void ShowAddMappingDialog(string initialPath)
        {
            // If the main window is already open and visible, use it directly with its ViewModel
            if (_mainWindow != null && _mainWindow.IsLoaded && _mainWindow.IsVisible)
            {
                _mainWindow.Activate();
                _mainWindow.ViewModel?.ShowAddDialog(initialPath, showSuccessMessage: true);
                return;
            }

            // Otherwise, show ONLY the Add Mapping dialog without opening the main window
            var mappings = _mainWindow?.ViewModel != null
                ? _mainWindow.ViewModel.Mappings.ToList()
                : Services.MappingStore.Load();

            var usedLetters = mappings.Select(m => m.DriveLetter);
            var dlg = new AddEditDialog(null, usedLetters, initialPath)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Topmost = true
            };

            dlg.Loaded += (s, e) =>
            {
                dlg.Activate();
                dlg.Topmost = false;
            };

            if (dlg.ShowDialog() != true) return;

            var m = dlg.Result;
            if (mappings.Any(x => x.DriveLetter.Equals(m.DriveLetter, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"Drive {m.DisplayLetter} is already in the list.", "Duplicate Letter",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var (ok, err) = Services.SubstService.Mount(m.DriveLetter, m.FolderPath, m.Label);
            m.IsActive = ok;
            m.MountOnLoad = true;

            if (_mainWindow?.ViewModel != null)
            {
                _mainWindow.ViewModel.Mappings.Add(m);
                _mainWindow.ViewModel.SaveAll();
                _mainWindow.ViewModel.RefreshStatus();
            }
            else
            {
                mappings.Add(m);
                Services.MappingStore.Save(mappings);
            }

            if (ok)
            {
                _tray?.ShowNotification("Drive Mounted", $"Drive {m.DisplayLetter} mounted to {m.FolderPath}", m.DisplayLetter);
                var result = MessageBox.Show(
                    $"Drive {m.DisplayLetter} has been successfully mounted for:\n\n{m.FolderPath}\n\nWould you like to open Drive {m.DisplayLetter} in File Explorer now?",
                    "Drive Mounted",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Process.Start("explorer.exe", $"{m.DisplayLetter}\\");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to open Explorer: {ex}");
                    }
                }
            }
            else
            {
                MessageBox.Show(
                    $"Added {m.DisplayLetter}, but could not mount it immediately:\n{err}",
                    "Mount Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
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
            _mainWindow?.ShowDimmer();
            try
            {
                var win = new SettingsWindow { Owner = _mainWindow };
                win.ShowDialog();
            }
            finally
            {
                _mainWindow?.HideDimmer();
            }
        }

        internal void ShowActualDrives()
        {
            // Ensure the main window is available as Owner before opening actual drives
            ShowMainWindow();
            _mainWindow?.ShowDimmer();
            try
            {
                var win = new ActualDrivesWindow { Owner = _mainWindow };
                win.ShowDialog();
            }
            finally
            {
                _mainWindow?.HideDimmer();
            }
        }

        private void RefreshMainIfOpen()
            => _mainWindow?.ViewModel?.RefreshStatus();

        private void ExitApp()
        {
            _tray?.Dispose();
            _tray = null;
            SingleInstance.Release();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SingleInstance.Release();
            _tray?.Dispose();
            _tray = null;
            base.OnExit(e);
        }
    }
}
