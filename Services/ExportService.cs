using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using pixel_edit.Models;

namespace pixel_edit.Services;

/// <summary>
/// PNG 导出服务，将像素工程渲染为位图文件。
/// </summary>
public sealed class ExportService : IExportService
{
    /// <summary>
    /// 将像素工程导出为 PNG 图片。
    /// 导出内容包括：
    /// 1) 像素图主体（按调色板颜色渲染）
    /// 2) 非透明像素上的颜色别名标注
    /// 3) 图像下方的颜色使用统计区域
    /// </summary>
    /// <param name="project">待导出的像素工程。</param>
    /// <param name="path">输出 PNG 文件路径。</param>
    /// <returns>异步任务。</returns>
    public Task ExportPngAsync(PixelProject project, string path)
    {
        var topLayer = project.Layers.OrderBy(x => x.ZIndex).LastOrDefault();
        if (topLayer is null)
        {
            return Task.CompletedTask;
        }

        var width = Math.Max(1, project.Canvas.Width);
        var height = Math.Max(1, project.Canvas.Height);

        // 选取“合适”的导出像素格尺寸：
        // - 需要足够空间容纳色号文本
        // - 也要避免超大图导致导出过重
        var baseCell = Math.Clamp(project.Canvas.PixelSize, 16, 42);
        var maxMainWidth = 1800;
        var fitByWidth = Math.Max(12, maxMainWidth / width);
        var cellSize = Math.Max(12, Math.Min(baseCell, fitByWidth));

        var mainWidth = width * cellSize;
        var mainHeight = height * cellSize;

        var usage = new int[project.Palette.Count];
        foreach (var idx in topLayer.Pixels)
        {
            if (idx >= 0 && idx < usage.Length)
            {
                usage[idx]++;
            }
        }

        var usedEntries = usage
            .Select((count, idx) => (count, idx))
            .Where(x => x.count > 0)
            .OrderByDescending(x => x.count)
            .ThenBy(x => project.Palette[x.idx].Alias, StringComparer.OrdinalIgnoreCase)
            .Select(x => (Palette: project.Palette[x.idx], Count: x.count))
            .ToList();

        var legendPadding = 16;
        var legendHeaderHeight = 28;
        var legendItemHeight = 24;
        var legendColumnWidth = 260;
        var legendColumns = Math.Max(1, mainWidth / legendColumnWidth);
        var legendRows = Math.Max(1, (int)Math.Ceiling(usedEntries.Count / (double)legendColumns));
        var legendHeight = legendPadding * 2 + legendHeaderHeight + legendRows * legendItemHeight + 8;

        var outputWidth = mainWidth;
        var outputHeight = mainHeight + legendHeight;

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            // 整体白底
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, outputWidth, outputHeight));

            // 主图区域背景
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(248, 248, 248)), null, new Rect(0, 0, mainWidth, mainHeight));

            var textTypeface = new Typeface("Segoe UI");
            var labelFontSize = Math.Max(8, cellSize * 0.38);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var paletteIndex = topLayer.Pixels[y * width + x];
                    if (paletteIndex < 0 || paletteIndex >= project.Palette.Count)
                    {
                        continue;
                    }

                    var entry = project.Palette[paletteIndex];
                    var fillColor = ColorHelper.FromHex(entry.Hex);
                    var fillBrush = new SolidColorBrush(fillColor);
                    fillBrush.Freeze();

                    var rect = new Rect(x * cellSize, y * cellSize, cellSize, cellSize);
                    dc.DrawRectangle(fillBrush, new Pen(new SolidColorBrush(Color.FromArgb(48, 0, 0, 0)), 0.5), rect);

                    // 在非透明像素格中心标注色号
                    var label = string.IsNullOrWhiteSpace(entry.Alias) ? "?" : entry.Alias;
                    var textColor = GetReadableTextColor(fillColor);
                    var text = new FormattedText(
                        label,
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        textTypeface,
                        labelFontSize,
                        new SolidColorBrush(textColor),
                        1.0)
                    {
                        TextAlignment = TextAlignment.Center,
                        Trimming = TextTrimming.None
                    };

                    var tx = rect.X + rect.Width / 2;
                    var ty = rect.Y + (rect.Height - text.Height) / 2;
                    dc.DrawText(text, new Point(tx, ty));
                }
            }

            // 底部统计分隔线
            var legendTop = mainHeight;
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(224, 224, 224)), 1), new Point(0, legendTop), new Point(outputWidth, legendTop));

            // 标题
            var headerText = new FormattedText(
                $"颜色使用情况（共 {usedEntries.Count} 种）",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                textTypeface,
                15,
                new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                1.0);
            dc.DrawText(headerText, new Point(legendPadding, legendTop + 8));

            // 明细
            for (var i = 0; i < usedEntries.Count; i++)
            {
                var col = i / legendRows;
                var row = i % legendRows;

                var originX = legendPadding + col * legendColumnWidth;
                var originY = legendTop + legendPadding + legendHeaderHeight + row * legendItemHeight;

                var item = usedEntries[i];
                var color = ColorHelper.FromHex(item.Palette.Hex);

                dc.DrawRectangle(
                    new SolidColorBrush(color),
                    new Pen(new SolidColorBrush(Color.FromArgb(64, 0, 0, 0)), 0.8),
                    new Rect(originX, originY + 3, 14, 14));

                var line = new FormattedText(
                    $"{item.Palette.Alias}  {item.Palette.Hex}  {item.Count} px",
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    textTypeface,
                    12,
                    new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    1.0);
                dc.DrawText(line, new Point(originX + 22, originY));
            }
        }

        var bitmap = new RenderTargetBitmap(outputWidth, outputHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var fs = File.Create(path);
        encoder.Save(fs);

        return Task.CompletedTask;
    }

    private static Color GetReadableTextColor(Color background)
    {
        // 感知亮度，决定使用深色或浅色文字
        var luminance = (0.299 * background.R + 0.587 * background.G + 0.114 * background.B) / 255.0;
        return luminance > 0.62 ? Color.FromRgb(36, 36, 36) : Colors.White;
    }
}
