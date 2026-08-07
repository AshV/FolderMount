using System;
using Microsoft.Win32;

namespace FolderMount.Services
{
    /// <summary>
    /// Manages the Windows startup registry entry for FolderMount.
    /// Writes to HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run —
    /// this does NOT require elevated (admin) permissions.
    /// </summary>
    public static class StartupService
    {
        private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName  = "FolderMount";

        /// <summary>Returns true if the startup Run entry exists for the current user.</summary>
        public static bool IsEnabled
        {
            get
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                    return key?.GetValue(ValueName) != null;
            }
        }

        /// <summary>
        /// Registers FolderMount to run silently at login using the current executable path.
        /// Pass the path to FolderMount.exe (typically Assembly.GetExecutingAssembly().Location).
        /// The /startup flag tells the app to skip showing the main window.
        /// </summary>
        public static void Enable(string exePath)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                key?.SetValue(ValueName, $"\"{exePath}\" /startup");
        }

        /// <summary>Removes the startup registry entry.</summary>
        public static void Disable()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                key?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
