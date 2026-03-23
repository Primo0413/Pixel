using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using pixel_edit.Models;
using pixel_edit.Services;
using System.Collections.ObjectModel;

namespace pixel_edit.ViewModels;

public sealed partial class ComposeViewModel : ObservableObject
{
    private readonly IProjectStorageService _projectStorageService;
    private readonly IComposeService _composeService;
    private readonly IExportService _exportService;
    private readonly Action<PixelProject> _openEditor;
    private readonly Action _goHome;

    private readonly List<PixelProject> _loadedProjects = [];

    public ObservableCollection<string> LoadedProjectNames { get; } = [];

    [ObservableProperty]
    private ComposeMode selectedMode = ComposeMode.Stack;

    [ObservableProperty]
    private PixelProject? composedProject;

    public Array Modes => Enum.GetValues(typeof(ComposeMode));

    public IRelayCommand BackHomeCommand { get; }
    public IAsyncRelayCommand AddProjectsCommand { get; }
    public IRelayCommand ComposeCommand { get; }
    public IRelayCommand OpenInEditorCommand { get; }
    public IAsyncRelayCommand SaveComposedProjectCommand { get; }
    public IAsyncRelayCommand ExportPngCommand { get; }

    public ComposeViewModel(IProjectStorageService projectStorageService, IComposeService composeService, IExportService exportService, Action<PixelProject> openEditor, Action goHome)
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

    private void OpenInEditor()
    {
        if (ComposedProject is null)
        {
            return;
        }

        _openEditor(ComposedProject);
    }

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
