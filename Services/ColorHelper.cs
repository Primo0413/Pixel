using System.Windows.Media;

namespace pixel_edit.Services;

internal static class ColorHelper
{
    public static Color FromHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return Colors.Transparent;
        }

        var value = hex.StartsWith("#", StringComparison.Ordinal) ? hex : $"#{hex}";
        return (Color)ColorConverter.ConvertFromString(value)!;
    }

    public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
