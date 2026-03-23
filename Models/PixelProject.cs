namespace pixel_edit.Models;

public sealed class PixelProject
{
    public int SchemaVersion { get; set; } = 1;
    public string ProjectId { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Untitled";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;
    public CanvasSpec Canvas { get; set; } = new();
    public List<PaletteEntry> Palette { get; set; } = [];
    public List<PixelLayer> Layers { get; set; } = [];
    public ComposeMode ComposeMode { get; set; } = ComposeMode.Stack;
    public List<CompositionItem> CompositionItems { get; set; } = [];
}
