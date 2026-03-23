namespace pixel_edit.Models;

public enum ComposeMode
{
    Stitch,
    Stack
}

public sealed class CompositionItem
{
    public string ProjectPath { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int ZIndex { get; set; }
}
