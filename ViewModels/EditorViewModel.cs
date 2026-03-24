using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using pixel_edit.Models;
using pixel_edit.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace pixel_edit.ViewModels;

/// <summary>
/// 编辑器视图模型。
/// 负责图片转换后的像素画编辑、橡皮擦除、缩放、导入/导出、颜色统计与原图对比展示。
/// </summary>
public sealed partial class EditorViewModel : ObservableObject
{
    /// <summary>
    /// 颜色使用统计条目。
    /// </summary>
    public sealed class ColorUsageItem
    {
        /// <summary>
        /// 颜色别名（例如 A1、M6）。
        /// </summary>
        public string Alias { get; init; } = string.Empty;

        /// <summary>
        /// 颜色十六进制值（#RRGGBB）。
        /// </summary>
        public string Hex { get; init; } = "#000000";

        /// <summary>
        /// 该颜色在当前图层中被使用的像素数量。
        /// </summary>
        public int Count { get; init; }
    }

    /// <summary>
    /// 单次擦除动作记录，用于“退回擦除”功能。
    /// </summary>
    /// <param name="X">被擦除像素的 X 坐标。</param>
    /// <param name="Y">被擦除像素的 Y 坐标。</param>
    /// <param name="PreviousPaletteIndex">擦除前的调色板索引。</param>
    private sealed record EraseAction(int X, int Y, int PreviousPaletteIndex);

    /// <summary>
    /// 擦除撤回栈（后进先出）。
    /// </summary>
    private readonly Stack<EraseAction> _eraseUndoStack = new();

    /// <summary>
    /// 工程存储服务。
    /// </summary>
    private readonly IProjectStorageService _projectStorageService;

    /// <summary>
    /// 图片转像素服务（仅在图片转换模式下可用）。
    /// </summary>
    private readonly IPixelConvertService? _pixelConvertService;

    /// <summary>
    /// PNG 导出服务。
    /// </summary>
    private readonly IExportService _exportService;

    /// <summary>
    /// 返回首页回调。
    /// </summary>
    private readonly Action _goHome;

    /// <summary>
    /// 页面标题。
    /// </summary>
    [ObservableProperty]
    private string title = "编辑器";

    /// <summary>
    /// 当前编辑中的工程对象。
    /// </summary>
    [ObservableProperty]
    private PixelProject? project;

    /// <summary>
    /// 当前选中的调色板颜色。
    /// </summary>
    [ObservableProperty]
    private PaletteEntry? selectedColor;

    /// <summary>
    /// 当前选中的像素格。
    /// </summary>
    [ObservableProperty]
    private PixelCell? selectedCell;

    /// <summary>
    /// 是否启用橡皮模式。
    /// </summary>
    [ObservableProperty]
    private bool eraserMode;

    /// <summary>
    /// 横向像素点数量（用于图片转换时控制目标宽度）。
    /// </summary>
    [ObservableProperty]
    private int horizontalPixelCount = 128;

    /// <summary>
    /// 原图路径（用于重新生成时复用）。
    /// </summary>
    [ObservableProperty]
    private string? originalImagePath;

    /// <summary>
    /// 原图位图源（用于叠加对比）。
    /// </summary>
    [ObservableProperty]
    private BitmapSource? originalImageSource;

    /// <summary>
    /// 是否显示原图参考层。
    /// </summary>
    [ObservableProperty]
    private bool showOriginalReference = true;

    /// <summary>
    /// 是否处于图片转换中。
    /// </summary>
    [ObservableProperty]
    private bool isConverting;

    /// <summary>
    /// 画布缩放百分比（25%~400%）。
    /// </summary>
    [ObservableProperty]
    private int zoomPercent = 100;

    /// <summary>
    /// 画布像素格集合（渲染主数据源）。
    /// </summary>
    public ObservableCollection<PixelCell> Cells { get; } = [];

    /// <summary>
    /// 颜色统计集合（仅显示实际使用到的颜色）。
    /// </summary>
    public ObservableCollection<ColorUsageItem> ColorUsageStats { get; } = [];

    /// <summary>
    /// 当前工程实际使用到的颜色总数。
    /// </summary>
    public int UsedColorCount => ColorUsageStats.Count;

    /// <summary>
    /// 原图参考层透明度。
    /// </summary>
    public double OriginalReferenceOpacity => ShowOriginalReference ? 1.0 : 0.0;

    /// <summary>
    /// 像素层透明度（显示原图参考时适当降低以便对比）。
    /// </summary>
    public double PixelLayerOpacity => ShowOriginalReference ? 0.8 : 1.0;

    /// <summary>
    /// 网格线粗细（显示原图参考时隐藏网格线）。
    /// </summary>
    public double PixelGridLineThickness => ShowOriginalReference ? 0.0 : 0.5;

    /// <summary>
    /// 当前是否允许用户交互（转换中会锁定）。
    /// </summary>
    public bool IsInteractionEnabled => !IsConverting;

    /// <summary>
    /// “重新生成”按钮文案。
    /// </summary>
    public string RegenerateButtonText => IsConverting ? "生成中..." : "重新生成像素图";

    /// <summary>
    /// 是否存在可撤回的擦除动作。
    /// </summary>
    public bool CanUndoErase => _eraseUndoStack.Count > 0;

    /// <summary>
    /// 单个像素格渲染尺寸。
    /// </summary>
    public int PixelCellSize => Math.Max(1, Project?.Canvas.PixelSize ?? 6);

    /// <summary>
    /// 像素画渲染宽度。
    /// </summary>
    public double PixelRenderWidth => (Project?.Canvas.Width ?? 0) * PixelCellSize;

    /// <summary>
    /// 像素画渲染高度。
    /// </summary>
    public double PixelRenderHeight => (Project?.Canvas.Height ?? 0) * PixelCellSize;

    /// <summary>
    /// 画布缩放系数。
    /// </summary>
    public double CanvasScale => ZoomPercent / 100.0;

    /// <summary>
    /// 返回首页命令。
    /// </summary>
    public IRelayCommand BackHomeCommand { get; }

    /// <summary>
    /// 保存工程命令。
    /// </summary>
    public IAsyncRelayCommand SaveProjectCommand { get; }

    /// <summary>
    /// 加载工程命令。
    /// </summary>
    public IAsyncRelayCommand LoadProjectCommand { get; }

    /// <summary>
    /// 导出 PNG 命令。
    /// </summary>
    public IAsyncRelayCommand ExportPngCommand { get; }

    /// <summary>
    /// 绘制/擦除像素命令。
    /// </summary>
    public IRelayCommand<PixelCell> PaintCellCommand { get; }

    /// <summary>
    /// 删除当前选中像素命令。
    /// </summary>
    public IRelayCommand DeleteSelectedPixelCommand { get; }

    /// <summary>
    /// 撤回上一条擦除命令。
    /// </summary>
    public IRelayCommand UndoEraseCommand { get; }

    /// <summary>
    /// 放大命令。
    /// </summary>
    public IRelayCommand ZoomInCommand { get; }

    /// <summary>
    /// 缩小命令。
    /// </summary>
    public IRelayCommand ZoomOutCommand { get; }

    /// <summary>
    /// 缩放重置命令。
    /// </summary>
    public IRelayCommand ResetZoomCommand { get; }

    /// <summary>
    /// 导入并转换图片命令。
    /// </summary>
    public IAsyncRelayCommand ImportAndConvertImageCommand { get; }

    /// <summary>
    /// 使用当前参数重新生成像素图命令。
    /// </summary>
    public IAsyncRelayCommand RegeneratePixelImageCommand { get; }

    /// <summary>
    /// 初始化“普通编辑模式”构造函数（已有工程）。
    /// </summary>
    /// <param name="initialProject">初始工程。</param>
    /// <param name="projectStorageService">工程存储服务。</param>
    /// <param name="exportService">导出服务。</param>
    /// <param name="goHome">返回首页回调。</param>
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
        UndoEraseCommand = new RelayCommand(UndoErase);
        ZoomInCommand = new RelayCommand(ZoomIn);
        ZoomOutCommand = new RelayCommand(ZoomOut);
        ResetZoomCommand = new RelayCommand(ResetZoom);
        ImportAndConvertImageCommand = new AsyncRelayCommand(() => Task.CompletedTask);
        RegeneratePixelImageCommand = new AsyncRelayCommand(() => Task.CompletedTask);

        RebuildCells();
    }

    /// <summary>
    /// 初始化“图片转换模式”构造函数。
    /// </summary>
    /// <param name="projectStorageService">工程存储服务。</param>
    /// <param name="pixelConvertService">图片转换服务。</param>
    /// <param name="exportService">导出服务。</param>
    /// <param name="goHome">返回首页回调。</param>
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
        UndoEraseCommand = new RelayCommand(UndoErase);
        ZoomInCommand = new RelayCommand(ZoomIn);
        ZoomOutCommand = new RelayCommand(ZoomOut);
        ResetZoomCommand = new RelayCommand(ResetZoom);
        ImportAndConvertImageCommand = new AsyncRelayCommand(ImportAndConvertImageAsync);
        RegeneratePixelImageCommand = new AsyncRelayCommand(RegeneratePixelImageAsync);

        Title = "图片转像素画";
    }

    /// <summary>
    /// 当“显示原图参考”状态变化时，刷新相关衍生属性绑定。
    /// </summary>
    /// <param name="value">新的显示状态。</param>
    partial void OnShowOriginalReferenceChanged(bool value)
    {
        OnPropertyChanged(nameof(OriginalReferenceOpacity));
        OnPropertyChanged(nameof(PixelLayerOpacity));
        OnPropertyChanged(nameof(PixelGridLineThickness));
    }

    /// <summary>
    /// 当转换状态变化时，刷新交互可用性与按钮文案。
    /// </summary>
    /// <param name="value">新的转换状态。</param>
    partial void OnIsConvertingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsInteractionEnabled));
        OnPropertyChanged(nameof(RegenerateButtonText));
    }

    /// <summary>
    /// 当工程对象变化时，重置依赖状态并刷新相关显示。
    /// </summary>
    /// <param name="value">新的工程对象。</param>
    partial void OnProjectChanged(PixelProject? value)
    {
        OnPropertyChanged(nameof(PixelCellSize));
        OnPropertyChanged(nameof(PixelRenderWidth));
        OnPropertyChanged(nameof(PixelRenderHeight));
        _eraseUndoStack.Clear();
        OnPropertyChanged(nameof(CanUndoErase));
        RebuildColorUsageStats();
    }

    /// <summary>
    /// 当缩放百分比变化时进行范围限制并刷新缩放系数。
    /// </summary>
    /// <param name="value">新缩放百分比。</param>
    partial void OnZoomPercentChanged(int value)
    {
        if (value < 25)
        {
            ZoomPercent = 25;
            return;
        }

        if (value > 400)
        {
            ZoomPercent = 400;
            return;
        }

        OnPropertyChanged(nameof(CanvasScale));
    }

    /// <summary>
    /// 当横向像素点数量变化时进行范围限制。
    /// </summary>
    /// <param name="value">新的横向像素点数量。</param>
    partial void OnHorizontalPixelCountChanged(int value)
    {
        if (value < 1)
        {
            HorizontalPixelCount = 1;
            return;
        }

        if (value > 1024)
        {
            HorizontalPixelCount = 1024;
        }
    }

    /// <summary>
    /// 重新统计当前主图层的颜色使用情况。
    /// </summary>
    private void RebuildColorUsageStats()
    {
        ColorUsageStats.Clear();

        if (Project is null || Project.Layers.Count == 0)
        {
            OnPropertyChanged(nameof(UsedColorCount));
            return;
        }

        var layer = Project.Layers.OrderBy(l => l.ZIndex).Last();
        var usage = new int[Project.Palette.Count];
        foreach (var index in layer.Pixels)
        {
            if (index >= 0 && index < usage.Length)
            {
                usage[index]++;
            }
        }

        for (var i = 0; i < Project.Palette.Count; i++)
        {
            if (usage[i] == 0)
            {
                continue;
            }

            var p = Project.Palette[i];
            ColorUsageStats.Add(new ColorUsageItem
            {
                Alias = p.Alias,
                Hex = p.Hex,
                Count = usage[i]
            });
        }

        OnPropertyChanged(nameof(UsedColorCount));
    }

    /// <summary>
    /// 根据当前工程数据重建用于界面渲染的像素格集合。
    /// </summary>
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

    /// <summary>
    /// 对单个像素格执行上色或擦除操作。
    /// </summary>
    /// <param name="cell">目标像素格。</param>
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
            var previousPaletteIndex = layer.Pixels[index];
            if (previousPaletteIndex == -1)
            {
                return;
            }

            _eraseUndoStack.Push(new EraseAction(cell.X, cell.Y, previousPaletteIndex));
            OnPropertyChanged(nameof(CanUndoErase));

            layer.Pixels[index] = -1;
            cell.PaletteIndex = -1;
            cell.Brush = Brushes.Transparent;
            RebuildColorUsageStats();
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
        RebuildColorUsageStats();
    }

    /// <summary>
    /// 撤回最近一次擦除操作。
    /// </summary>
    private void UndoErase()
    {
        if (Project is null || Project.Layers.Count == 0 || _eraseUndoStack.Count == 0)
        {
            return;
        }

        var action = _eraseUndoStack.Pop();
        OnPropertyChanged(nameof(CanUndoErase));

        var layer = Project.Layers.OrderBy(l => l.ZIndex).Last();
        var index = action.Y * Project.Canvas.Width + action.X;
        if (index < 0 || index >= layer.Pixels.Count)
        {
            return;
        }

        layer.Pixels[index] = action.PreviousPaletteIndex;

        var cell = Cells.FirstOrDefault(c => c.X == action.X && c.Y == action.Y);
        if (cell is null)
        {
            return;
        }

        cell.PaletteIndex = action.PreviousPaletteIndex;
        if (action.PreviousPaletteIndex >= 0 && action.PreviousPaletteIndex < Project.Palette.Count)
        {
            cell.Brush = new SolidColorBrush(ColorHelper.FromHex(Project.Palette[action.PreviousPaletteIndex].Hex));
        }
        else
        {
            cell.Brush = Brushes.Transparent;
        }

        RebuildColorUsageStats();
    }

    /// <summary>
    /// 放大画布显示比例。
    /// </summary>
    private void ZoomIn()
    {
        ZoomPercent = Math.Min(400, ZoomPercent + 25);
    }

    /// <summary>
    /// 缩小画布显示比例。
    /// </summary>
    private void ZoomOut()
    {
        ZoomPercent = Math.Max(25, ZoomPercent - 25);
    }

    /// <summary>
    /// 重置画布缩放比例为 100%。
    /// </summary>
    private void ResetZoom()
    {
        ZoomPercent = 100;
    }

    /// <summary>
    /// 删除当前选中的像素点（内部通过一次橡皮操作实现）。
    /// </summary>
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

    /// <summary>
    /// 将当前工程保存到本地文件。
    /// </summary>
    /// <returns>异步任务。</returns>
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

    /// <summary>
    /// 从本地文件加载工程并刷新编辑器。
    /// </summary>
    /// <returns>异步任务。</returns>
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

    /// <summary>
    /// 将当前工程导出为 PNG。
    /// </summary>
    /// <returns>异步任务。</returns>
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

    /// <summary>
    /// 导入图片并按当前参数执行首次转换。
    /// </summary>
    /// <returns>异步任务。</returns>
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

        OriginalImagePath = dialog.FileName;
        OriginalImageSource = LoadBitmap(dialog.FileName);

        await ReconvertFromOriginalImageAsync();
    }

    /// <summary>
    /// 基于已导入原图与当前参数重新生成像素图。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async Task RegeneratePixelImageAsync()
    {
        await ReconvertFromOriginalImageAsync();
    }

    /// <summary>
    /// 执行图片转像素核心流程（后台线程运行，前台仅更新状态与结果）。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async Task ReconvertFromOriginalImageAsync()
    {
        if (_pixelConvertService is null || string.IsNullOrWhiteSpace(OriginalImagePath))
        {
            return;
        }

        IsConverting = true;
        try
        {
            var imagePath = OriginalImagePath;
            var horizontalPixelCount = HorizontalPixelCount;
            var projectName = Path.GetFileNameWithoutExtension(imagePath);

            var convertedProject = await Task.Run(() =>
                _pixelConvertService.ConvertAsync(imagePath, horizontalPixelCount, projectName));

            Project = convertedProject;
            Title = $"图片转像素画：{projectName}";
            SelectedColor = Project.Palette.FirstOrDefault();
            RebuildCells();
        }
        finally
        {
            IsConverting = false;
        }
    }

    /// <summary>
    /// 从磁盘加载图片为可复用的位图源。
    /// </summary>
    /// <param name="path">图片绝对路径。</param>
    /// <returns>已冻结（Freeze）的位图对象。</returns>
    private static BitmapImage LoadBitmap(string path)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }
}
