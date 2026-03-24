using System.IO;
using System.Linq;
using System.Text.Json;
using pixel_edit.Models;

namespace pixel_edit.Services;

/// <summary>
/// 颜色别名服务，负责读取、规范化与持久化颜色别名调色板。
/// </summary>
public sealed class ColorAliasService : IColorAliasService
{
    /// <summary>
    /// 当前内存中的调色板数据。
    /// </summary>
    private readonly List<PaletteEntry> _palette;

    /// <summary>
    /// 初始化颜色别名服务。
    /// 若本地配置不存在，会创建默认色板文件。
    /// </summary>
    public ColorAliasService()
    {
        var filePath = AppPaths.ColorAliasFile;
        if (!File.Exists(filePath))
        {
            var defaults = new Dictionary<string, string>
            {
                ["M0"] = "#000000",
                ["M1"] = "#FFFFFF",
                ["M2"] = "#FF0000",
                ["M3"] = "#00FF00",
                ["M4"] = "#0000FF",
                ["M5"] = "#FFFF00",
                ["M6"] = "#445566"
            };
            File.WriteAllText(filePath, JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true }));
        }

        var map = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(filePath)) ?? [];
        _palette = map
            .OrderBy(x => x.Key)
            .Select(kv => new PaletteEntry { Alias = kv.Key, Hex = NormalizeHex(kv.Value), Name = kv.Key })
            .ToList();
    }

    /// <summary>
    /// 读取当前调色板。
    /// </summary>
    /// <returns>调色板只读集合。</returns>
    public IReadOnlyList<PaletteEntry> LoadPalette() => _palette;

    /// <summary>
    /// 为指定颜色获取或创建别名。
    /// </summary>
    /// <param name="hex">输入颜色十六进制值。</param>
    /// <returns>颜色对应的别名。</returns>
    public string EnsureAlias(string hex)
    {
        var normalized = NormalizeHex(hex);
        var existing = _palette.FirstOrDefault(x => string.Equals(x.Hex, normalized, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing.Alias;
        }

        var next = $"M{_palette.Count}";
        _palette.Add(new PaletteEntry { Alias = next, Hex = normalized, Name = next });
        Persist();
        return next;
    }

    /// <summary>
    /// 规范化十六进制颜色字符串为 #RRGGBB 大写格式。
    /// </summary>
    /// <param name="hex">待规范化颜色字符串。</param>
    /// <returns>规范化后的颜色字符串。</returns>
    private static string NormalizeHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return "#000000";
        }

        var value = hex.StartsWith("#", StringComparison.Ordinal) ? hex : $"#{hex}";
        if (value.Length == 4)
        {
            value = $"#{value[1]}{value[1]}{value[2]}{value[2]}{value[3]}{value[3]}";
        }

        return value.ToUpperInvariant();
    }

    /// <summary>
    /// 将当前调色板持久化到本地配置文件。
    /// </summary>
    private void Persist()
    {
        var map = _palette.ToDictionary(x => x.Alias, x => x.Hex);
        File.WriteAllText(AppPaths.ColorAliasFile, JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true }));
    }
}
