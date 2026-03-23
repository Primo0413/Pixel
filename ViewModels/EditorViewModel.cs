using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using pixel_edit.Models;
using pixel_edit.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace pixel_edit.ViewModels;

public sealed partial class EditorViewModel : ObservableObject
{
    private readonly IProjectStorageService _projectStorageService;
    private readonly IPixelConvertService? _pixelConvertService;
    private readonly IExportService _exportService;
    private readonly Action _goHome;

    [ObservableProperty]
    private string title = "编辑器";

    [ObservableProperty]
    private PixelProject? project;

    [ObservableProperty]
    private PaletteEntry? selectedColor;

    [ObservableProperty]
    private PixelCell? selectedCell;

    [ObservableProperty]
    private bool eraserMode;

    public ObservableCollection<PixelCell> Cells { get; } = [];

    public IRelayCommand BackHomeCommand { get; }
    public IAsyncRelayCommand SaveProjectCommand { get; }
    public IAsyncRelayCommand LoadProjectCommand { get; }
    public IAsyncRelayCommand ExportPngCommand { get; }
    public IRelayCommand<PixelCell> PaintCellCommand { get; }
    public IRelayCommand DeleteSelectedPixelCommand { get; }
    public IAsyncRelayCommand ImportAndConvertImageCommand { get; }

    public EditorViewModel(PixelProject initialProject, IProjectStorageService projectStorageService, IExportService exportService, Action goHome)
    {
        _projectStorageService = projectStorageService;
        _exportService = exportService;
        _goHome = goHome;

        Project = initialProject;
        Title = $"编辑：{initialProject.Name}";
        SelectedColor = initialProject.Palette.FirstOrDefault();

        BackHomeCommand = new RelayCommand(() => _goHome());
        SaveProjectCommand = new AsyncRelayCommand(SaveProjectAsync);
        LoadProjectCommand = new AsyncRelayCommand(LoadProjectAsync);
        ExportPngCommand = new AsyncRelayCommand(ExportPngAsync);
        PaintCellCommand = new RelayCommand<PixelCell>(PaintCell);
        DeleteSelectedPixelCommand = new RelayCommand(DeleteSelectedPixel);
        ImportAndConvertImageCommand = new AsyncRelayCommand(() => Task.CompletedTask);

        RebuildCells();
    }

    public EditorViewModel(IProjectStorageService projectStorageService, IPixelConvertService pixelConvertService, IExportService exportService, Action goHome)
    {
        _projectStorageService = projectStorageService;
        _pixelConvertService = pixelConvertService;
        _exportService = exportService;
        _goHome = goHome;

        BackHomeCommand = new RelayCommand(() => _goHome());
        SaveProjectCommand = new AsyncRelayCommand(SaveProjectAsync);
        LoadProjectCommand = new AsyncRelayCommand(LoadProjectAsync);
        ExportPngCommand = new AsyncRelayCommand(ExportPngAsync);
        PaintCellCommand = new RelayCommand<PixelCell>(PaintCell);
        DeleteSelectedPixelCommand = new RelayCommand(DeleteSelectedPixel);
        ImportAndConvertImageCommand = new AsyncRelayCommand(ImportAndConvertImageAsync);

        Title = "图片转像素画";
    }

    private void RebuildCells()
    {
        Cells.Clear();

        if (Project is null || Project.Layers.Count == 0)
        {
            return;
        }

        var layer = Project.Layers.OrderBy(l => l.ZIndex).Last();
        var width = Project.Canvas.Width;
        var height = Project.Canvas.Height;

        if (layer.Pixels.Count != width * height)
        {
            layer.Pixels = Enumerable.Repeat(-1, width * height).ToList();
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var paletteIndex = layer.Pixels[y * width + x];
                Brush brush = Brushes.Transparent;
                if (paletteIndex >= 0 && paletteIndex < Project.Palette.Count)
                {
                    brush = new SolidColorBrush(ColorHelper.FromHex(Project.Palette[paletteIndex].Hex));
                }

                Cells.Add(new PixelCell
                {
                    X = x,
                    Y = y,
                    PaletteIndex = paletteIndex,
                    Brush = brush
                });
            }
        }
    }

    private void PaintCell(PixelCell? cell)
    {
        if (cell is null || Project is null || Project.Layers.Count == 0)
        {
            return;
        }

        SelectedCell = cell;

        var layer = Project.Layers.OrderBy(l => l.ZIndex).Last();
        var index = cell.Y * Project.Canvas.Width + cell.X;

        if (EraserMode || SelectedColor is null)
        {
            layer.Pixels[index] = -1;
            cell.PaletteIndex = -1;
            cell.Brush = Brushes.Transparent;
            return;
        }

        var paletteIndex = Project.Palette.FindIndex(x => x.Alias == SelectedColor.Alias);
        if (paletteIndex < 0)
        {
            return;
        }

        layer.Pixels[index] = paletteIndex;
        cell.PaletteIndex = paletteIndex;
        cell.Brush = new SolidColorBrush(ColorHelper.FromHex(SelectedColor.Hex));
    }

    private void DeleteSelectedPixel()
    {
        if (SelectedCell is null)
        {
            return;
        }

        EraserMode = true;
        PaintCell(SelectedCell);
        EraserMode = false;
    }

    private async Task SaveProjectAsync()
    {
        if (Project is null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Pixel Project (*.pxproj.json)|*.pxproj.json",
            InitialDirectory = AppPaths.ProjectsDirectory,
            FileName = string.IsNullOrWhiteSpace(Project.Name) ? "untitled" : Project.Name
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await _projectStorageService.SaveAsync(dialog.FileName, Project);
    }

    private async Task LoadProjectAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Pixel Project (*.pxproj.json)|*.pxproj.json",
            InitialDirectory = AppPaths.ProjectsDirectory,
            CheckFileExists = true
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        Project = await _projectStorageService.LoadAsync(dialog.FileName);
        Title = $"编辑：{Project.Name}";
        SelectedColor = Project.Palette.FirstOrDefault();
        RebuildCells();
    }

    private async Task ExportPngAsync()
    {
        if (Project is null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "PNG Image (*.png)|*.png",
            InitialDirectory = AppPaths.ProjectsDirectory,
            FileName = string.IsNullOrWhiteSpace(Project.Name) ? "untitled" : Project.Name
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await _exportService.ExportPngAsync(Project, dialog.FileName);
    }

    private async Task ImportAndConvertImageAsync()
    {
        if (_pixelConvertService is null)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var projectName = Path.GetFileNameWithoutExtension(dialog.FileName);
        Project = await _pixelConvertService.ConvertAsync(dialog.FileName, 64, 64, 12, projectName);
        Title = $"图片转像素画：{projectName}";
        SelectedColor = Project.Palette.FirstOrDefault();
        RebuildCells();
    }
}
