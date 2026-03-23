using System.IO;
using System.Text.Json;
using pixel_edit.Models;

namespace pixel_edit.Services;

public sealed class ProjectStorageService : IProjectStorageService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public async Task SaveAsync(string path, PixelProject project)
    {
        project.ModifiedUtc = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(project, Options);
        await File.WriteAllTextAsync(path, json);
    }

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
