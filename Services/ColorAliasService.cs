using System.IO;
using System.Linq;
using System.Text.Json;
using pixel_edit.Models;

namespace pixel_edit.Services;

public sealed class ColorAliasService : IColorAliasService
{
    private readonly List<PaletteEntry> _palette;

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

    public IReadOnlyList<PaletteEntry> LoadPalette() => _palette;

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

    private void Persist()
    {
        var map = _palette.ToDictionary(x => x.Alias, x => x.Hex);
        File.WriteAllText(AppPaths.ColorAliasFile, JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true }));
    }
}
