using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace FolderMount.Services
{
    /// <summary>
    /// Manages custom drive labels in Windows Explorer via the HKCU DriveIcons registry key.
    /// This enables virtual drives created via SUBST / DefineDosDevice to display friendly
    /// custom names in 'This PC' (e.g. "Work Projects (P:)") without administrative privileges.
    /// </summary>
    public static class DriveLabelService
    {
        private const string DriveIconsBasePath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\DriveIcons";

        // Shell change notification constants (to refresh Explorer without restarting explorer.exe)
        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

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

            try
            {
                string letter = driveLetter.TrimEnd(':').ToUpper();
                string subKeyPath = $@"{DriveIconsBasePath}\{letter}\DefaultLabel";

                using (var key = Registry.CurrentUser.CreateSubKey(subKeyPath))
                {
                    key?.SetValue(null, label.Trim());
                }

                if (notifyShell)
                    NotifyShell();
            }
            catch (Exception)
            {
                // Silently ignore if registry write fails (e.g. restricted policy)
            }
        }

        /// <summary>
        /// Clears the custom drive label from HKCU DriveIcons for the specified drive letter.
        /// If no other settings exist under the drive key, the drive letter subkey is also removed.
        /// </summary>
        public static void ClearDriveLabel(string driveLetter, bool notifyShell = true)
        {
            if (string.IsNullOrWhiteSpace(driveLetter))
                return;

            try
            {
                string letter = driveLetter.TrimEnd(':').ToUpper();
                string letterKeyPath = $@"{DriveIconsBasePath}\{letter}";

                using (var letterKey = Registry.CurrentUser.OpenSubKey(letterKeyPath, true))
                {
                    if (letterKey != null)
                    {
                        letterKey.DeleteSubKeyTree("DefaultLabel", throwOnMissingSubKey: false);

                        // If no other subkeys or values exist, delete the letter key itself to keep registry clean
                        if (letterKey.SubKeyCount == 0 && letterKey.ValueCount == 0)
                        {
                            using (var baseKey = Registry.CurrentUser.OpenSubKey(DriveIconsBasePath, true))
                            {
                                baseKey?.DeleteSubKey(letter, throwOnMissingSubKey: false);
                            }
                        }
                    }
                }

                if (notifyShell)
                    NotifyShell();
            }
            catch (Exception)
            {
                // Silently ignore if registry delete fails
            }
        }

        /// <summary>
        /// Broadcasts a shell change notification so Windows Explorer refreshes drive labels immediately.
        /// </summary>
        public static void NotifyShell()
        {
            try
            {
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception)
            {
                // Silently ignore if notification fails
            }
        }
    }
}
