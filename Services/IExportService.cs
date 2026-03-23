using pixel_edit.Models;

namespace pixel_edit.Services;

public interface IExportService
{
    Task ExportPngAsync(PixelProject project, string path);
}
