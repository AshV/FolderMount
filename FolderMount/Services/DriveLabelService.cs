using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace FolderMount.Services
{
    /// <summary>
    /// Manages custom drive labels and shell notifications for Windows Explorer.
    /// This enables virtual drives created via SUBST / DefineDosDevice to display friendly
    /// custom names in 'This PC' and ensures Explorer cleanly adds/removes drive icons.
    /// </summary>
    public static class DriveLabelService
    {
        private const string DriveIconsBasePath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\DriveIcons";

        // Shell change notification constants
        private const int SHCNE_DRIVEREMOVED = 0x00000080;
        private const int SHCNE_DRIVEADD     = 0x00000100;
        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_PATHW       = 0x0005;
        private const uint SHCNF_FLUSH       = 0x1000;
        private const uint SHCNF_IDLIST      = 0x0000;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern void SHChangeNotify(
            int wEventId,
            uint uFlags,
            [MarshalAs(UnmanagedType.LPWStr)] string dwItem1,
            [MarshalAs(UnmanagedType.LPWStr)] string dwItem2);

        /// <summary>
        /// Sets or clears the drive label for a given drive letter based on whether label is provided.
        /// </summary>
        public static void SetOrClearLabel(string driveLetter, string label, bool notifyShell = true)
        {
            if (string.IsNullOrWhiteSpace(driveLetter))
                return;

            if (string.IsNullOrWhiteSpace(label))
            {
                ClearDriveLabel(driveLetter, notifyShell);
            }
            else
            {
                SetDriveLabel(driveLetter, label, notifyShell);
            }
        }

        /// <summary>
        /// Sets a custom label for the specified drive letter in HKCU DriveIcons.
        /// </summary>
        public static void SetDriveLabel(string driveLetter, string label, bool notifyShell = true)
        {
            if (string.IsNullOrWhiteSpace(driveLetter))
                return;

            string letter = driveLetter.TrimEnd(':', '\\').ToUpper();
            string trimmedLabel = label.Trim();

            // 1. Direct registry write via .NET
            try
            {
                string subKeyPath = $@"{DriveIconsBasePath}\{letter}\DefaultLabel";
                using (var key = Registry.CurrentUser.CreateSubKey(subKeyPath))
                {
                    key?.SetValue(null, trimmedLabel);
                }
            }
            catch { }

            // 2. Fallback via reg.exe (ensures writes reach real HKCU even if running in an MSIX package)
            SetRegistryViaRegExe(letter, trimmedLabel);

            if (notifyShell)
                NotifyShell();
        }

        /// <summary>
        /// Clears the custom drive label from HKCU DriveIcons for the specified drive letter.
        /// </summary>
        public static void ClearDriveLabel(string driveLetter, bool notifyShell = true)
        {
            if (string.IsNullOrWhiteSpace(driveLetter))
                return;

            string letter = driveLetter.TrimEnd(':', '\\').ToUpper();

            // 1. Direct registry delete via .NET
            try
            {
                string letterKeyPath = $@"{DriveIconsBasePath}\{letter}";
                using (var letterKey = Registry.CurrentUser.OpenSubKey(letterKeyPath, true))
                {
                    if (letterKey != null)
                    {
                        letterKey.DeleteSubKeyTree("DefaultLabel", throwOnMissingSubKey: false);

                        if (letterKey.SubKeyCount == 0 && letterKey.ValueCount == 0)
                        {
                            using (var baseKey = Registry.CurrentUser.OpenSubKey(DriveIconsBasePath, true))
                            {
                                baseKey?.DeleteSubKey(letter, throwOnMissingSubKey: false);
                            }
                        }
                    }
                }
            }
            catch { }

            // 2. Fallback via reg.exe (ensures delete reaches real HKCU even if running in an MSIX package)
            DeleteRegistryViaRegExe(letter);

            if (notifyShell)
                NotifyShell();
        }

        /// <summary>
        /// Notifies Windows Explorer that a drive has been added so it appears immediately with its label.
        /// </summary>
        public static void NotifyDriveAdded(string driveLetter)
        {
            if (string.IsNullOrWhiteSpace(driveLetter)) return;
            try
            {
                string drivePath = driveLetter.TrimEnd(':', '\\').ToUpper() + @":\";
                SHChangeNotify(SHCNE_DRIVEADD, SHCNF_PATHW | SHCNF_FLUSH, drivePath, null);
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, null, null);
            }
            catch { }
        }

        /// <summary>
        /// Notifies Windows Explorer that a drive has been removed so it disappears immediately
        /// and does not linger as a phantom 'Local Disk'.
        /// </summary>
        public static void NotifyDriveRemoved(string driveLetter)
        {
            if (string.IsNullOrWhiteSpace(driveLetter)) return;
            try
            {
                string drivePath = driveLetter.TrimEnd(':', '\\').ToUpper() + @":\";
                SHChangeNotify(SHCNE_DRIVEREMOVED, SHCNF_PATHW | SHCNF_FLUSH, drivePath, null);
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, null, null);
            }
            catch { }
        }

        /// <summary>
        /// Broadcasts an association change notification so Windows Explorer refreshes cached drive labels.
        /// </summary>
        public static void NotifyShell()
        {
            try
            {
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, null, null);
            }
            catch { }
        }

        private static void SetRegistryViaRegExe(string letter, string label)
        {
            try
            {
                using var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = $"add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\DriveIcons\\{letter}\\DefaultLabel\" /ve /t REG_SZ /d \"{label.Replace("\"", "\\\"")}\" /f",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                proc?.WaitForExit(1000);
            }
            catch { }
        }

        private static void DeleteRegistryViaRegExe(string letter)
        {
            try
            {
                using var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = $"delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\DriveIcons\\{letter}\" /f",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                proc?.WaitForExit(1000);
            }
            catch { }
        }
    }
}
