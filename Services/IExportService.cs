using pixel_edit.Models;

namespace pixel_edit.Services;

/// <summary>
/// 导出服务接口，负责将像素工程导出为图片文件。
/// </summary>
public interface IExportService
{
    /// <summary>
    /// 将指定像素工程导出为 PNG 文件。
    /// </summary>
    /// <param name="project">待导出的像素工程。</param>
    /// <param name="path">导出目标路径。</param>
    /// <returns>异步任务。</returns>
    Task ExportPngAsync(PixelProject project, string path);
}
