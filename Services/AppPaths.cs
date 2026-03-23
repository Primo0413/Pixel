using System.IO;

namespace pixel_edit.Services;

public static class AppPaths
{
    public static string RootDirectory
    {
        get
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "pixel_edit");
            Directory.CreateDirectory(root);
            return root;
        }
    }

    public static string ProjectsDirectory
    {
        get
        {
            var path = Path.Combine(RootDirectory, "projects");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string ColorAliasFile => Path.Combine(RootDirectory, "color-aliases.json");
}
