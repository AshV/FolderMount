using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using FolderMount.Services;

namespace FolderMount
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            WindowHelper.ApplyModernWindowStyling(this, isDialog: true);
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            ThemeService.ThemeChanged += OnThemeChanged;
            CommandBindings.Add(new System.Windows.Input.CommandBinding(SystemCommands.CloseWindowCommand, (s, e) => SystemCommands.CloseWindow((Window)e.Parameter)));
        }

        private bool _isInitializing;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isInitializing = true;
            try
            {
                // Reflect current theme setting
                CmbTheme.SelectedIndex = (int)SettingsStore.Current.Theme;
                UpdateThemeDescription(SettingsStore.Current.Theme);

                // Reflect current startup state (enabled by default)
                ChkStartup.IsChecked = StartupService.IsEnabled;

                // Reflect context menu state
                ChkContextMenu.IsChecked = IsContextMenuEnabled();

                // Show mappings file path
                TxtMappingPath.Text = MappingStore.MappingsFilePath;
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ThemeService.ThemeChanged -= OnThemeChanged;
        }

        private void OnThemeChanged(AppThemeMode mode, bool isDark)
        {
            Dispatcher.Invoke(() =>
            {
                if (!_isInitializing)
                {
                    _isInitializing = true;
                    try
                    {
                        CmbTheme.SelectedIndex = (int)mode;
                        UpdateThemeDescription(mode);
                    }
                    finally
                    {
                        _isInitializing = false;
                    }
                }
            });
        }

        private void UpdateThemeDescription(AppThemeMode mode)
        {
            if (TxtThemeDesc == null) return;

            TxtThemeDesc.Text = mode switch
            {
                AppThemeMode.System => $"Follows Windows (currently {(ThemeService.IsWindowsDarkMode ? "Dark" : "Light")}).",
                AppThemeMode.Dark   => "Dark theme is active.",
                AppThemeMode.Light  => "Light theme is active.",
                _                   => "Follows Windows system setting."
            };
        }

        private void CmbTheme_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (CmbTheme.SelectedIndex >= 0)
            {
                var selectedMode = (AppThemeMode)CmbTheme.SelectedIndex;
                SettingsStore.Current.Theme = selectedMode;
                SettingsStore.Save();
                ThemeService.ApplyTheme(selectedMode);
                UpdateThemeDescription(selectedMode);
            }
        }

        private void ChkStartup_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            string exePath = StartupService.GetExecutablePath();
            StartupService.Enable(exePath);
            SettingsStore.Current.RunAtWindowsStartup = true;
            SettingsStore.Save();
        }

        private void ChkStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            StartupService.Disable();
            SettingsStore.Current.RunAtWindowsStartup = false;
            SettingsStore.Save();
        }

        private static readonly string[] ContextMenuRegPaths = new[]
        {
            @"Software\Classes\Directory\shell\FolderMount",
            @"Software\Classes\Folder\shell\FolderMount"
        };

        private bool IsContextMenuEnabled()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(ContextMenuRegPaths[0]))
                {
                    return key != null;
                }
            }
            catch { return false; }
        }

        private void ChkContextMenu_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            try
            {
                string exePath = StartupService.GetExecutablePath();
                foreach (var regPath in ContextMenuRegPaths)
                {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(regPath))
                    {
                        key.SetValue("", "Mount as Drive (FolderMount)");
                        key.SetValue("Icon", $"\"{exePath}\",0");
                        using (var cmdKey = key.CreateSubKey("command"))
                        {
                            cmdKey.SetValue("", $"\"{exePath}\" /add \"%1\"");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModernMessageBox.Show($"Could not add context menu:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChkContextMenu_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            try
            {
                foreach (var regPath in ContextMenuRegPaths)
                {
                    Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(regPath, false);
                }
            }
            catch (Exception ex)
            {
                ModernMessageBox.Show($"Could not remove context menu:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
        {
            string dir = Path.GetDirectoryName(MappingStore.MappingsFilePath);
            if (Directory.Exists(dir))
                Process.Start("explorer.exe", dir);
        }

        private void QuickExport_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                FileName = "FolderMount_Mappings.xml",
                Title = "Export Mappings"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var mappings = MappingStore.Load();
                    MappingStore.Export(dlg.FileName, mappings);
                    ModernMessageBox.Show($"Mappings exported successfully.", "Export Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    ModernMessageBox.Show($"Export failed:\n{ex.Message}", "Export Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void QuickImport_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                Title = "Import Mappings"
            };

            if (dlg.ShowDialog() == true)
            {
                var ans = ModernMessageBox.Show(
                    "Importing will replace your current mappings. Existing mapped drives will be ejected.\n\nAre you sure you want to proceed?",
                    "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (ans == MessageBoxResult.Yes)
                {
                    try
                    {
                        var imported = MappingStore.Import(dlg.FileName);
                        var mw = Owner as MainWindow;
                        var current = mw != null ? (System.Collections.Generic.IEnumerable<FolderMount.Models.DriveMapping>)mw.ViewModel.Mappings : MappingStore.Load();
                        SubstService.UnmountAll(current);
                        MappingStore.Save(imported);

                        // Tell the main window to reload
                        if (mw != null)
                        {
                            mw.ViewModel.LoadMappings();
                        }
                        
                        ModernMessageBox.Show($"Imported {imported.Count} mapping(s).", "Import Complete",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        ModernMessageBox.Show($"Import failed:\n{ex.Message}", "Import Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnActualDrives_Click(object sender, RoutedEventArgs e)
        {
            var win = new ActualDrivesWindow { Owner = this };
            win.ShowDialog();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
