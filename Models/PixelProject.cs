namespace pixel_edit.Models;

/// <summary>
/// 像素工程根对象，包含画布、调色板、图层及组合信息。
/// </summary>
public sealed class PixelProject
{
    /// <summary>
    /// 工程数据结构版本号。
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>
    /// 工程唯一标识。
    /// </summary>
    public string ProjectId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 工程名称。
    /// </summary>
    public string Name { get; set; } = "Untitled";

    /// <summary>
    /// 工程创建时间（UTC）。
    /// </summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 工程最近修改时间（UTC）。
    /// </summary>
    public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 画布规格。
    /// </summary>
    public CanvasSpec Canvas { get; set; } = new();

    /// <summary>
    /// 工程调色板。
    /// </summary>
    public List<PaletteEntry> Palette { get; set; } = [];

    /// <summary>
    /// 工程图层集合。
    /// </summary>
    public List<PixelLayer> Layers { get; set; } = [];

    /// <summary>
    /// 组合模式（当工程由多个工程组合生成时有效）。
    /// </summary>
    public ComposeMode ComposeMode { get; set; } = ComposeMode.Stack;

    /// <summary>
    /// 组合来源项列表。
    /// </summary>
    public List<CompositionItem> CompositionItems { get; set; } = [];
}
