using System.IO;
using System.Text.Json;
using pixel_edit.Models;

namespace pixel_edit.Services;

/// <summary>
/// 像素工程存储服务，负责 .pxproj.json 的序列化与反序列化。
/// </summary>
public sealed class ProjectStorageService : IProjectStorageService
{
    /// <summary>
    /// JSON 序列化配置。
    /// </summary>
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// 保存像素工程到指定路径。
    /// </summary>
    /// <param name="path">目标文件路径。</param>
    /// <param name="project">待保存的工程对象。</param>
    /// <returns>异步任务。</returns>
    public async Task SaveAsync(string path, PixelProject project)
    {
        project.ModifiedUtc = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(project, Options);
        await File.WriteAllTextAsync(path, json);
    }

    /// <summary>
    /// 从指定路径加载像素工程。
    /// </summary>
    /// <param name="path">工程文件路径。</param>
    /// <returns>反序列化后的工程对象。</returns>
    /// <exception cref="InvalidDataException">当文件内容无法解析为有效工程时抛出。</exception>
    public async Task<PixelProject> LoadAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        var project = JsonSerializer.Deserialize<PixelProject>(json, Options);
        if (project is null)
        {
            throw new InvalidDataException("无法解析项目文件。");
        }

        return project;
    }
}
