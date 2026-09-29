using System;
using System.Drawing;
using System.Windows.Forms;
using FolderMount.Services;

namespace FolderMount
{
    /// <summary>
    /// Manages the system tray NotifyIcon independently of App.xaml partial class,
    /// so that System.Drawing / System.Windows.Forms code does not conflict with
    /// WPF's XAML compiler temporary project generation.
    /// </summary>
    internal sealed class TrayManager : IDisposable
    {
        private readonly NotifyIcon _icon;
        private readonly ContextMenuStrip _menu;
        private readonly ToolStripMenuItem _itemSystem;
        private readonly ToolStripMenuItem _itemDark;
        private readonly ToolStripMenuItem _itemLight;

        private string _lastMountedDrive;

        public TrayManager(
            Action onOpen,
            Action onMountAll,
            Action onUnmountAll,
            Action onActualDrives,
            Action onSettings,
            Action onExit)
        {
            _icon = new NotifyIcon
            {
                Text    = "FolderMount — Virtual Drive Manager",
                Visible = true,
                Icon    = LoadAppIcon()
            };

            _icon.BalloonTipClicked += (_, __) =>
            {
                if (!string.IsNullOrEmpty(_lastMountedDrive))
                {
                    try
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"{_lastMountedDrive}\\");
                    }
                    catch { }
                }
            };

            _menu       = new ContextMenuStrip();
            _menu.Font  = new Font("Segoe UI", 9.5f);

            AddItem(_menu, "📂  Open FolderMount",          onOpen);
            AddItem(_menu, "⚡  Mount All Drives",          onMountAll);
            AddItem(_menu, "⏏  Eject All Drives",          onUnmountAll);
            _menu.Items.Add(new ToolStripSeparator());

            // Theme submenu
            var themeMenu = new ToolStripMenuItem("🌓  Theme");
            _itemSystem = new ToolStripMenuItem("💻  Follow Windows", null, (_, __) => SetTheme(AppThemeMode.System));
            _itemDark   = new ToolStripMenuItem("🌙  Dark Theme",     null, (_, __) => SetTheme(AppThemeMode.Dark));
            _itemLight  = new ToolStripMenuItem("☀️  Light Theme",    null, (_, __) => SetTheme(AppThemeMode.Light));
            themeMenu.DropDownItems.Add(_itemSystem);
            themeMenu.DropDownItems.Add(_itemDark);
            themeMenu.DropDownItems.Add(_itemLight);
            _menu.Items.Add(themeMenu);

            _menu.Items.Add(new ToolStripSeparator());
            AddItem(_menu, "💽  Actual Drives & Labels",   onActualDrives);
            AddItem(_menu, "⚙  Settings",                   onSettings);
            _menu.Items.Add(new ToolStripSeparator());
            AddItem(_menu, "✕  Exit",                        onExit);

            _icon.ContextMenuStrip = _menu;
            _icon.DoubleClick     += (_, __) => onOpen();

            UpdateThemeMenu(SettingsStore.Current.Theme, ThemeService.CurrentActiveTheme == ThemeService.ActiveTheme.Dark);
            ThemeService.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged(AppThemeMode mode, bool isDark)
        {
            if (_menu.IsHandleCreated)
            {
                _menu.BeginInvoke(new Action(() => UpdateThemeMenu(mode, isDark)));
            }
            else
            {
                UpdateThemeMenu(mode, isDark);
            }
        }

        private void SetTheme(AppThemeMode mode)
        {
            SettingsStore.Current.Theme = mode;
            SettingsStore.Save();
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                ThemeService.ApplyTheme(mode);
            });
        }

        private void UpdateThemeMenu(AppThemeMode mode, bool isDark)
        {
            _itemSystem.Checked = (mode == AppThemeMode.System);
            _itemDark.Checked   = (mode == AppThemeMode.Dark);
            _itemLight.Checked  = (mode == AppThemeMode.Light);

            _menu.BackColor = isDark ? Color.FromArgb(26, 26, 46) : Color.FromArgb(255, 255, 255);
            _menu.ForeColor = isDark ? Color.FromArgb(241, 245, 249) : Color.FromArgb(15, 23, 42);
            _menu.Renderer  = new ModernMenuRenderer(isDark);
        }

        private static ToolStripMenuItem AddItem(ContextMenuStrip menu, string text, Action action)
        {
            var item = new ToolStripMenuItem(text);
            item.Click += (_, __) => action();
            menu.Items.Add(item);
            return item;
        }

        private static System.Drawing.Icon LoadAppIcon()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Assets/icon.ico", UriKind.Absolute);
                var sri = System.Windows.Application.GetResourceStream(uri);
                if (sri != null)
                    return new System.Drawing.Icon(sri.Stream);
            }
            catch { }

            try
            {
                string dir     = System.IO.Path.GetDirectoryName(
                                    System.Reflection.Assembly.GetExecutingAssembly().Location);
                string icoPath = System.IO.Path.Combine(dir, "Assets", "icon.ico");
                if (System.IO.File.Exists(icoPath))
                    return new System.Drawing.Icon(icoPath);
            }
            catch { }

            return SystemIcons.Application;
        }

        public void ShowNotification(string title, string message, string driveLetter = null, ToolTipIcon icon = ToolTipIcon.Info)
        {
            try
            {
                _lastMountedDrive = driveLetter;
                _icon?.ShowBalloonTip(3000, title, message, icon);
            }
            catch { }
        }

        public void Dispose()
        {
            ThemeService.ThemeChanged -= OnThemeChanged;
            _icon.Visible = false;
            _icon.Dispose();
        }
    }

    // ─── Modern tray context menu theming ──────────────────────────────────────

    internal class ModernMenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly bool _isDark;

        public ModernMenuRenderer(bool isDark) : base(new ModernMenuColors(isDark))
        {
            _isDark = isDark;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled
                ? (_isDark ? Color.FromArgb(241, 245, 249) : Color.FromArgb(15, 23, 42))
                : (_isDark ? Color.FromArgb(100, 116, 139) : Color.FromArgb(148, 163, 184));
            base.OnRenderItemText(e);
        }
    }

    internal class ModernMenuColors : ProfessionalColorTable
    {
        private readonly Color _bg;
        private readonly Color _hover;
        private readonly Color _border;
        private readonly Color _sep;

        public ModernMenuColors(bool isDark)
        {
            if (isDark)
            {
                _bg     = Color.FromArgb(26, 26, 46);
                _hover  = Color.FromArgb(45, 58, 107);
                _border = Color.FromArgb(45, 58, 107);
                _sep    = Color.FromArgb(30, 42, 74);
            }
            else
            {
                _bg     = Color.FromArgb(255, 255, 255);
                _hover  = Color.FromArgb(238, 242, 246);
                _border = Color.FromArgb(203, 213, 225);
                _sep    = Color.FromArgb(226, 232, 240);
            }
        }

        public override Color MenuItemSelected              => _hover;
        public override Color MenuItemBorder                => _border;
        public override Color MenuBorder                    => _border;
        public override Color ToolStripDropDownBackground   => _bg;
        public override Color ImageMarginGradientBegin      => _bg;
        public override Color ImageMarginGradientMiddle     => _bg;
        public override Color ImageMarginGradientEnd        => _bg;
        public override Color SeparatorDark                 => _sep;
        public override Color SeparatorLight                => _sep;
        public override Color MenuItemSelectedGradientBegin => _hover;
        public override Color MenuItemSelectedGradientEnd   => _hover;
        public override Color MenuItemPressedGradientBegin  => _hover;
        public override Color MenuItemPressedGradientEnd    => _hover;
    }
}
