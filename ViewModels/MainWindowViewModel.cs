using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using pixel_edit.Models;
using pixel_edit.Services;
using System.Linq;

namespace pixel_edit.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IProjectStorageService _projectStorageService;
    private readonly IColorAliasService _colorAliasService;
    private readonly IPixelConvertService _pixelConvertService;
    private readonly IExportService _exportService;
    private readonly IComposeService _composeService;

    private object? _currentViewModel;
    public object? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    private bool _isSidebarCollapsed;
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

    private string _activeNavKey = "home";
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
            }
        }
    }

    public string HomeNavTag => ActiveNavKey == "home" ? "active" : string.Empty;
    public string ConvertNavTag => ActiveNavKey == "convert" ? "active" : string.Empty;
    public string BlankNavTag => ActiveNavKey == "blank" ? "active" : string.Empty;
    public string ComposeNavTag => ActiveNavKey == "compose" ? "active" : string.Empty;

    public bool IsSidebarExpanded => !IsSidebarCollapsed;
    public double SidebarWidth => IsSidebarCollapsed ? 76 : 240;

    public IRelayCommand ToggleSidebarCommand { get; }
    public IRelayCommand GoHomeCommand { get; }
    public IRelayCommand GoConvertImageCommand { get; }
    public IRelayCommand GoBlankEditorCommand { get; }
    public IRelayCommand GoComposeCommand { get; }

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

    private void ShowHome()
    {
        ActiveNavKey = "home";
        CurrentViewModel = new HomeViewModel(
            onCreateBlank: OpenBlankEditor,
            onConvertImage: OpenImageConverter,
            onOpenCompose: OpenCompose);
    }

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

    private void OpenImageConverter()
    {
        ActiveNavKey = "convert";
        CurrentViewModel = new EditorViewModel(_projectStorageService, _pixelConvertService, _exportService, ShowHome);
    }

    private void OpenCompose()
    {
        ActiveNavKey = "compose";
        CurrentViewModel = new ComposeViewModel(_projectStorageService, _composeService, _exportService, OpenEditorFromProject, ShowHome);
    }

    private void OpenEditorFromProject(PixelProject project)
    {
        ActiveNavKey = "compose";
        CurrentViewModel = new EditorViewModel(project, _projectStorageService, _exportService, ShowHome);
    }
}
