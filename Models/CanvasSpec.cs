namespace pixel_edit.Models;

public sealed class CanvasSpec
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int PixelSize { get; set; } = 16;
}
