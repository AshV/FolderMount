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
            CommandBindings.Add(new System.Windows.Input.CommandBinding(SystemCommands.CloseWindowCommand, (s, e) => SystemCommands.CloseWindow((Window)e.Parameter)));
            _isEdit = existing != null;

            if (_isEdit)
            {
                Title = "Edit Mapping";
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
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select a file inside the folder you want to map, or paste a folder path",
                CheckFileExists = false,
                ValidateNames = false,
                FileName = "Folder Selection"
            };

            if (dlg.ShowDialog() == true)
            {
                string path = dlg.FileName;
                // If they picked an actual file, extract the directory
                if (File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                }
                else if (Path.GetFileName(path) == "Folder Selection")
                {
                    path = Path.GetDirectoryName(path);
                }
                
                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                {
                    TxtFolderPath.Text = path;
                }
                else if (!string.IsNullOrWhiteSpace(path))
                {
                    // Fallback to just whatever they pasted if it doesn't match above logic
                    TxtFolderPath.Text = path;
                }
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
