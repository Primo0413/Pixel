using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace pixel_edit.Models;

/// <summary>
/// 编辑器中单个可交互像素格的视图模型对象。
/// </summary>
public sealed partial class PixelCell : ObservableObject
{
    /// <summary>
    /// 像素格的 X 坐标。
    /// </summary>
    public int X { get; init; }

    /// <summary>
    /// 像素格的 Y 坐标。
    /// </summary>
    public int Y { get; init; }

    /// <summary>
    /// 当前像素格对应的调色板索引；-1 表示透明。
    /// </summary>
    [ObservableProperty]
    private int paletteIndex;

    /// <summary>
    /// 当前像素格在界面上的画刷表现。
    /// </summary>
    [ObservableProperty]
    private Brush brush = Brushes.Transparent;
}
