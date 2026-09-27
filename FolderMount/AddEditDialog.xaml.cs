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

            UpdateOptimizationWarning();
        }

        private bool _userEditedLabel;
        private string _hostLetter;
        private string _hostVolumeLabel;

        private void TxtFolderPath_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!_isEdit && (!_userEditedLabel || string.IsNullOrWhiteSpace(TxtLabel.Text)))
            {
                string path = TxtFolderPath.Text.Trim();
                if (!string.IsNullOrEmpty(path))
                {
                    string folderName = GetDefaultLabelFromPath(path);
                    if (!string.IsNullOrEmpty(folderName))
                    {
                        TxtLabel.Text = folderName;
                    }
                }
            }

            UpdateOptimizationWarning();
        }

        private void TxtLabel_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (TxtLabel.IsKeyboardFocused)
            {
                _userEditedLabel = !string.IsNullOrWhiteSpace(TxtLabel.Text);
            }

            UpdateOptimizationWarning();
        }

        private void UpdateOptimizationWarning()
        {
            string path = TxtFolderPath.Text.Trim();
            _hostLetter = GetHostPhysicalDriveLetter(path);

            if (string.IsNullOrEmpty(_hostLetter))
            {
                BtnLabelWarning.Visibility = Visibility.Collapsed;
                return;
            }

            var (isOptimized, volLabel) = CheckHostDriveOptimization(_hostLetter);
            _hostVolumeLabel = volLabel;

            if (!isOptimized)
            {
                string virtualLabel = TxtLabel.Text.Trim();
                string labelDesc = string.IsNullOrEmpty(virtualLabel)
                    ? "this virtual drive label"
                    : $"'{virtualLabel}'";

                BtnLabelWarning.Visibility = Visibility.Visible;
                BtnLabelWarning.ToolTip =
                    $"Actual drive ({_hostLetter}:) label is not optimized so it won't show {labelDesc} in Windows Explorer.\nClick for options to optimize it.";
            }
            else
            {
                BtnLabelWarning.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnLabelWarning_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_hostLetter)) return;

            string virtualLabel = TxtLabel.Text.Trim();
            string labelDesc = string.IsNullOrEmpty(virtualLabel)
                ? "the virtual drive label"
                : $"\"{virtualLabel}\"";

            string msg =
                $"Actual drive ({_hostLetter}:) is not using an optimized drive label.\n\n" +
                $"• Current filesystem Volume Label: \"{_hostVolumeLabel}\"\n\n" +
                $"Because the host drive has a filesystem Volume Label on disk, Windows Explorer will display \"{_hostVolumeLabel}\" instead of {labelDesc}.\n\n" +
                $"Would you like to optimize Drive {_hostLetter}: now?\n\n" +
                $"• The filesystem Volume Label (\"{_hostVolumeLabel}\") will be removed from disk.\n" +
                $"• It will be saved as an Explorer Drive Label so Drive {_hostLetter}: keeps its friendly name.\n" +
                $"• Virtual drives mapped from {_hostLetter}: can then display their own custom labels in Explorer.\n\n" +
                $"Click 'Yes' to optimize now, or 'No' to ignore and continue.";

            var result = System.Windows.MessageBox.Show(
                this,
                msg,
                $"Drive {_hostLetter}: Not Optimized",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                bool success = DriveLabelService.MigrateVolumeLabelToDriveLabel(_hostLetter, out string err);
                if (success)
                {
                    System.Windows.MessageBox.Show(
                        this,
                        $"Drive {_hostLetter}: has been optimized successfully!\n\nWindows Explorer will now display custom labels for virtual drives mounted from this drive.",
                        "Optimization Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    UpdateOptimizationWarning();
                }
                else if (!string.IsNullOrEmpty(err))
                {
                    System.Windows.MessageBox.Show(
                        this,
                        err,
                        "Optimization Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            // If No, ignore and do nothing
        }

        private static string GetHostPhysicalDriveLetter(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            try
            {
                string root = Path.GetPathRoot(path);
                if (string.IsNullOrEmpty(root)) return null;

                string letter = root.Substring(0, 1).ToUpper();

                // If letter is currently a SUBST virtual drive, trace back to the real root
                var activeMappings = SubstService.GetActiveMappings();
                int depth = 0;
                while (activeMappings.TryGetValue(letter, out string target) && depth++ < 5)
                {
                    string targetRoot = Path.GetPathRoot(target);
                    if (string.IsNullOrEmpty(targetRoot)) break;
                    letter = targetRoot.Substring(0, 1).ToUpper();
                }

                return letter;
            }
            catch
            {
                return null;
            }
        }

        private static (bool IsOptimized, string VolumeLabel) CheckHostDriveOptimization(string hostLetter)
        {
            if (string.IsNullOrWhiteSpace(hostLetter)) return (true, null);

            try
            {
                var di = new DriveInfo(hostLetter + ":");
                if (!di.IsReady) return (true, null);

                string volLabel = di.VolumeLabel;
                // If it has a non-empty Volume Label, it's not optimized
                if (!string.IsNullOrWhiteSpace(volLabel))
                {
                    return (false, volLabel);
                }

                return (true, null);
            }
            catch
            {
                return (true, null);
            }
        }

        private static string GetDefaultLabelFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            try
            {
                string trimmed = path.Trim().Trim('"', '\'').TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string name = Path.GetFileName(trimmed);
                if (!string.IsNullOrEmpty(name))
                    return name;

                if (trimmed.EndsWith(':'))
                    return $"Drive {trimmed.TrimEnd(':')}";

                return trimmed;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description         = "Select the folder to map as a virtual drive";
                dlg.UseDescriptionForTitle = true;
                dlg.ShowNewFolderButton  = true;

                // If the user already has a path typed/pasted, start the browser there
                string currentPath = TxtFolderPath.Text.Trim();
                if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
                    dlg.SelectedPath = currentPath;

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
