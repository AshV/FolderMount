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

                    // 1. Enable Immersive Dark Mode for DWM rendering
                    int darkMode = 1;
                    if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int)) != 0)
                    {
                        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkMode, sizeof(int));
                    }

                    // 2. Windows 11 Rounded corners
                    int cornerPreference = DWMWCP_ROUND;
                    DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

                    // 3. Windows 11 crisp border color (#4E4E6E in BGR format: 0x006E4E4E)
                    if (isDialog)
                    {
                        int borderColor = 0x006E4E4E;
                        DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));
                    }
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
    }
}
