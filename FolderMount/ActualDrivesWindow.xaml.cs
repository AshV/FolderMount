using System;
using System.Collections.ObjectModel;
using System.Windows;
using FolderMount.Models;
using FolderMount.Services;

namespace FolderMount
{
    public partial class ActualDrivesWindow : Window
    {
        public ObservableCollection<ActualDrive> Drives { get; } = new ObservableCollection<ActualDrive>();

        public ActualDrivesWindow()
        {
            InitializeComponent();
            GridDrives.ItemsSource = Drives;
            Loaded += (s, e) => LoadDrives();

            CommandBindings.Add(new System.Windows.Input.CommandBinding(
                SystemCommands.CloseWindowCommand,
                (s, e) => Close()));
        }

        public void LoadDrives()
        {
            Drives.Clear();
            var list = ActualDriveService.GetActualDrives();
            foreach (var d in list)
            {
                Drives.Add(d);
            }
            TxtStatus.Text = $"Found {Drives.Count} actual drive(s).";
        }

        private void BtnMigrate_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not ActualDrive drive) return;

            var result = MessageBox.Show(
                $"Convert {drive.DisplayLetter} to use an Explorer Drive Label?\n\n" +
                $"• The filesystem Volume Label ('{drive.VolumeLabel}') will be removed from the disk.\n" +
                $"• An Explorer Drive Label ('{drive.VolumeLabel}') will be set in the registry.\n\n" +
                "Virtual (SUBST) drives mounted from this drive will then display their own custom labels in Windows Explorer without being masked by the host volume.\n\n" +
                "Administrator permission may be requested by Windows to update the disk label.",
                "Migrate to Drive Label",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            TxtStatus.Text = $"Updating {drive.DisplayLetter}…";
            bool success = DriveLabelService.MigrateVolumeLabelToDriveLabel(drive.DriveLetter, out string err);

            if (success)
            {
                LoadDrives();
                TxtStatus.Text = $"Successfully converted {drive.DisplayLetter} to Drive Label.";
                MessageBox.Show(
                    $"Drive {drive.DisplayLetter} now uses an Explorer Drive Label!\n\n" +
                    "Virtual drives mounted from this drive can now show their own custom labels in Windows Explorer.",
                    "Migration Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                TxtStatus.Text = "Migration cancelled or failed.";
                if (!string.IsNullOrEmpty(err))
                {
                    MessageBox.Show(err, "Migration Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not ActualDrive drive) return;

            var result = MessageBox.Show(
                $"Restore filesystem Volume Label for {drive.DisplayLetter}?\n\n" +
                $"• The disk's Volume Label will be set to '{drive.DriveLabel}'.\n" +
                $"• The registry Drive Label override will be removed.\n\n" +
                "Note: Virtual drives mounted from this drive will once again inherit this volume label in Windows Explorer.",
                "Restore Volume Label",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            TxtStatus.Text = $"Restoring {drive.DisplayLetter}…";
            bool success = DriveLabelService.RestoreVolumeLabelFromDriveLabel(drive.DriveLetter, out string err);

            if (success)
            {
                LoadDrives();
                TxtStatus.Text = $"Restored Volume Label for {drive.DisplayLetter}.";
            }
            else
            {
                TxtStatus.Text = "Restore cancelled or failed.";
                if (!string.IsNullOrEmpty(err))
                {
                    MessageBox.Show(err, "Restore Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not ActualDrive drive) return;

            var dlg = new EditDriveLabelDialog(drive) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                LoadDrives();
                TxtStatus.Text = $"Updated label for {drive.DisplayLetter}.";
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadDrives();
            DriveLabelService.NotifyShell();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
