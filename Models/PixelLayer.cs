namespace pixel_edit.Models;

/// <summary>
/// 表示像素工程中的单个图层。
/// </summary>
public sealed class PixelLayer
{
    /// <summary>
    /// 图层唯一标识。
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 图层名称。
    /// </summary>
    public string Name { get; set; } = "Layer";

    /// <summary>
    /// 图层层级，值越大通常越靠上。
    /// </summary>
    public int ZIndex { get; set; }

    /// <summary>
    /// 图层是否可见。
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// 图层在 X 方向的偏移（像素格单位）。
    /// </summary>
    public int OffsetX { get; set; }

    /// <summary>
    /// 图层在 Y 方向的偏移（像素格单位）。
    /// </summary>
    public int OffsetY { get; set; }

    /// <summary>
    /// 图层像素数据，保存调色板索引；-1 表示透明。
    /// </summary>
    public List<int> Pixels { get; set; } = [];
}
