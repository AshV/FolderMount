using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FolderMount.Models
{
    /// <summary>
    /// Represents a saved drive letter → folder path mapping.
    /// IsActive is computed at runtime by comparing against active SUBST output.
    /// </summary>
    public class DriveMapping : INotifyPropertyChanged
    {
        private string _driveLetter;
        private string _folderPath;
        private string _label;
        private bool _isActive;
        private bool _mountOnLoad = true;

        /// <summary>Single drive letter, e.g. "P"</summary>
        public string DriveLetter
        {
            get => _driveLetter;
            set { _driveLetter = value?.ToUpper(); OnPropertyChanged(); OnPropertyChanged(nameof(DisplayLetter)); }
        }

        /// <summary>Absolute folder path, e.g. "D:\Projects"</summary>
        public string FolderPath
        {
            get => _folderPath;
            set { _folderPath = value; OnPropertyChanged(); }
        }

        /// <summary>Optional user-facing label, e.g. "Work Projects"</summary>
        public string Label
        {
            get => _label;
            set { _label = value; OnPropertyChanged(); }
        }

        /// <summary>Runtime property — true when the SUBST drive is currently active on the system.</summary>
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusGlyph)); OnPropertyChanged(nameof(StatusText)); }
        }

        /// <summary>Whether this drive should be automatically mounted on application load.</summary>
        public bool MountOnLoad
        {
            get => _mountOnLoad;
            set { _mountOnLoad = value; OnPropertyChanged(); }
        }

        /// <summary>Formatted drive letter for display, e.g. "P:"</summary>
        public string DisplayLetter => string.IsNullOrEmpty(DriveLetter) ? "" : DriveLetter + ":";

        /// <summary>Colour-coded bullet for the status column</summary>
        public string StatusGlyph => IsActive ? "●" : "○";

        /// <summary>Human-readable status</summary>
        public string StatusText => IsActive ? "Mounted" : "Inactive";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public DriveMapping Clone() =>
            new DriveMapping { DriveLetter = DriveLetter, FolderPath = FolderPath, Label = Label, IsActive = IsActive, MountOnLoad = MountOnLoad };
    }
}
