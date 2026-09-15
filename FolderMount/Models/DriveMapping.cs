using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace FolderMount.Models
{
    /// <summary>
    /// Represents a saved drive letter → folder path mapping.
    /// IsActive is computed at runtime by comparing against active SUBST output.
    /// Derived display properties (StatusBrush, StatusText, Visibility helpers)
    /// are calculated from IsActive so the XAML can bind directly without converters.
    /// </summary>
    public class DriveMapping : INotifyPropertyChanged
    {
        // ── Colour constants (match the app's design tokens) ──────────────────
        private static readonly SolidColorBrush BrushActive   = Freeze(new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80)));
        private static readonly SolidColorBrush BrushInactive = Freeze(new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)));

        // ── Backing fields ────────────────────────────────────────────────────
        private string _driveLetter;
        private string _folderPath;
        private string _label;
        private bool   _isActive;
        private bool   _mountOnLoad = true;

        // ── Core properties ───────────────────────────────────────────────────

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
            set
            {
                _isActive = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusBrush));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(MountVisibility));
                OnPropertyChanged(nameof(UnmountVisibility));
            }
        }

        /// <summary>Whether this drive should be automatically mounted on application load.</summary>
        public bool MountOnLoad
        {
            get => _mountOnLoad;
            set { _mountOnLoad = value; OnPropertyChanged(); }
        }

        // ── Derived display properties (replace IValueConverters) ─────────────

        /// <summary>Formatted drive letter for display, e.g. "P:"</summary>
        public string DisplayLetter => string.IsNullOrEmpty(DriveLetter) ? "" : DriveLetter + ":";

        /// <summary>Colour-coded brush for the status column.</summary>
        public SolidColorBrush StatusBrush => IsActive ? BrushActive : BrushInactive;

        /// <summary>Human-readable status.</summary>
        public string StatusText => IsActive ? "Mounted" : "Inactive";

        /// <summary>Visible when the drive is NOT mounted (show "Mount" button).</summary>
        public Visibility MountVisibility => IsActive ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>Visible when the drive IS mounted (show "Eject" button).</summary>
        public Visibility UnmountVisibility => IsActive ? Visibility.Visible : Visibility.Collapsed;

        // ── Clone ─────────────────────────────────────────────────────────────

        /// <summary>Creates a shallow copy without triggering property-changed notifications.</summary>
        public DriveMapping Clone() => (DriveMapping)MemberwiseClone();

        // ── INotifyPropertyChanged ────────────────────────────────────────────

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ── Helpers ───────────────────────────────────────────────────────────

        private static SolidColorBrush Freeze(SolidColorBrush brush)
        {
            brush.Freeze();
            return brush;
        }
    }
}
