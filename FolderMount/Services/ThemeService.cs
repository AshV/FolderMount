using System;
using Microsoft.Win32;

namespace FolderMount.Services
{
    /// <summary>
    /// Reads the Windows system dark/light theme setting from the registry
    /// so FolderMount can follow the user's preferred colour scheme.
    /// </summary>
    public static class ThemeService
    {
        private const string ThemeKeyPath =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        public enum AppTheme { Dark, Light }

        /// <summary>
        /// Returns the current Windows app theme.
        /// Defaults to Dark if the key cannot be read (older Windows builds).
        /// </summary>
        public static AppTheme GetCurrentTheme()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(ThemeKeyPath, false))
                {
                    var value = key?.GetValue("AppsUseLightTheme");
                    if (value is int intVal)
                        return intVal == 1 ? AppTheme.Light : AppTheme.Dark;
                }
            }
            catch { /* fall through to default */ }
            return AppTheme.Dark;
        }

        /// <summary>Returns true when the system is set to dark mode.</summary>
        public static bool IsDarkMode => GetCurrentTheme() == AppTheme.Dark;
    }
}
