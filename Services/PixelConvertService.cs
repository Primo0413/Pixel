using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using pixel_edit.Models;

namespace pixel_edit.Services;

/// <summary>
/// 图片转像素工程服务。
/// 核心职责：将源图片采样到目标网格，并在固定色板中匹配最接近颜色。
/// </summary>
public sealed class PixelConvertService(IColorAliasService colorAliasService) : IPixelConvertService
{
    /// <summary>
    /// 色板采样缓存结构，用于减少循环中的重复解析开销。
    /// </summary>
    /// <param name="Index">色板索引。</param>
    /// <param name="R">红色分量。</param>
    /// <param name="G">绿色分量。</param>
    /// <param name="B">蓝色分量。</param>
    /// <param name="Hue">色相（HSV）。</param>
    /// <param name="Saturation">饱和度（HSV）。</param>
    /// <param name="IsNeutral">是否视为中性色（低饱和）。</param>
    private readonly record struct PaletteSample(
        int Index,
        byte R,
        byte G,
        byte B,
        double Hue,
        double Saturation,
        bool IsNeutral);

    /// <summary>
    /// 将输入图片转换为像素工程。
    /// </summary>
    /// <param name="imagePath">输入图片路径。</param>
    /// <param name="horizontalPixelCount">目标横向像素点数量。</param>
    /// <param name="projectName">生成工程名称。</param>
    /// <returns>转换完成后的像素工程对象。</returns>
    public Task<PixelProject> ConvertAsync(string imagePath, int horizontalPixelCount, string projectName)
    {
        var source = LoadPixels(imagePath, out var sourceWidth, out var sourceHeight);
        var sourceStride = sourceWidth * 4;

        var targetWidth = Math.Max(1, horizontalPixelCount);
        var targetHeight = Math.Max(1, (int)Math.Round((double)sourceHeight * targetWidth / sourceWidth));
        var pixelSize = Math.Max(1, 768 / targetWidth);

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

        if (project.Palette.Count == 0)
        {
            project.Palette.Add(new PaletteEntry { Alias = "M0", Hex = "#000000", Name = "M0" });
        }

        var paletteSamples = project.Palette
            .Select((item, idx) => BuildPaletteSample(idx, item.Hex))
            .ToList();

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

                var paletteIndex = FindNearestPaletteIndex(r, g, b, paletteSamples);
                layer.Pixels.Add(paletteIndex);
            }
        }

        project.Layers.Add(layer);
        return Task.FromResult(project);
    }

    /// <summary>
    /// 在固定色板中查找与指定 RGB 最接近的颜色索引。
    /// 匹配策略：优先同色系（Hue 接近），再以 RGB/饱和度综合距离评分。
    /// </summary>
    /// <param name="r">目标红色分量。</param>
    /// <param name="g">目标绿色分量。</param>
    /// <param name="b">目标蓝色分量。</param>
    /// <param name="palette">预处理后的色板采样集合。</param>
    /// <returns>最接近颜色的色板索引。</returns>
    private static int FindNearestPaletteIndex(byte r, byte g, byte b, IReadOnlyList<PaletteSample> palette)
    {
        ToHsv(r, g, b, out var hue, out var saturation, out _);
        var sourceNeutral = saturation < 0.12;

        var candidateSet = palette;

        if (sourceNeutral)
        {
            var neutralCandidates = palette.Where(x => x.IsNeutral).ToList();
            if (neutralCandidates.Count > 0)
            {
                candidateSet = neutralCandidates;
            }
        }
        else
        {
            var sameHueCandidates = palette.Where(x => !x.IsNeutral && HueDistance(hue, x.Hue) <= 32.0).ToList();
            if (sameHueCandidates.Count == 0)
            {
                sameHueCandidates = palette.Where(x => !x.IsNeutral && HueDistance(hue, x.Hue) <= 48.0).ToList();
            }

            if (sameHueCandidates.Count > 0)
            {
                candidateSet = sameHueCandidates;
            }
        }

        var bestScore = double.MaxValue;
        var bestIndex = candidateSet[0].Index;

        foreach (var c in candidateSet)
        {
            var dr = r - c.R;
            var dg = g - c.G;
            var db = b - c.B;
            var rgbDistance = dr * dr + dg * dg + db * db;

            var huePenalty = sourceNeutral ? 0.0 : Math.Pow(HueDistance(hue, c.Hue), 2) * 2.0;
            var satDiff = (saturation - c.Saturation) * 255.0;
            var saturationPenalty = satDiff * satDiff * 0.25;

            var score = rgbDistance + huePenalty + saturationPenalty;
            if (score < bestScore)
            {
                bestScore = score;
                bestIndex = c.Index;
            }
        }

        return bestIndex;
    }

    /// <summary>
    /// 从调色板条目构建采样数据。
    /// </summary>
    /// <param name="index">色板索引。</param>
    /// <param name="hex">色板颜色十六进制值。</param>
    /// <returns>可用于快速匹配的色板采样对象。</returns>
    private static PaletteSample BuildPaletteSample(int index, string hex)
    {
        ParseHex(hex, out var r, out var g, out var b);
        ToHsv(r, g, b, out var hue, out var saturation, out _);
        return new PaletteSample(index, r, g, b, hue, saturation, saturation < 0.12);
    }

    /// <summary>
    /// 将十六进制颜色字符串解析为 RGB 分量。
    /// </summary>
    /// <param name="hex">输入十六进制颜色值。</param>
    /// <param name="r">输出红色分量。</param>
    /// <param name="g">输出绿色分量。</param>
    /// <param name="b">输出蓝色分量。</param>
    private static void ParseHex(string hex, out byte r, out byte g, out byte b)
    {
        var value = string.IsNullOrWhiteSpace(hex) ? "000000" : hex.Trim();
        if (value.StartsWith('#'))
        {
            value = value[1..];
        }

        if (value.Length == 3)
        {
            value = string.Concat(value[0], value[0], value[1], value[1], value[2], value[2]);
        }

        if (value.Length != 6)
        {
            r = 0;
            g = 0;
            b = 0;
            return;
        }

        r = byte.TryParse(value.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var pr) ? pr : (byte)0;
        g = byte.TryParse(value.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var pg) ? pg : (byte)0;
        b = byte.TryParse(value.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var pb) ? pb : (byte)0;
    }

    /// <summary>
    /// 将 RGB 颜色转换为 HSV。
    /// </summary>
    /// <param name="r">红色分量。</param>
    /// <param name="g">绿色分量。</param>
    /// <param name="b">蓝色分量。</param>
    /// <param name="hue">输出色相（0-360）。</param>
    /// <param name="saturation">输出饱和度（0-1）。</param>
    /// <param name="value">输出明度（0-1）。</param>
    private static void ToHsv(byte r, byte g, byte b, out double hue, out double saturation, out double value)
    {
        var rd = r / 255.0;
        var gd = g / 255.0;
        var bd = b / 255.0;

        var max = Math.Max(rd, Math.Max(gd, bd));
        var min = Math.Min(rd, Math.Min(gd, bd));
        var delta = max - min;

        hue = 0.0;
        if (delta > 0.0)
        {
            if (max == rd)
            {
                hue = 60.0 * (((gd - bd) / delta) % 6.0);
            }
            else if (max == gd)
            {
                hue = 60.0 * (((bd - rd) / delta) + 2.0);
            }
            else
            {
                hue = 60.0 * (((rd - gd) / delta) + 4.0);
            }

            if (hue < 0.0)
            {
                hue += 360.0;
            }
        }

        saturation = max <= 0.0 ? 0.0 : delta / max;
        value = max;
    }

    /// <summary>
    /// 计算两个色相角度的环形最短距离。
    /// </summary>
    /// <param name="h1">色相 1。</param>
    /// <param name="h2">色相 2。</param>
    /// <returns>0-180 范围内的最短距离。</returns>
    private static double HueDistance(double h1, double h2)
    {
        var diff = Math.Abs(h1 - h2);
        return Math.Min(diff, 360.0 - diff);
    }

    /// <summary>
    /// 读取图片像素并统一转换为 BGRA32 字节数组。
    /// </summary>
    /// <param name="imagePath">图片路径。</param>
    /// <param name="width">输出图片宽度。</param>
    /// <param name="height">输出图片高度。</param>
    /// <returns>按行排列的 BGRA32 像素数据。</returns>
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
