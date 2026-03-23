using pixel_edit.Models;

namespace pixel_edit.Services;

public interface IColorAliasService
{
    IReadOnlyList<PaletteEntry> LoadPalette();
    string EnsureAlias(string hex);
}
