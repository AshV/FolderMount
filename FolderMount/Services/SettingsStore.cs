using System;
using System.IO;
using System.Text.Json;

namespace FolderMount.Services
{
    /// <summary>
    /// Supported application theme modes.
    /// </summary>
    public enum AppThemeMode
    {
        System = 0,
        Dark = 1,
        Light = 2
    }

    /// <summary>
    /// Represents user preferences for FolderMount.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Whether FolderMount should launch automatically on Windows login.
        /// Enabled by default.
        /// </summary>
        public bool RunAtWindowsStartup { get; set; } = true;

        /// <summary>
        /// Preferred theme: System (default, follows Windows), Dark, or Light.
        /// </summary>
        public AppThemeMode Theme { get; set; } = AppThemeMode.System;
    }

    /// <summary>
    /// Persists application settings to %APPDATA%\FolderMount\settings.json.
    /// </summary>
    public static class SettingsStore
    {
        private static readonly string AppDataDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FolderMount");

        public static string SettingsFilePath => Path.Combine(AppDataDir, "settings.json");

        private static AppSettings _current;

        /// <summary>
        /// Gets the current loaded settings instance, loading from disk on first access.
        /// </summary>
        public static AppSettings Current
        {
            get
            {
                if (_current == null)
                    _current = Load();
                return _current;
            }
        }

        /// <summary>
        /// Loads settings from settings.json or returns default settings if the file does not exist.
        /// </summary>
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                        return settings;
                }
            }
            catch
            {
                // Fall back to defaults on corrupt/unreadable file
            }

            return new AppSettings();
        }

        /// <summary>
        /// Saves the given or current settings to settings.json.
        /// </summary>
        public static void Save(AppSettings settings = null)
        {
            try
            {
                if (!Directory.Exists(AppDataDir))
                    Directory.CreateDirectory(AppDataDir);

                _current = settings ?? _current ?? new AppSettings();
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_current, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // Silently ignore save errors
            }
        }
    }
}
