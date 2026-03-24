namespace pixel_edit.Models;

/// <summary>
/// 描述像素工程画布规格。
/// </summary>
public sealed class CanvasSpec
{
    /// <summary>
    /// 画布宽度（以像素格为单位）。
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 画布高度（以像素格为单位）。
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 每个像素格在界面/导出中的显示尺寸。
    /// </summary>
    public int PixelSize { get; set; } = 16;
}
