using System.Windows.Controls;
using System.Windows.Input;
using pixel_edit.Models;
using pixel_edit.ViewModels;

namespace pixel_edit.Views;

/// <summary>
/// 编辑器视图代码后台。
/// </summary>
public partial class EditorView : UserControl
{
    private bool _isLeftDragging;

    /// <summary>
    /// 初始化编辑器视图。
    /// </summary>
    public EditorView()
    {
        InitializeComponent();
    }

    private void PixelCell_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isLeftDragging = true;
        if (sender is Border border)
        {
            border.CaptureMouse();
            PaintCell(border);
            e.Handled = true;
        }
    }

    private void PixelCell_MouseEnter(object sender, MouseEventArgs e)
    {
        if (!_isLeftDragging || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (sender is Border border)
        {
            PaintCell(border);
        }
    }

    private void PixelCell_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isLeftDragging = false;
        if (sender is Border border)
        {
            border.ReleaseMouseCapture();
        }
    }

    private void PixelCell_MouseLeave(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Released)
        {
            _isLeftDragging = false;
            if (sender is Border border)
            {
                border.ReleaseMouseCapture();
            }
        }
    }

    private void PaintCell(Border border)
    {
        if (DataContext is not EditorViewModel vm)
        {
            return;
        }

        if (border.DataContext is not PixelCell cell)
        {
            return;
        }

        if (vm.PaintCellCommand.CanExecute(cell))
        {
            vm.PaintCellCommand.Execute(cell);
        }
    }
}
