using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace pixel_edit.Converters;

/// <summary>
/// 将十六进制颜色字符串转换为画刷对象的转换器。
/// </summary>
public sealed class HexToBrushConverter : IValueConverter
{
    /// <summary>
    /// 将十六进制颜色值转换为 <see cref="SolidColorBrush"/>。
    /// </summary>
    /// <param name="value">输入值，预期为 #RRGGBB 字符串。</param>
    /// <param name="targetType">目标类型。</param>
    /// <param name="parameter">可选参数。</param>
    /// <param name="culture">区域信息。</param>
    /// <returns>转换后的画刷；输入无效时返回透明画刷。</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string hex || string.IsNullOrWhiteSpace(hex))
        {
            return Brushes.Transparent;
        }

        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex)!;
            return new SolidColorBrush(color);
        }
        catch
        {
            return Brushes.Transparent;
        }
    }

    /// <summary>
    /// 反向转换（未实现，直接返回 DoNothing）。
    /// </summary>
    /// <param name="value">输入值。</param>
    /// <param name="targetType">目标类型。</param>
    /// <param name="parameter">可选参数。</param>
    /// <param name="culture">区域信息。</param>
    /// <returns><see cref="Binding.DoNothing"/>。</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
