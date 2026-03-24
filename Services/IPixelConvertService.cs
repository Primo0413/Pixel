using pixel_edit.Models;

namespace pixel_edit.Services;

/// <summary>
/// 图片转像素工程服务接口。
/// </summary>
public interface IPixelConvertService
{
    /// <summary>
    /// 将图片转换为像素工程。
    /// </summary>
    /// <param name="imagePath">输入图片路径。</param>
    /// <param name="horizontalPixelCount">目标横向像素点数量。</param>
    /// <param name="projectName">生成工程名称。</param>
    /// <returns>转换后的像素工程对象。</returns>
    Task<PixelProject> ConvertAsync(string imagePath, int horizontalPixelCount, string projectName);
}
