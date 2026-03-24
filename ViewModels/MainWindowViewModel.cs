using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using pixel_edit.Models;
using pixel_edit.Services;
using System.Linq;

namespace pixel_edit.ViewModels;

/// <summary>
/// 主窗口视图模型，负责应用级导航、侧边栏状态以及各功能页视图模型的创建。
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    /// <summary>
    /// 工程存储服务实例。
    /// </summary>
    private readonly IProjectStorageService _projectStorageService;

    /// <summary>
    /// 颜色别名服务实例。
    /// </summary>
    private readonly IColorAliasService _colorAliasService;

    /// <summary>
    /// 图片转像素服务实例。
    /// </summary>
    private readonly IPixelConvertService _pixelConvertService;

    /// <summary>
    /// 导出服务实例。
    /// </summary>
    private readonly IExportService _exportService;

    /// <summary>
    /// 组合服务实例。
    /// </summary>
    private readonly IComposeService _composeService;

    /// <summary>
    /// 当前显示的页面视图模型。
    /// </summary>
    private object? _currentViewModel;

    /// <summary>
    /// 当前显示的页面视图模型。
    /// </summary>
    public object? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    /// <summary>
    /// 侧边栏是否处于折叠状态。
    /// </summary>
    private bool _isSidebarCollapsed;

    /// <summary>
    /// 侧边栏是否处于折叠状态。
    /// </summary>
    public bool IsSidebarCollapsed
    {
        get => _isSidebarCollapsed;
        set
        {
            if (SetProperty(ref _isSidebarCollapsed, value))
            {
                OnPropertyChanged(nameof(IsSidebarExpanded));
                OnPropertyChanged(nameof(SidebarWidth));
            }
        }
    }

    /// <summary>
    /// 当前激活导航键。
    /// </summary>
    private string _activeNavKey = "home";

    /// <summary>
    /// 当前激活导航键。
    /// </summary>
    public string ActiveNavKey
    {
        get => _activeNavKey;
        set
        {
            if (SetProperty(ref _activeNavKey, value))
            {
                OnPropertyChanged(nameof(HomeNavTag));
                OnPropertyChanged(nameof(ConvertNavTag));
                OnPropertyChanged(nameof(BlankNavTag));
                OnPropertyChanged(nameof(ComposeNavTag));
                OnPropertyChanged(nameof(PageSubtitle));
            }
        }
    }

    /// <summary>
    /// 首页导航按钮标记。
    /// </summary>
    public string HomeNavTag => ActiveNavKey == "home" ? "active" : string.Empty;

    /// <summary>
    /// 图片转换导航按钮标记。
    /// </summary>
    public string ConvertNavTag => ActiveNavKey == "convert" ? "active" : string.Empty;

    /// <summary>
    /// 白板编辑导航按钮标记。
    /// </summary>
    public string BlankNavTag => ActiveNavKey == "blank" ? "active" : string.Empty;

    /// <summary>
    /// 组合导航按钮标记。
    /// </summary>
    public string ComposeNavTag => ActiveNavKey == "compose" ? "active" : string.Empty;

    /// <summary>
    /// 页头副标题文本，用于说明当前页面可执行的主要操作。
    /// </summary>
    public string PageSubtitle => ActiveNavKey switch
    {
        "convert" => "导入图片，调整像素参数并生成像素图。",
        "blank" => "创建空白画布并手工绘制像素作品。",
        "compose" => "加载多个工程并执行拼接或堆叠。",
        _ => "欢迎使用 Pixel Edit，选择左侧功能开始创作。"
    };

    /// <summary>
    /// 侧边栏是否展开。
    /// </summary>
    public bool IsSidebarExpanded => !IsSidebarCollapsed;

    /// <summary>
    /// 侧边栏宽度。
    /// </summary>
    public double SidebarWidth => IsSidebarCollapsed ? 76 : 240;

    /// <summary>
    /// 切换侧边栏展开/折叠命令。
    /// </summary>
    public IRelayCommand ToggleSidebarCommand { get; }

    /// <summary>
    /// 跳转首页命令。
    /// </summary>
    public IRelayCommand GoHomeCommand { get; }

    /// <summary>
    /// 跳转图片转换页命令。
    /// </summary>
    public IRelayCommand GoConvertImageCommand { get; }

    /// <summary>
    /// 跳转白板编辑页命令。
    /// </summary>
    public IRelayCommand GoBlankEditorCommand { get; }

    /// <summary>
    /// 跳转组合页命令。
    /// </summary>
    public IRelayCommand GoComposeCommand { get; }

    /// <summary>
    /// 初始化主窗口视图模型并默认显示首页。
    /// </summary>
    public MainWindowViewModel()
    {
        _projectStorageService = new ProjectStorageService();
        _colorAliasService = new ColorAliasService();
        _pixelConvertService = new PixelConvertService(_colorAliasService);
        _exportService = new ExportService();
        _composeService = new ComposeService();

        ToggleSidebarCommand = new RelayCommand(() => IsSidebarCollapsed = !IsSidebarCollapsed);
        GoHomeCommand = new RelayCommand(ShowHome);
        GoConvertImageCommand = new RelayCommand(OpenImageConverter);
        GoBlankEditorCommand = new RelayCommand(OpenBlankEditor);
        GoComposeCommand = new RelayCommand(OpenCompose);

        ShowHome();
    }

    /// <summary>
    /// 显示首页视图。
    /// </summary>
    private void ShowHome()
    {
        ActiveNavKey = "home";
        CurrentViewModel = new HomeViewModel(
            onCreateBlank: OpenBlankEditor,
            onConvertImage: OpenImageConverter,
            onOpenCompose: OpenCompose);
    }

    /// <summary>
    /// 打开白板编辑页并创建一个默认空白工程。
    /// </summary>
    private void OpenBlankEditor()
    {
        ActiveNavKey = "blank";
        var project = new PixelProject
        {
            Name = $"blank-{DateTime.Now:yyyyMMdd-HHmmss}",
            Canvas = new CanvasSpec { Width = 32, Height = 32, PixelSize = 16 },
            Palette = _colorAliasService.LoadPalette().Select(x => new PaletteEntry
            {
                Alias = x.Alias,
                Hex = x.Hex,
                Name = x.Name
            }).ToList(),
            Layers = [new PixelLayer { Name = "Base", ZIndex = 0, Pixels = Enumerable.Repeat(-1, 32 * 32).ToList() }]
        };

        CurrentViewModel = new EditorViewModel(project, _projectStorageService, _exportService, ShowHome);
    }

    /// <summary>
    /// 打开图片转换编辑页。
    /// </summary>
    private void OpenImageConverter()
    {
        ActiveNavKey = "convert";
        CurrentViewModel = new EditorViewModel(_projectStorageService, _pixelConvertService, _exportService, ShowHome);
    }

    /// <summary>
    /// 打开组合页。
    /// </summary>
    private void OpenCompose()
    {
        ActiveNavKey = "compose";
        CurrentViewModel = new ComposeViewModel(_projectStorageService, _composeService, _exportService, OpenEditorFromProject, ShowHome);
    }

    /// <summary>
    /// 从组合结果打开编辑器。
    /// </summary>
    /// <param name="project">待编辑的工程对象。</param>
    private void OpenEditorFromProject(PixelProject project)
    {
        ActiveNavKey = "compose";
        CurrentViewModel = new EditorViewModel(project, _projectStorageService, _exportService, ShowHome);
    }
}
