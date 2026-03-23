using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using pixel_edit.Models;

namespace pixel_edit.Services;

public sealed class PixelConvertService(IColorAliasService colorAliasService) : IPixelConvertService
{
    public Task<PixelProject> ConvertAsync(string imagePath, int targetWidth, int targetHeight, int pixelSize, string projectName)
    {
        var source = LoadPixels(imagePath, out var sourceWidth, out var sourceHeight);
        var sourceStride = sourceWidth * 4;

        var project = new PixelProject
        {
            Name = projectName,
            Canvas = new CanvasSpec
            {
                Width = targetWidth,
                Height = targetHeight,
                PixelSize = pixelSize
            },
            Palette = colorAliasService.LoadPalette().Select(x => new PaletteEntry
            {
                Alias = x.Alias,
                Hex = x.Hex,
                Name = x.Name
            }).ToList()
        };

        var paletteIndexByAlias = project.Palette
            .Select((item, idx) => (item.Alias, idx))
            .ToDictionary(x => x.Alias, x => x.idx, StringComparer.OrdinalIgnoreCase);

        var layer = new PixelLayer { Name = "Base", ZIndex = 0 };

        for (var y = 0; y < targetHeight; y++)
        {
            for (var x = 0; x < targetWidth; x++)
            {
                var sx = x * sourceWidth / targetWidth;
                var sy = y * sourceHeight / targetHeight;
                var sourceIndex = sy * sourceStride + sx * 4;

                var b = source[sourceIndex];
                var g = source[sourceIndex + 1];
                var r = source[sourceIndex + 2];
                var a = source[sourceIndex + 3];

                if (a == 0)
                {
                    layer.Pixels.Add(-1);
                    continue;
                }

                var hex = $"#{r:X2}{g:X2}{b:X2}";
                var alias = colorAliasService.EnsureAlias(hex);
                if (!paletteIndexByAlias.TryGetValue(alias, out var paletteIndex))
                {
                    project.Palette.Add(new PaletteEntry { Alias = alias, Hex = hex, Name = alias });
                    paletteIndex = project.Palette.Count - 1;
                    paletteIndexByAlias[alias] = paletteIndex;
                }

                layer.Pixels.Add(paletteIndex);
            }
        }

        project.Layers.Add(layer);
        return Task.FromResult(project);
    }

    private static byte[] LoadPixels(string imagePath, out int width, out int height)
    {
        using var fs = File.OpenRead(imagePath);
        var decoder = BitmapDecoder.Create(fs, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        var converted = new FormatConvertedBitmap(frame, System.Windows.Media.PixelFormats.Bgra32, null, 0);

        width = converted.PixelWidth;
        height = converted.PixelHeight;

        var stride = width * 4;
        var data = new byte[stride * height];
        converted.CopyPixels(data, stride, 0);
        return data;
    }
}
