using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using pixel_edit.Models;
using pixel_edit.Services;
using System.Collections.ObjectModel;

namespace pixel_edit.ViewModels;

/// <summary>
/// 组合页面视图模型。
/// 负责加载多个本地工程、执行拼接/堆叠、导出结果和回到编辑器继续修改。
/// </summary>
public sealed partial class ComposeViewModel : ObservableObject
{
    /// <summary>
    /// 工程读写服务。
    /// </summary>
    private readonly IProjectStorageService _projectStorageService;

    /// <summary>
    /// 组合算法服务。
    /// </summary>
    private readonly IComposeService _composeService;

    /// <summary>
    /// PNG 导出服务。
    /// </summary>
    private readonly IExportService _exportService;

    /// <summary>
    /// 打开编辑器回调。
    /// </summary>
    private readonly Action<PixelProject> _openEditor;

    /// <summary>
    /// 返回首页回调。
    /// </summary>
    private readonly Action _goHome;

    /// <summary>
    /// 内部缓存：已加载的工程对象。
    /// </summary>
    private readonly List<PixelProject> _loadedProjects = [];

    /// <summary>
    /// 已加载工程名称列表（用于界面展示）。
    /// </summary>
    public ObservableCollection<string> LoadedProjectNames { get; } = [];

    /// <summary>
    /// 当前选中的组合模式。
    /// </summary>
    [ObservableProperty]
    private ComposeMode selectedMode = ComposeMode.Stack;

    /// <summary>
    /// 当前组合结果工程。
    /// </summary>
    [ObservableProperty]
    private PixelProject? composedProject;

    /// <summary>
    /// 所有可选组合模式枚举值。
    /// </summary>
    public Array Modes => Enum.GetValues(typeof(ComposeMode));

    /// <summary>
    /// 返回首页命令。
    /// </summary>
    public IRelayCommand BackHomeCommand { get; }

    /// <summary>
    /// 加载多个工程命令。
    /// </summary>
    public IAsyncRelayCommand AddProjectsCommand { get; }

    /// <summary>
    /// 执行组合命令。
    /// </summary>
    public IRelayCommand ComposeCommand { get; }

    /// <summary>
    /// 在编辑器中打开组合结果命令。
    /// </summary>
    public IRelayCommand OpenInEditorCommand { get; }

    /// <summary>
    /// 保存组合工程命令。
    /// </summary>
    public IAsyncRelayCommand SaveComposedProjectCommand { get; }

    /// <summary>
    /// 导出组合结果 PNG 命令。
    /// </summary>
    public IAsyncRelayCommand ExportPngCommand { get; }

    /// <summary>
    /// 初始化组合页面视图模型。
    /// </summary>
    /// <param name="projectStorageService">工程存储服务。</param>
    /// <param name="composeService">组合服务。</param>
    /// <param name="exportService">导出服务。</param>
    /// <param name="openEditor">打开编辑器回调。</param>
    /// <param name="goHome">返回首页回调。</param>
    public ComposeViewModel(
        IProjectStorageService projectStorageService,
        IComposeService composeService,
        IExportService exportService,
        Action<PixelProject> openEditor,
        Action goHome)
    {
        _projectStorageService = projectStorageService;
        _composeService = composeService;
        _exportService = exportService;
        _openEditor = openEditor;
        _goHome = goHome;

        BackHomeCommand = new RelayCommand(() => _goHome());
        AddProjectsCommand = new AsyncRelayCommand(AddProjectsAsync);
        ComposeCommand = new RelayCommand(Compose);
        OpenInEditorCommand = new RelayCommand(OpenInEditor);
        SaveComposedProjectCommand = new AsyncRelayCommand(SaveComposedProjectAsync);
        ExportPngCommand = new AsyncRelayCommand(ExportPngAsync);
    }

    /// <summary>
    /// 打开文件选择框并加载多个本地工程。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async Task AddProjectsAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Pixel Project (*.pxproj.json)|*.pxproj.json",
            InitialDirectory = AppPaths.ProjectsDirectory,
            CheckFileExists = true,
            Multiselect = true
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        foreach (var file in dialog.FileNames)
        {
            var project = await _projectStorageService.LoadAsync(file);
            _loadedProjects.Add(project);
            LoadedProjectNames.Add(project.Name);
        }
    }

    /// <summary>
    /// 按当前模式执行工程组合并生成结果工程。
    /// </summary>
    private void Compose()
    {
        if (_loadedProjects.Count == 0)
        {
            return;
        }

        ComposedProject = _composeService.Compose(
            _loadedProjects,
            SelectedMode,
            _loadedProjects[0].Canvas.PixelSize,
            $"composed-{DateTime.Now:yyyyMMdd-HHmmss}");
    }

    /// <summary>
    /// 将当前组合结果在编辑器中打开。
    /// </summary>
    private void OpenInEditor()
    {
        if (ComposedProject is null)
        {
            return;
        }

        _openEditor(ComposedProject);
    }

    /// <summary>
    /// 保存当前组合结果工程到本地文件。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async Task SaveComposedProjectAsync()
    {
        if (ComposedProject is null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Pixel Project (*.pxproj.json)|*.pxproj.json",
            InitialDirectory = AppPaths.ProjectsDirectory,
            FileName = ComposedProject.Name
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await _projectStorageService.SaveAsync(dialog.FileName, ComposedProject);
    }

    /// <summary>
    /// 将当前组合结果导出为 PNG。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async Task ExportPngAsync()
    {
        if (ComposedProject is null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "PNG Image (*.png)|*.png",
            InitialDirectory = AppPaths.ProjectsDirectory,
            FileName = ComposedProject.Name
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await _exportService.ExportPngAsync(ComposedProject, dialog.FileName);
    }
}
