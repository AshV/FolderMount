using System.ComponentModel;
using System.Windows;
using FolderMount.ViewModels;

namespace FolderMount
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();
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

        /// <summary>Minimize to tray instead of closing.</summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var win = new SettingsWindow { Owner = this };
            win.ShowDialog();
        }
    }
}
