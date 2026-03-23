using System.Windows;
using pixel_edit.ViewModels;

namespace pixel_edit
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}
