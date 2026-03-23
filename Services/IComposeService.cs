using System.Windows.Media;
using pixel_edit.Models;

namespace pixel_edit.Services;

public interface IComposeService
{
    PixelProject Compose(IReadOnlyList<PixelProject> projects, ComposeMode mode, int pixelSize, string name);
}
