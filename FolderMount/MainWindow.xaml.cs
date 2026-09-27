using System;
using System.ComponentModel;
using System.Windows;
using FolderMount.Services;
using FolderMount.ViewModels;

namespace FolderMount
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }
        private int _dimmerCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            WindowHelper.ApplyModernWindowStyling(this, isDialog: false);

            ViewModel   = new MainViewModel();
            DataContext = ViewModel;

            CommandBindings.Add(new System.Windows.Input.CommandBinding(SystemCommands.CloseWindowCommand, (s, e) => SystemCommands.CloseWindow((Window)e.Parameter)));
            CommandBindings.Add(new System.Windows.Input.CommandBinding(SystemCommands.MaximizeWindowCommand, (s, e) => 
            {
                var w = (Window)e.Parameter;
                if (w.WindowState == WindowState.Maximized)
                    SystemCommands.RestoreWindow(w);
                else
                    SystemCommands.MaximizeWindow(w);
            }));
            CommandBindings.Add(new System.Windows.Input.CommandBinding(SystemCommands.MinimizeWindowCommand, (s, e) => SystemCommands.MinimizeWindow((Window)e.Parameter)));
            CommandBindings.Add(new System.Windows.Input.CommandBinding(SystemCommands.RestoreWindowCommand, (s, e) => SystemCommands.RestoreWindow((Window)e.Parameter)));
        }

        /// <summary>
        /// Visually dims the main window background when a modal dialog or sub-window is opened.
        /// </summary>
        public void ShowDimmer()
        {
            _dimmerCount++;
            if (ModalDimmer != null)
                ModalDimmer.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Restores main window background when the modal dialog closes.
        /// </summary>
        public void HideDimmer()
        {
            _dimmerCount = Math.Max(0, _dimmerCount - 1);
            if (_dimmerCount == 0 && ModalDimmer != null)
                ModalDimmer.Visibility = Visibility.Collapsed;
        }

        /// <summary>Minimize to tray instead of closing.</summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            ShowDimmer();
            try
            {
                var win = new SettingsWindow { Owner = this };
                win.ShowDialog();
            }
            finally
            {
                HideDimmer();
            }
        }

        private void BtnActualDrives_Click(object sender, RoutedEventArgs e)
        {
            ShowDimmer();
            try
            {
                var win = new ActualDrivesWindow { Owner = this };
                win.ShowDialog();
            }
            finally
            {
                HideDimmer();
            }
        }
    }
}
