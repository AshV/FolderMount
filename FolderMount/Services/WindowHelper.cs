using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FolderMount.Services
{
    /// <summary>
    /// Applies Windows DWM attributes for modern window elevation,
    /// dark mode title bar integration, Windows 11 rounded corners, and drop shadows.
    /// </summary>
    public static class WindowHelper
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_BORDER_COLOR = 34;

        private const int DWMWCP_ROUND = 2;

        /// <summary>
        /// Applies modern OS window enhancements (DWM drop shadow, rounded corners, dark mode borders).
        /// </summary>
        public static void ApplyModernWindowStyling(Window window, bool isDialog = false)
        {
            if (window == null) return;

            void Apply()
            {
                try
                {
                    var hwnd = new WindowInteropHelper(window).Handle;
                    if (hwnd == IntPtr.Zero) return;

                    // 1. Windows 11 Rounded corners
                    int cornerPreference = DWMWCP_ROUND;
                    DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

                    // 2. Apply current active theme (Dark / Light)
                    bool isDark = ThemeService.CurrentActiveTheme == ThemeService.ActiveTheme.Dark;
                    UpdateWindowTheme(window, isDark, isDialog);
                }
                catch
                {
                    // Fail gracefully on older OS versions where DWM attributes are not available
                }
            }

            if (window.IsLoaded)
                Apply();
            else
                window.SourceInitialized += (s, e) => Apply();
        }

        /// <summary>
        /// Updates the DWM theme attributes (dark mode title bar, Windows 11 border color) live.
        /// </summary>
        public static void UpdateWindowTheme(Window window, bool isDark, bool isDialog = false)
        {
            if (window == null) return;

            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                int darkMode = isDark ? 1 : 0;
                if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkMode, sizeof(int));
                }

                // In BGR: Dark #4E4E6E is 0x006E4E4E; Light #CBD5E1 is 0x00E1D5CB
                int borderColor = isDark ? 0x006E4E4E : 0x00E1D5CB;
                DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));
            }
            catch
            {
            }
        }
    }
}
