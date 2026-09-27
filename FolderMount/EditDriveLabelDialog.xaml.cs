using System;
using System.Windows;
using FolderMount.Models;
using FolderMount.Services;

namespace FolderMount
{
    public partial class EditDriveLabelDialog : Window
    {
        private readonly ActualDrive _drive;

        public EditDriveLabelDialog(ActualDrive drive)
        {
            InitializeComponent();
            _drive = drive ?? throw new ArgumentNullException(nameof(drive));

            TxtTitle.Text = $"Edit Drive Label — {drive.DisplayLetter}";
            TxtDriveLetter.Text = drive.DisplayLetter;
            TxtCurrentVolLabel.Text = drive.HasVolumeLabel ? drive.VolumeLabel : "(None)";
            TxtDriveLabel.Text = drive.DriveLabel;

            CommandBindings.Add(new System.Windows.Input.CommandBinding(
                SystemCommands.CloseWindowCommand,
                (s, e) => Close()));
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string newLabel = TxtDriveLabel.Text.Trim();
            DriveLabelService.SetOrClearLabel(_drive.DriveLetter, newLabel);
            _drive.DriveLabel = newLabel;
            DialogResult = true;
            Close();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            DriveLabelService.ClearDriveLabel(_drive.DriveLetter);
            _drive.DriveLabel = string.Empty;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
