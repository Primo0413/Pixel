using System.IO;

namespace pixel_edit.Services;

/// <summary>
/// 应用本地文件路径管理器。
/// </summary>
public static class AppPaths
{
    /// <summary>
    /// 应用根目录（位于 LocalApplicationData 下）。访问时会自动确保目录存在。
    /// </summary>
    public static string RootDirectory
    {
        get
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "pixel_edit");
            Directory.CreateDirectory(root);
            return root;
        }
    }

    /// <summary>
    /// 项目默认保存目录。访问时会自动确保目录存在。
    /// </summary>
    public static string ProjectsDirectory
    {
        get
        {
            var path = Path.Combine(RootDirectory, "projects");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    /// <summary>
    /// 仓库根目录的色板文件路径（若能在当前目录向上找到）。
    /// </summary>
    public static string? WorkspaceColorAliasFile => FindUpwards("color-aliases.json");

    /// <summary>
    /// 颜色别名配置文件路径。
    /// 优先使用仓库根目录色板；找不到时退回 LocalAppData。
    /// </summary>
    public static string ColorAliasFile => WorkspaceColorAliasFile ?? Path.Combine(RootDirectory, "color-aliases.json");

    private static string? FindUpwards(string fileName)
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
