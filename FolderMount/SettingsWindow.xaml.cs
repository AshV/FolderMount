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
            Loaded += OnLoaded;
            CommandBindings.Add(new System.Windows.Input.CommandBinding(SystemCommands.CloseWindowCommand, (s, e) => SystemCommands.CloseWindow((Window)e.Parameter)));
        }

        private bool _isInitializing;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isInitializing = true;
            try
            {
                // Reflect current startup state (enabled by default)
                ChkStartup.IsChecked = StartupService.IsEnabled;

                // Show mappings file path
                TxtMappingPath.Text = MappingStore.MappingsFilePath;
            }
            finally
            {
                _isInitializing = false;
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
                    MessageBox.Show($"Mappings exported successfully.", "Export Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed:\n{ex.Message}", "Export Error",
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
                var ans = MessageBox.Show(
                    "Importing will replace your current mappings. Existing mapped drives will be ejected.\n\nAre you sure you want to proceed?",
                    "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (ans == MessageBoxResult.Yes)
                {
                    try
                    {
                        var imported = MappingStore.Import(dlg.FileName);
                        var current = MappingStore.Load();
                        SubstService.UnmountAll(current);
                        MappingStore.Save(imported);

                        // Tell the main window to reload
                        if (Owner is MainWindow mw)
                        {
                            mw.ViewModel.LoadMappings();
                        }
                        
                        MessageBox.Show($"Imported {imported.Count} mapping(s).", "Import Complete",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Import failed:\n{ex.Message}", "Import Error",
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
