namespace pixel_edit.Models;

/// <summary>
/// 调色板中的颜色条目。
/// </summary>
public sealed class PaletteEntry
{
    /// <summary>
    /// 颜色别名（例如 A1、M6）。
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// 颜色十六进制值（格式：#RRGGBB）。
    /// </summary>
    public string Hex { get; set; } = "#000000";

    /// <summary>
    /// 颜色名称或备注。
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
