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
            ["BgBase"]          = (Color)ColorConverter.ConvertFromString("#1E1E28"),
            ["BgSurface"]       = (Color)ColorConverter.ConvertFromString("#2A2A35"),
            ["BgCard"]          = (Color)ColorConverter.ConvertFromString("#252530"),
            ["BgElevated"]      = (Color)ColorConverter.ConvertFromString("#323242"),
            ["BgDialog"]        = (Color)ColorConverter.ConvertFromString("#252536"),
            ["BgRow"]           = (Color)ColorConverter.ConvertFromString("#2A2A35"),
            ["BgRowAlt"]        = (Color)ColorConverter.ConvertFromString("#252530"),
            ["BgRowHover"]      = (Color)ColorConverter.ConvertFromString("#323242"),
            ["BgRowSelect"]     = (Color)ColorConverter.ConvertFromString("#3E3E55"),
            ["Border"]          = (Color)ColorConverter.ConvertFromString("#3F3F5A"),
            ["BorderSubtle"]    = (Color)ColorConverter.ConvertFromString("#2D2D44"),
            ["BorderDialog"]    = (Color)ColorConverter.ConvertFromString("#4E4E6E"),
            ["TextPrimary"]     = (Color)ColorConverter.ConvertFromString("#F8FAFC"),
            ["TextSecondary"]   = (Color)ColorConverter.ConvertFromString("#CBD5E1"),
            ["TextMuted"]       = (Color)ColorConverter.ConvertFromString("#94A3B8"),
            ["AccentPrimary"]   = (Color)ColorConverter.ConvertFromString("#7C73FF"),
            ["AccentHover"]     = (Color)ColorConverter.ConvertFromString("#8C84FF"),
            ["AccentPressed"]   = (Color)ColorConverter.ConvertFromString("#6A61E0"),
            ["AccentDanger"]    = (Color)ColorConverter.ConvertFromString("#EF4444"),
            ["AccentSuccess"]   = (Color)ColorConverter.ConvertFromString("#4ADE80"),
            ["AccentWarning"]   = (Color)ColorConverter.ConvertFromString("#FBBF24"),
            ["AccentSecondary"] = (Color)ColorConverter.ConvertFromString("#38BDF8")
        };

        private static readonly Dictionary<string, Color> LightTokens = new()
        {
            ["BgBase"]          = (Color)ColorConverter.ConvertFromString("#F1F5F9"),
            ["BgSurface"]       = (Color)ColorConverter.ConvertFromString("#FFFFFF"),
            ["BgCard"]          = (Color)ColorConverter.ConvertFromString("#FFFFFF"),
            ["BgElevated"]      = (Color)ColorConverter.ConvertFromString("#E2E8F0"),
            ["BgDialog"]        = (Color)ColorConverter.ConvertFromString("#F8FAFC"),
            ["BgRow"]           = (Color)ColorConverter.ConvertFromString("#FFFFFF"),
            ["BgRowAlt"]        = (Color)ColorConverter.ConvertFromString("#F8FAFC"),
            ["BgRowHover"]      = (Color)ColorConverter.ConvertFromString("#EEF2F6"),
            ["BgRowSelect"]     = (Color)ColorConverter.ConvertFromString("#E0E7FF"),
            ["Border"]          = (Color)ColorConverter.ConvertFromString("#CBD5E1"),
            ["BorderSubtle"]    = (Color)ColorConverter.ConvertFromString("#E2E8F0"),
            ["BorderDialog"]    = (Color)ColorConverter.ConvertFromString("#CBD5E1"),
            ["TextPrimary"]     = (Color)ColorConverter.ConvertFromString("#0F172A"),
            ["TextSecondary"]   = (Color)ColorConverter.ConvertFromString("#475569"),
            ["TextMuted"]       = (Color)ColorConverter.ConvertFromString("#64748B"),
            ["AccentPrimary"]   = (Color)ColorConverter.ConvertFromString("#6366F1"),
            ["AccentHover"]     = (Color)ColorConverter.ConvertFromString("#4F46E5"),
            ["AccentPressed"]   = (Color)ColorConverter.ConvertFromString("#4338CA"),
            ["AccentDanger"]    = (Color)ColorConverter.ConvertFromString("#DC2626"),
            ["AccentSuccess"]   = (Color)ColorConverter.ConvertFromString("#16A34A"),
            ["AccentWarning"]   = (Color)ColorConverter.ConvertFromString("#D97706"),
            ["AccentSecondary"] = (Color)ColorConverter.ConvertFromString("#0284C7")
        };
    }
}
