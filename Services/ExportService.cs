using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using pixel_edit.Models;

namespace pixel_edit.Services;

public sealed class ExportService : IExportService
{
    public Task ExportPngAsync(PixelProject project, string path)
    {
        var width = project.Canvas.Width;
        var height = project.Canvas.Height;
        var scale = Math.Max(1, project.Canvas.PixelSize);
        var wb = new WriteableBitmap(width * scale, height * scale, 96, 96, PixelFormats.Bgra32, null);

        var topLayer = project.Layers.OrderBy(x => x.ZIndex).LastOrDefault();
        if (topLayer is null)
        {
            return Task.CompletedTask;
        }

        var buffer = new byte[wb.PixelWidth * wb.PixelHeight * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = topLayer.Pixels[y * width + x];
                if (index < 0 || index >= project.Palette.Count)
                {
                    continue;
                }

                var color = ColorHelper.FromHex(project.Palette[index].Hex);
                for (var py = 0; py < scale; py++)
                {
                    for (var px = 0; px < scale; px++)
                    {
                        var tx = x * scale + px;
                        var ty = y * scale + py;
                        var pos = (ty * wb.PixelWidth + tx) * 4;
                        buffer[pos] = color.B;
                        buffer[pos + 1] = color.G;
                        buffer[pos + 2] = color.R;
                        buffer[pos + 3] = 255;
                    }
                }
            }
        }

        wb.WritePixels(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight), buffer, wb.PixelWidth * 4, 0);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(wb));
        using var fs = File.Create(path);
        encoder.Save(fs);
        return Task.CompletedTask;
    }
}
