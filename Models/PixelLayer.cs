namespace pixel_edit.Models;

public sealed class PixelLayer
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Layer";
    public int ZIndex { get; set; }
    public bool Visible { get; set; } = true;
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public List<int> Pixels { get; set; } = [];
}
