using System.Windows.Media;

namespace pixel_edit.Services;

/// <summary>
/// 颜色辅助工具，负责 Color 与十六进制字符串之间的转换。
/// </summary>
internal static class ColorHelper
{
    /// <summary>
    /// 将十六进制颜色字符串转换为 <see cref="Color"/>。
    /// </summary>
    /// <param name="hex">十六进制颜色值，支持 #RRGGBB 或 RRGGBB。</param>
    /// <returns>转换后的颜色对象；输入为空时返回透明色。</returns>
    public static Color FromHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return Colors.Transparent;
        }

        var value = hex.StartsWith("#", StringComparison.Ordinal) ? hex : $"#{hex}";
        return (Color)ColorConverter.ConvertFromString(value)!;
    }

    /// <summary>
    /// 将 <see cref="Color"/> 转换为十六进制字符串（#RRGGBB）。
    /// </summary>
    /// <param name="color">输入颜色对象。</param>
    /// <returns>十六进制颜色字符串。</returns>
    public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
