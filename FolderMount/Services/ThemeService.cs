using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace FolderMount.Services
{
    /// <summary>
    /// Manages application themes (System Default, Dark, Light).
    /// Defaults to following the Windows system theme setting, and dynamically updates
    /// all UI resource brushes and OS window DWM frames live when changed.
    /// </summary>
    public static class ThemeService
    {
        private const string PersonalizeKey =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        public enum ActiveTheme
        {
            Dark,
            Light
        }

        private static bool _listenerInitialized;

        /// <summary>
        /// Fires when the effective theme or theme mode changes.
        /// Parameters: (AppThemeMode mode, bool isDark)
        /// </summary>
        public static event Action<AppThemeMode, bool> ThemeChanged;

        /// <summary>
        /// Gets the current resolved theme (Dark or Light) that is actively displayed.
        /// </summary>
        public static ActiveTheme CurrentActiveTheme { get; private set; } = ActiveTheme.Dark;

        /// <summary>
        /// Gets the user's selected mode (System, Dark, Light).
        /// </summary>
        public static AppThemeMode CurrentMode => SettingsStore.Current.Theme;

        /// <summary>
        /// Reads Windows system dark/light preference from registry.
        /// Returns ActiveTheme.Light if Windows AppsUseLightTheme == 1, otherwise ActiveTheme.Dark.
        /// </summary>
        public static ActiveTheme GetWindowsTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey, false);
                var val = key?.GetValue("AppsUseLightTheme");
                if (val is int intVal)
                    return intVal == 1 ? ActiveTheme.Light : ActiveTheme.Dark;
            }
            catch
            {
                // Fall through to default
            }
            return ActiveTheme.Dark;
        }

        /// <summary>Returns true when the Windows system is set to dark mode.</summary>
        public static bool IsWindowsDarkMode => GetWindowsTheme() == ActiveTheme.Dark;

        /// <summary>
        /// Initializes the system preference listener and applies the configured theme.
        /// </summary>
        public static void Initialize()
        {
            if (!_listenerInitialized)
            {
                _listenerInitialized = true;
                SystemEvents.UserPreferenceChanged += (s, e) =>
                {
                    if (SettingsStore.Current.Theme == AppThemeMode.System)
                    {
                        Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            ApplyTheme(AppThemeMode.System);
                        });
                    }
                };
            }

            ApplyTheme(SettingsStore.Current.Theme);
        }

        /// <summary>
        /// Applies the specified theme mode (System, Dark, or Light).
        /// Mutates all resource brushes live and updates DWM frame dark mode attributes.
        /// </summary>
        public static void ApplyTheme(AppThemeMode mode)
        {
            ActiveTheme targetTheme = mode switch
            {
                AppThemeMode.Dark  => ActiveTheme.Dark,
                AppThemeMode.Light => ActiveTheme.Light,
                _                  => GetWindowsTheme()
            };

            CurrentActiveTheme = targetTheme;
            bool isDark = targetTheme == ActiveTheme.Dark;

            ApplyBrushPalette(isDark);
            ApplyDwmToAllWindows(isDark);

            ThemeChanged?.Invoke(mode, isDark);
        }

        private static readonly (string Token, string[] BrushKeys)[] KeyMap = new[]
        {
            ("BgBase", new[] { "BrBgBase" }),
            ("BgSurface", new[] { "BrBgSurface" }),
            ("BgCard", new[] { "BrBgCard" }),
            ("BgElevated", new[] { "BrBgElevated" }),
            ("BgDialog", new[] { "BrBgDialog" }),
            ("BgRow", new[] { "BrBgRow" }),
            ("BgRowAlt", new[] { "BrBgRowAlt" }),
            ("BgRowHover", new[] { "BrBgRowHover" }),
            ("BgRowSelect", new[] { "BrBgRowSelect" }),
            ("Border", new[] { "BrBorder" }),
            ("BorderSubtle", new[] { "BrBorderSubtle" }),
            ("BorderDialog", new[] { "BrBorderDialog" }),
            ("TextPrimary", new[] { "BrText", "BrTextPrimary" }),
            ("TextSecondary", new[] { "BrTextSec", "BrTextSecondary" }),
            ("TextMuted", new[] { "BrTextMuted" }),
            ("AccentPrimary", new[] { "BrAccent", "BrAccentPrimary" }),
            ("AccentHover", new[] { "BrAccentHover" }),
            ("AccentPressed", new[] { "BrAccentPressed" }),
            ("AccentDanger", new[] { "BrDanger", "BrAccentDanger" }),
            ("AccentSuccess", new[] { "BrSuccess", "BrAccentSuccess" }),
            ("AccentWarning", new[] { "BrWarning", "BrAccentWarning" }),
            ("AccentSecondary", new[] { "BrSecondary", "BrAccentSecondary" })
        };

        private static void ApplyBrushPalette(bool isDark)
        {
            if (Application.Current == null) return;

            var palette = isDark ? DarkTokens : LightTokens;

            foreach (var mapping in KeyMap)
            {
                if (!palette.TryGetValue(mapping.Token, out Color color))
                    continue;

                // Update Color resource (e.g. "BgBase", "AccentPrimary", etc.)
                Application.Current.Resources[mapping.Token] = color;

                // Create new frozen brush for optimal performance and thread-safety
                var brush = new SolidColorBrush(color);
                brush.Freeze();

                // Update SolidColorBrush resources
                foreach (string brushKey in mapping.BrushKeys)
                {
                    Application.Current.Resources[brushKey] = brush;
                }
            }
        }

        private static void ApplyDwmToAllWindows(bool isDark)
        {
            if (Application.Current == null) return;

            foreach (Window win in Application.Current.Windows)
            {
                bool isDialog = win is not MainWindow;
                WindowHelper.UpdateWindowTheme(win, isDark, isDialog);
            }
        }

        // ─── Theme Palettes ──────────────────────────────────────────────────────────

        private static readonly Dictionary<string, Color> DarkTokens = new()
        {
            ["BgBase"]          = Color.FromRgb(0x1E, 0x1E, 0x28),
            ["BgSurface"]       = Color.FromRgb(0x2A, 0x2A, 0x35),
            ["BgCard"]          = Color.FromRgb(0x25, 0x25, 0x30),
            ["BgElevated"]      = Color.FromRgb(0x32, 0x32, 0x42),
            ["BgDialog"]        = Color.FromRgb(0x25, 0x25, 0x36),
            ["BgRow"]           = Color.FromRgb(0x2A, 0x2A, 0x35),
            ["BgRowAlt"]        = Color.FromRgb(0x25, 0x25, 0x30),
            ["BgRowHover"]      = Color.FromRgb(0x32, 0x32, 0x42),
            ["BgRowSelect"]     = Color.FromRgb(0x3E, 0x3E, 0x55),
            ["Border"]          = Color.FromRgb(0x3F, 0x3F, 0x5A),
            ["BorderSubtle"]    = Color.FromRgb(0x2D, 0x2D, 0x44),
            ["BorderDialog"]    = Color.FromRgb(0x4E, 0x4E, 0x6E),
            ["TextPrimary"]     = Color.FromRgb(0xF8, 0xFA, 0xFC),
            ["TextSecondary"]   = Color.FromRgb(0xCB, 0xD5, 0xE1),
            ["TextMuted"]       = Color.FromRgb(0x94, 0xA3, 0xB8),
            ["AccentPrimary"]   = Color.FromRgb(0x7C, 0x73, 0xFF),
            ["AccentHover"]     = Color.FromRgb(0x8C, 0x84, 0xFF),
            ["AccentPressed"]   = Color.FromRgb(0x6A, 0x61, 0xE0),
            ["AccentDanger"]    = Color.FromRgb(0xEF, 0x44, 0x44),
            ["AccentSuccess"]   = Color.FromRgb(0x4A, 0xDE, 0x80),
            ["AccentWarning"]   = Color.FromRgb(0xFB, 0xBF, 0x24),
            ["AccentSecondary"] = Color.FromRgb(0x38, 0xBD, 0xF8)
        };

        private static readonly Dictionary<string, Color> LightTokens = new()
        {
            ["BgBase"]          = Color.FromRgb(0xF1, 0xF5, 0xF9),
            ["BgSurface"]       = Color.FromRgb(0xFF, 0xFF, 0xFF),
            ["BgCard"]          = Color.FromRgb(0xFF, 0xFF, 0xFF),
            ["BgElevated"]      = Color.FromRgb(0xE2, 0xE8, 0xF0),
            ["BgDialog"]        = Color.FromRgb(0xF8, 0xFA, 0xFC),
            ["BgRow"]           = Color.FromRgb(0xFF, 0xFF, 0xFF),
            ["BgRowAlt"]        = Color.FromRgb(0xF8, 0xFA, 0xFC),
            ["BgRowHover"]      = Color.FromRgb(0xEE, 0xF2, 0xF6),
            ["BgRowSelect"]     = Color.FromRgb(0xE0, 0xE7, 0xFF),
            ["Border"]          = Color.FromRgb(0xCB, 0xD5, 0xE1),
            ["BorderSubtle"]    = Color.FromRgb(0xE2, 0xE8, 0xF0),
            ["BorderDialog"]    = Color.FromRgb(0xCB, 0xD5, 0xE1),
            ["TextPrimary"]     = Color.FromRgb(0x0F, 0x17, 0x2A),
            ["TextSecondary"]   = Color.FromRgb(0x47, 0x55, 0x69),
            ["TextMuted"]       = Color.FromRgb(0x64, 0x74, 0x8B),
            ["AccentPrimary"]   = Color.FromRgb(0x63, 0x66, 0xF1),
            ["AccentHover"]     = Color.FromRgb(0x4F, 0x46, 0xE5),
            ["AccentPressed"]   = Color.FromRgb(0x43, 0x38, 0xCA),
            ["AccentDanger"]    = Color.FromRgb(0xDC, 0x26, 0x26),
            ["AccentSuccess"]   = Color.FromRgb(0x16, 0xA3, 0x4A),
            ["AccentWarning"]   = Color.FromRgb(0xD9, 0x77, 0x06),
            ["AccentSecondary"] = Color.FromRgb(0x02, 0x84, 0xC7)
        };
    }
}
