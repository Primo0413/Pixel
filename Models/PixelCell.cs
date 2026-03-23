using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace pixel_edit.Models;

public sealed partial class PixelCell : ObservableObject
{
    public int X { get; init; }
    public int Y { get; init; }

    [ObservableProperty]
    private int paletteIndex;

    [ObservableProperty]
    private Brush brush = Brushes.Transparent;
}
