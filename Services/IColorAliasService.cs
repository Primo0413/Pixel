using pixel_edit.Models;

namespace pixel_edit.Services;

/// <summary>
/// 颜色别名服务接口，负责读取调色板及维护颜色别名。
/// </summary>
public interface IColorAliasService
{
    /// <summary>
    /// 加载当前可用的调色板颜色列表。
    /// </summary>
    /// <returns>只读调色板条目集合。</returns>
    IReadOnlyList<PaletteEntry> LoadPalette();

    /// <summary>
    /// 为指定颜色获取（或创建）一个别名。
    /// </summary>
    /// <param name="hex">待处理颜色的十六进制值，支持 #RRGGBB 或 RRGGBB。</param>
    /// <returns>颜色别名（例如 M6）。</returns>
    string EnsureAlias(string hex);
}
