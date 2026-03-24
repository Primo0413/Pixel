using pixel_edit.Models;

namespace pixel_edit.Services;

/// <summary>
/// 项目存储服务接口，负责像素工程的保存与读取。
/// </summary>
public interface IProjectStorageService
{
    /// <summary>
    /// 将像素工程保存到指定路径。
    /// </summary>
    /// <param name="path">目标文件路径。</param>
    /// <param name="project">待保存的像素工程对象。</param>
    /// <returns>异步任务。</returns>
    Task SaveAsync(string path, PixelProject project);

    /// <summary>
    /// 从指定路径读取像素工程。
    /// </summary>
    /// <param name="path">工程文件路径。</param>
    /// <returns>读取到的像素工程对象。</returns>
    Task<PixelProject> LoadAsync(string path);
}
