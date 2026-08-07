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
            string exePath = Assembly.GetExecutingAssembly().Location;
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
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string dest    = Path.Combine(desktop, "FolderMount_mappings.xml");
                var    mappings = MappingStore.Load();
                MappingStore.Export(dest, mappings);
                MessageBox.Show($"Exported to:\n{dest}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
