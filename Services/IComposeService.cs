using System.Windows.Media;
using pixel_edit.Models;

namespace pixel_edit.Services;

/// <summary>
/// 工程组合服务接口，负责将多个像素工程按指定方式合成为新工程。
/// </summary>
public interface IComposeService
{
    /// <summary>
    /// 执行像素工程组合。
    /// </summary>
    /// <param name="projects">待组合的工程列表，不能为空。</param>
    /// <param name="mode">组合模式（拼接或堆叠）。</param>
    /// <param name="pixelSize">输出工程的像素显示尺寸。</param>
    /// <param name="name">输出工程名称。</param>
    /// <returns>组合完成后的新工程对象。</returns>
    PixelProject Compose(IReadOnlyList<PixelProject> projects, ComposeMode mode, int pixelSize, string name);
}
