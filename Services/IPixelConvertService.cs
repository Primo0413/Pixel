using System.Windows.Media.Imaging;
using pixel_edit.Models;

namespace pixel_edit.Services;

public interface IPixelConvertService
{
    Task<PixelProject> ConvertAsync(string imagePath, int targetWidth, int targetHeight, int pixelSize, string projectName);
}
