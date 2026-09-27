using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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

        /// <summary>
        /// Gets the full executable path of the current process.
        /// Resolves to .exe even when executing via dotnet host.
        /// </summary>
        public static string GetExecutablePath()
        {
            string path = Environment.ProcessPath;
            if (string.IsNullOrEmpty(path))
            {
                try
                {
                    path = Process.GetCurrentProcess().MainModule?.FileName;
                }
                catch { }
            }
            if (string.IsNullOrEmpty(path))
            {
                try
                {
                    path = Assembly.GetExecutingAssembly().Location;
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(path) && path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                string exeCandidate = Path.ChangeExtension(path, ".exe");
                if (File.Exists(exeCandidate))
                    path = exeCandidate;
            }

            return path ?? string.Empty;
        }

        /// <summary>Returns true if the startup Run entry exists for the current user.</summary>
        public static bool IsEnabled
        {
            get
            {
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                        return key?.GetValue(ValueName) != null;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Registers FolderMount to run silently at login using the current executable path.
        /// Pass the path to FolderMount.exe.
        /// The /startup flag tells the app to skip showing the main window.
        /// </summary>
        public static void Enable(string exePath)
        {
            if (string.IsNullOrWhiteSpace(exePath))
                return;

            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
                    key?.SetValue(ValueName, $"\"{exePath}\" /startup");
            }
            catch
            {
                // Silently ignore if registry write is not permitted
            }
        }

        /// <summary>Removes the startup registry entry.</summary>
        public static void Disable()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                    key?.DeleteValue(ValueName, throwOnMissingValue: false);
            }
            catch
            {
                // Silently ignore if registry delete is not permitted
            }
        }

        /// <summary>
        /// Synchronizes the Windows startup registry entry with the user preference in SettingsStore.
        /// Enabled by default. If enabled, ensures the registry value exists and points to the current exe.
        /// If disabled, removes the registry value if present.
        /// </summary>
        public static void SyncStartupState()
        {
            try
            {
                bool shouldRunAtStartup = SettingsStore.Current.RunAtWindowsStartup;
                string exePath = GetExecutablePath();

                if (shouldRunAtStartup)
                {
                    if (string.IsNullOrEmpty(exePath))
                        return;

                    string expectedValue = $"\"{exePath}\" /startup";

                    using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                    {
                        var existing = key?.GetValue(ValueName) as string;
                        if (string.Equals(existing, expectedValue, StringComparison.OrdinalIgnoreCase))
                        {
                            return; // Already registered correctly
                        }
                    }

                    Enable(exePath);

                    if (!File.Exists(SettingsStore.SettingsFilePath))
                    {
                        SettingsStore.Save();
                    }
                }
                else
                {
                    if (IsEnabled)
                    {
                        Disable();
                    }
                }
            }
            catch
            {
                // Silently ignore sync errors
            }
        }
    }
}
