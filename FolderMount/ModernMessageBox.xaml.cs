using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FolderMount.Services;

namespace FolderMount
{
    /// <summary>
    /// A sleek, theme-aware modern message box that follows FolderMount's Dark/Light themes,
    /// typography, DWM elevation, and design tokens.
    /// </summary>
    public partial class ModernMessageBox : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        // Path geometries for vector icons
        private const string InfoGeometry = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z";
        private const string QuestionGeometry = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 16h-2v-2h2v2zm1.07-7.75l-.9.92C12.45 11.9 12 12.5 12 14h-2v-.5c0-1.1.45-2.1 1.17-2.83l1.24-1.26c.37-.36.59-.86.59-1.41 0-1.1-.9-2-2-2s-2 .9-2 2H7c0-2.76 2.24-5 5-5s5 2.24 5 5c0 1.04-.42 1.99-1.07 2.75z";
        private const string WarningGeometry = "M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z";
        private const string ErrorGeometry = "M12 2C6.47 2 2 6.47 2 12s4.47 10 10 10 10-4.47 10-10S17.53 2 12 2zm5 13.59L15.59 17 12 13.41 8.41 17 7 15.59 10.59 12 7 8.41 8.41 7 12 10.59 15.59 7 17 8.41 13.41 12 17 15.59z";

        public ModernMessageBox(string message, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            InitializeComponent();
            WindowHelper.ApplyModernWindowStyling(this, isDialog: true);
            CommandBindings.Add(new System.Windows.Input.CommandBinding(SystemCommands.CloseWindowCommand, (s, e) => CloseWithCancel()));

            Title = string.IsNullOrWhiteSpace(caption) ? "FolderMount" : caption;
            TxtMessage.Text = message ?? string.Empty;

            ConfigureIcon(icon);
            ConfigureButtons(button);
        }

        private void ConfigureIcon(MessageBoxImage icon)
        {
            switch (icon)
            {
                case MessageBoxImage.Error:
                    IconPath.Data = Geometry.Parse(ErrorGeometry);
                    IconPath.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "BrDanger");
                    TopAccentLine.SetResourceReference(Border.BackgroundProperty, "BrDanger");
                    IconBadge.SetResourceReference(Border.BorderBrushProperty, "BrDanger");
                    break;

                case MessageBoxImage.Warning:
                    IconPath.Data = Geometry.Parse(WarningGeometry);
                    IconPath.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "BrWarning");
                    TopAccentLine.SetResourceReference(Border.BackgroundProperty, "BrWarning");
                    IconBadge.SetResourceReference(Border.BorderBrushProperty, "BrWarning");
                    break;

                case MessageBoxImage.Question:
                    IconPath.Data = Geometry.Parse(QuestionGeometry);
                    IconPath.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "BrAccent");
                    TopAccentLine.SetResourceReference(Border.BackgroundProperty, "BrAccent");
                    IconBadge.SetResourceReference(Border.BorderBrushProperty, "BrAccent");
                    break;

                case MessageBoxImage.Information:
                default:
                    IconPath.Data = Geometry.Parse(InfoGeometry);
                    IconPath.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "BrAccent");
                    TopAccentLine.SetResourceReference(Border.BackgroundProperty, "BrAccent");
                    IconBadge.SetResourceReference(Border.BorderBrushProperty, "BrAccent");
                    break;
            }
        }

        private void ConfigureButtons(MessageBoxButton button)
        {
            switch (button)
            {
                case MessageBoxButton.OK:
                    BtnOk.Visibility = Visibility.Visible;
                    BtnOk.IsDefault = true;
                    break;

                case MessageBoxButton.OKCancel:
                    BtnCancel.Visibility = Visibility.Visible;
                    BtnOk.Visibility = Visibility.Visible;
                    BtnOk.IsDefault = true;
                    BtnCancel.IsCancel = true;
                    break;

                case MessageBoxButton.YesNo:
                    BtnNo.Visibility = Visibility.Visible;
                    BtnYes.Visibility = Visibility.Visible;
                    BtnYes.IsDefault = true;
                    BtnNo.IsCancel = true;
                    break;

                case MessageBoxButton.YesNoCancel:
                    BtnCancel.Visibility = Visibility.Visible;
                    BtnNo.Visibility = Visibility.Visible;
                    BtnYes.Visibility = Visibility.Visible;
                    BtnYes.IsDefault = true;
                    BtnCancel.IsCancel = true;
                    break;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            DialogResult = true;
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            DialogResult = true;
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            DialogResult = false;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            DialogResult = false;
        }

        private void CloseWithCancel()
        {
            if (BtnCancel.Visibility == Visibility.Visible)
                Result = MessageBoxResult.Cancel;
            else if (BtnNo.Visibility == Visibility.Visible)
                Result = MessageBoxResult.No;
            else
                Result = MessageBoxResult.OK;

            DialogResult = false;
        }

        /// <summary>
        /// Displays a modern, theme-aware message box.
        /// </summary>
        public static MessageBoxResult Show(string messageBoxText, string caption = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
        {
            Window activeWindow = null;
            if (Application.Current != null)
            {
                foreach (Window w in Application.Current.Windows)
                {
                    if (w.IsActive && w.IsVisible)
                    {
                        activeWindow = w;
                        break;
                    }
                }
                activeWindow ??= Application.Current.MainWindow;
                if (activeWindow != null && (!activeWindow.IsVisible || !activeWindow.IsLoaded))
                    activeWindow = null;
            }

            return Show(activeWindow, messageBoxText, caption, button, icon);
        }

        /// <summary>
        /// Displays a modern, theme-aware message box with an explicit owner window.
        /// </summary>
        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
        {
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                return Application.Current.Dispatcher.Invoke(() => Show(owner, messageBoxText, caption, button, icon));
            }

            var dlg = new ModernMessageBox(messageBoxText, caption, button, icon);

            if (owner != null && owner.IsLoaded && owner.IsVisible)
            {
                dlg.Owner = owner;
                dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dlg.Topmost = true;
            }

            var mainWin = owner as MainWindow;
            mainWin?.ShowDimmer();
            try
            {
                dlg.ShowDialog();
                return dlg.Result;
            }
            finally
            {
                mainWin?.HideDimmer();
            }
        }
    }
}
