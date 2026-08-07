using System;
using System.Drawing;
using System.Windows.Forms;

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

        public TrayManager(
            Action onOpen,
            Action onMountAll,
            Action onUnmountAll,
            Action onSettings,
            Action onExit)
        {
            _icon = new NotifyIcon
            {
                Text    = "FolderMount — Virtual Drive Manager",
                Visible = true,
                Icon    = LoadAppIcon()
            };

            var menu        = new ContextMenuStrip();
            menu.BackColor  = Color.FromArgb(26, 26, 46);
            menu.ForeColor  = Color.FromArgb(241, 245, 249);
            menu.Font       = new Font("Segoe UI", 9.5f);
            menu.Renderer   = new DarkMenuRenderer();

            AddItem(menu, "📂  Open FolderMount",    onOpen);
            AddItem(menu, "⚡  Mount All Drives",    onMountAll);
            AddItem(menu, "⏏  Disconnect All",       onUnmountAll);
            menu.Items.Add(new ToolStripSeparator());
            AddItem(menu, "⚙  Settings",             onSettings);
            menu.Items.Add(new ToolStripSeparator());
            AddItem(menu, "✕  Exit",                  onExit);

            _icon.ContextMenuStrip = menu;
            _icon.DoubleClick     += (_, __) => onOpen();
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
                // Load from the embedded WPF resource (pack:// URI)
                // This works whether the app is run from bin\Debug, bin\Release, or installed anywhere.
                var uri = new Uri("pack://application:,,,/Assets/icon.ico", UriKind.Absolute);
                var sri = System.Windows.Application.GetResourceStream(uri);
                if (sri != null)
                    return new System.Drawing.Icon(sri.Stream);
            }
            catch { }

            // Fallback: try loading from disk (same directory as the exe)
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

        public void Dispose()
        {
            _icon.Visible = false;
            _icon.Dispose();
        }
    }

    // ─── Dark tray context menu theming ──────────────────────────────────────

    internal class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkMenuColors()) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled
                ? Color.FromArgb(241, 245, 249)
                : Color.FromArgb(100, 116, 139);
            base.OnRenderItemText(e);
        }
    }

    internal class DarkMenuColors : ProfessionalColorTable
    {
        private static readonly Color Bg        = Color.FromArgb(26, 26, 46);
        private static readonly Color Hover     = Color.FromArgb(45, 58, 107);
        private static readonly Color BorderClr = Color.FromArgb(45, 58, 107);
        private static readonly Color Sep       = Color.FromArgb(30, 42, 74);

        public override Color MenuItemSelected              => Hover;
        public override Color MenuItemBorder                => BorderClr;
        public override Color MenuBorder                    => BorderClr;
        public override Color ToolStripDropDownBackground   => Bg;
        public override Color ImageMarginGradientBegin      => Bg;
        public override Color ImageMarginGradientMiddle     => Bg;
        public override Color ImageMarginGradientEnd        => Bg;
        public override Color SeparatorDark                 => Sep;
        public override Color SeparatorLight                => Sep;
        public override Color MenuItemSelectedGradientBegin => Hover;
        public override Color MenuItemSelectedGradientEnd   => Hover;
        public override Color MenuItemPressedGradientBegin  => Color.FromArgb(108, 99, 255);
        public override Color MenuItemPressedGradientEnd    => Color.FromArgb(108, 99, 255);
    }
}
