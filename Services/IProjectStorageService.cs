using pixel_edit.Models;

namespace pixel_edit.Services;

public interface IProjectStorageService
{
    Task SaveAsync(string path, PixelProject project);
    Task<PixelProject> LoadAsync(string path);
}
