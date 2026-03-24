using System.Windows;
using pixel_edit.ViewModels;

namespace pixel_edit
{
    /// <summary>
    /// 主窗口代码后台，负责初始化界面并挂载主视图模型。
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 初始化主窗口并设置 <see cref="MainWindowViewModel"/> 作为数据上下文。
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}
