using System.Linq;
using pixel_edit.Models;

namespace pixel_edit.Services;

/// <summary>
/// 像素工程组合服务，实现拼接与堆叠逻辑。
/// </summary>
public sealed class ComposeService : IComposeService
{
    /// <summary>
    /// 将多个像素工程按指定模式组合为新工程。
    /// </summary>
    /// <param name="projects">输入工程集合。</param>
    /// <param name="mode">组合模式。</param>
    /// <param name="pixelSize">输出工程像素显示尺寸。</param>
    /// <param name="name">输出工程名称。</param>
    /// <returns>组合后的像素工程。</returns>
    /// <exception cref="InvalidOperationException">当输入工程为空时抛出。</exception>
    public PixelProject Compose(IReadOnlyList<PixelProject> projects, ComposeMode mode, int pixelSize, string name)
    {
        if (projects.Count == 0)
        {
            throw new InvalidOperationException("没有可组合的项目。");
        }

        var palette = BuildMergedPalette(projects);
        var paletteIndexByHex = palette
            .Select((p, i) => (p.Hex, i))
            .ToDictionary(x => x.Hex, x => x.i, StringComparer.OrdinalIgnoreCase);

        var (width, height, placements) = BuildLayout(projects, mode);
        var output = Enumerable.Repeat(-1, width * height).ToArray();

        foreach (var placement in placements.OrderBy(p => p.zIndex))
        {
            var src = placement.project;
            var layer = src.Layers.OrderBy(l => l.ZIndex).LastOrDefault();
            if (layer is null)
            {
                continue;
            }

            for (var y = 0; y < src.Canvas.Height; y++)
            {
                for (var x = 0; x < src.Canvas.Width; x++)
                {
                    var srcIndex = layer.Pixels[y * src.Canvas.Width + x];
                    if (srcIndex < 0 || srcIndex >= src.Palette.Count)
                    {
                        continue;
                    }

                    var tx = x + placement.x;
                    var ty = y + placement.y;
                    if (tx < 0 || ty < 0 || tx >= width || ty >= height)
                    {
                        continue;
                    }

                    var hex = src.Palette[srcIndex].Hex;
                    var dstPaletteIndex = paletteIndexByHex[hex];
                    output[ty * width + tx] = dstPaletteIndex;
                }
            }
        }

        return new PixelProject
        {
            Name = name,
            Canvas = new CanvasSpec { Width = width, Height = height, PixelSize = pixelSize },
            Palette = palette,
            Layers = [new PixelLayer { Name = "Composed", ZIndex = 0, Pixels = output.ToList() }],
            ComposeMode = mode,
            CompositionItems = placements.Select(p => new CompositionItem
            {
                ProjectPath = p.path,
                X = p.x,
                Y = p.y,
                ZIndex = p.zIndex
            }).ToList()
        };
    }

    /// <summary>
    /// 合并多个工程的调色板，按颜色值去重。
    /// </summary>
    /// <param name="projects">输入工程集合。</param>
    /// <returns>合并后的调色板。</returns>
    private static List<PaletteEntry> BuildMergedPalette(IReadOnlyList<PixelProject> projects)
    {
        var merged = new List<PaletteEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var color in projects.SelectMany(p => p.Palette))
        {
            if (!seen.Add(color.Hex))
            {
                continue;
            }

            merged.Add(new PaletteEntry
            {
                Alias = color.Alias,
                Hex = color.Hex,
                Name = color.Name
            });
        }

        return merged;
    }

    /// <summary>
    /// 根据组合模式计算输出画布尺寸及各工程放置坐标。
    /// </summary>
    /// <param name="projects">输入工程集合。</param>
    /// <param name="mode">组合模式。</param>
    /// <returns>输出宽高及放置信息。</returns>
    private static (int width, int height, List<(PixelProject project, string path, int x, int y, int zIndex)> placements) BuildLayout(IReadOnlyList<PixelProject> projects, ComposeMode mode)
    {
        var placements = new List<(PixelProject, string, int, int, int)>();

        if (mode == ComposeMode.Stack)
        {
            var width = projects.Max(p => p.Canvas.Width);
            var height = projects.Max(p => p.Canvas.Height);
            for (var i = 0; i < projects.Count; i++)
            {
                placements.Add((projects[i], projects[i].ProjectId, 0, 0, i));
            }

            return (width, height, placements);
        }

        var xOffset = 0;
        var totalHeight = 0;
        foreach (var project in projects)
        {
            placements.Add((project, project.ProjectId, xOffset, 0, 0));
            xOffset += project.Canvas.Width;
            totalHeight = Math.Max(totalHeight, project.Canvas.Height);
        }

        return (xOffset, totalHeight, placements);
    }
}
