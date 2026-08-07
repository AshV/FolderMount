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
        }

        /// <summary>Minimize to tray instead of closing.</summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
