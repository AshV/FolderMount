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

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Reflect current startup state
            ChkStartup.IsChecked = StartupService.IsEnabled;

            // Show mappings file path
            TxtMappingPath.Text = MappingStore.MappingsFilePath;
        }

        private void ChkStartup_Checked(object sender, RoutedEventArgs e)
        {
            string exePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
            StartupService.Enable(exePath);
        }

        private void ChkStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            StartupService.Disable();
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
                    "Importing will replace your current mappings. Existing mapped drives will be disconnected.\n\nAre you sure you want to proceed?",
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

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
