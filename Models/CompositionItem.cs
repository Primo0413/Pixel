namespace pixel_edit.Models;

/// <summary>
/// 表示多个像素工程组合时使用的模式。
/// </summary>
public enum ComposeMode
{
    /// <summary>
    /// 拼接模式：将多个工程按顺序在水平方向拼接。
    /// </summary>
    Stitch,

    /// <summary>
    /// 堆叠模式：将多个工程叠放在同一画布坐标系中，后者覆盖前者。
    /// </summary>
    Stack
}

/// <summary>
/// 描述组合工程中的单个来源项及其放置信息。
/// </summary>
public sealed class CompositionItem
{
    /// <summary>
    /// 来源工程的路径或标识。
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// 放置在目标画布中的 X 偏移（像素格坐标）。
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// 放置在目标画布中的 Y 偏移（像素格坐标）。
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// 图层深度，值越大通常越靠上。
    /// </summary>
    public int ZIndex { get; set; }
}
