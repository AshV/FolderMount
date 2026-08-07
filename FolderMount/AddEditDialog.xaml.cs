using System.IO;
using System.Windows;
using System.Windows.Forms;
using FolderMount.Models;
using FolderMount.Services;

namespace FolderMount
{
    public partial class AddEditDialog : Window
    {
        /// <summary>Result mapping after OK is clicked.</summary>
        public DriveMapping Result { get; private set; }

        private readonly bool _isEdit;

        /// <summary>
        /// Pass null for Add mode, or an existing mapping clone for Edit mode.
        /// Pass <paramref name="usedLetters"/> to exclude letters already in the saved list.
        /// </summary>
        public AddEditDialog(DriveMapping existing, System.Collections.Generic.IEnumerable<string> usedLetters = null)
        {
            InitializeComponent();
            _isEdit = existing != null;

            if (_isEdit)
            {
                TxtDialogTitle.Text = "Edit Drive Mapping";
                BtnOk.Content       = "Save Changes";
            }

            // Get letters that are free on the system AND not already in our saved list
            var letters = SubstService.GetAvailableLetters(usedLetters);

            if (_isEdit && !letters.Contains(existing.DriveLetter))
                letters.Insert(0, existing.DriveLetter); // always keep the current letter selectable

            foreach (var l in letters)
                CmbDriveLetter.Items.Add(l + ":");

            // Pre-fill fields in edit mode
            if (_isEdit)
            {
                CmbDriveLetter.SelectedItem = existing.DriveLetter + ":";
                TxtFolderPath.Text          = existing.FolderPath;
                TxtLabel.Text               = existing.Label;
            }
            else if (CmbDriveLetter.Items.Count > 0)
            {
                CmbDriveLetter.SelectedIndex = 0;
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new FolderBrowserDialog
            {
                Description         = "Select the folder to map as a virtual drive",
                ShowNewFolderButton = false,
                SelectedPath        = TxtFolderPath.Text
            })
            {
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    TxtFolderPath.Text = dlg.SelectedPath;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            TxtValidation.Text = "";

            if (CmbDriveLetter.SelectedItem == null)
            {
                TxtValidation.Text = "Please select a drive letter.";
                return;
            }

            string path = TxtFolderPath.Text.Trim();
            if (string.IsNullOrWhiteSpace(path))
            {
                TxtValidation.Text = "Please enter or browse to a folder path.";
                return;
            }
            if (!Directory.Exists(path))
            {
                TxtValidation.Text = "Folder does not exist. Please choose a valid path.";
                return;
            }

            string letter = CmbDriveLetter.SelectedItem.ToString().TrimEnd(':');
            Result = new DriveMapping
            {
                DriveLetter = letter,
                FolderPath  = path,
                Label       = TxtLabel.Text.Trim()
            };
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;
    }
}
