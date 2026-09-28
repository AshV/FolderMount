using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace FolderMount.Models
{
    /// <summary>
    /// Represents a physical/logical storage drive (not a virtual SUBST drive).
    /// Tracks both its filesystem Volume Label and its Explorer Drive Label override.
    /// </summary>
    public class ActualDrive : INotifyPropertyChanged
    {
        private static readonly SolidColorBrush BrushSuccess = Freeze(new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80)));
        private static readonly SolidColorBrush BrushWarning = Freeze(new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)));
        private static readonly SolidColorBrush BrushMuted   = Freeze(new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)));

        private string _driveLetter;
        private string _volumeLabel;
        private string _driveLabel;
        private DriveType _driveType;
        private string _fileSystem;
        private long _totalSizeBytes;
        private long _freeSizeBytes;

        public string DriveLetter
        {
            get => _driveLetter;
            set
            {
                _driveLetter = value?.ToUpper();
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayLetter));
            }
        }

        public string DisplayLetter => string.IsNullOrEmpty(DriveLetter) ? "" : DriveLetter + ":";

        /// <summary>Actual filesystem volume label stored on disk.</summary>
        public string VolumeLabel
        {
            get => _volumeLabel ?? string.Empty;
            set
            {
                _volumeLabel = value;
                OnPropertyChanged();
                NotifyAllComputed();
            }
        }

        /// <summary>Explorer shell override label stored in HKCU DriveIcons.</summary>
        public string DriveLabel
        {
            get => _driveLabel ?? string.Empty;
            set
            {
                _driveLabel = value;
                OnPropertyChanged();
                NotifyAllComputed();
            }
        }

        public DriveType DriveType
        {
            get => _driveType;
            set { _driveType = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayDetails)); }
        }

        public string FileSystem
        {
            get => _fileSystem;
            set { _fileSystem = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayDetails)); }
        }

        public long TotalSizeBytes
        {
            get => _totalSizeBytes;
            set { _totalSizeBytes = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayDetails)); }
        }

        public long FreeSizeBytes
        {
            get => _freeSizeBytes;
            set { _freeSizeBytes = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayDetails)); }
        }

        // ── Computed Properties ──────────────────────────────────────────────

        public bool HasVolumeLabel => !string.IsNullOrWhiteSpace(VolumeLabel);
        public bool HasDriveLabel  => !string.IsNullOrWhiteSpace(DriveLabel);

        /// <summary>
        /// True when the drive uses a Drive Label override with no filesystem Volume Label,
        /// allowing SUBST drives mounted from this drive to show their own custom labels.
        /// </summary>
        public bool IsOptimized => !HasVolumeLabel && HasDriveLabel;

        public bool CanMigrate => HasVolumeLabel;
        public bool CanRestore => !HasVolumeLabel && HasDriveLabel;

        public System.Windows.Visibility MigrateVisibility => CanMigrate ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        public System.Windows.Visibility RestoreVisibility => CanRestore ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        public string DisplayVolumeLabel => HasVolumeLabel ? VolumeLabel : "(None)";
        public string DisplayDriveLabel  => HasDriveLabel  ? DriveLabel  : "(None)";

        public string DisplayDetails =>
            $"{DriveType} • {FileSystem} • {FormatBytes(TotalSizeBytes)} ({FormatBytes(FreeSizeBytes)} free)";

        public string StatusText
        {
            get
            {
                if (HasVolumeLabel)
                    return "⚠️ Volume label active (blocks virtual labels)";
                if (IsOptimized)
                    return "✓ Optimized (Virtual labels work)";
                return "Default (No label set)";
            }
        }

        public SolidColorBrush StatusBrush
        {
            get
            {
                if (HasVolumeLabel)
                    return BrushWarning;
                if (IsOptimized)
                    return BrushSuccess;
                return BrushMuted;
            }
        }

        private void NotifyAllComputed()
        {
            OnPropertyChanged(nameof(HasVolumeLabel));
            OnPropertyChanged(nameof(HasDriveLabel));
            OnPropertyChanged(nameof(IsOptimized));
            OnPropertyChanged(nameof(CanMigrate));
            OnPropertyChanged(nameof(CanRestore));
            OnPropertyChanged(nameof(MigrateVisibility));
            OnPropertyChanged(nameof(RestoreVisibility));
            OnPropertyChanged(nameof(DisplayVolumeLabel));
            OnPropertyChanged(nameof(DisplayDriveLabel));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusBrush));
        }

        private static readonly string[] Suffixes = { "B", "KB", "MB", "GB", "TB" };

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 B";
            int i = 0;
            double d = bytes;
            while (d >= 1024 && i < Suffixes.Length - 1)
            {
                d /= 1024;
                i++;
            }
            return $"{d:0.#} {Suffixes[i]}";
        }

        private static SolidColorBrush Freeze(SolidColorBrush brush)
        {
            brush.Freeze();
            return brush;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
