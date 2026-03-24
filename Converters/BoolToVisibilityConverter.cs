using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace pixel_edit.Converters;

/// <summary>
/// 将布尔值转换为 <see cref="Visibility"/> 的转换器。
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// 将布尔值转换为可见性：true 为 Visible，false 为 Collapsed。
    /// </summary>
    /// <param name="value">输入值。</param>
    /// <param name="targetType">目标类型。</param>
    /// <param name="parameter">可选参数。</param>
    /// <param name="culture">区域信息。</param>
    /// <returns>转换后的 <see cref="Visibility"/> 值。</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visible = value is true;
        return visible ? Visibility.Visible : Visibility.Collapsed;
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
