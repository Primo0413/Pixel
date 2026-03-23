using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace pixel_edit.ViewModels;

public sealed class HomeViewModel : ObservableObject
{
    private readonly Action _onCreateBlank;
    private readonly Action _onConvertImage;
    private readonly Action _onOpenCompose;

    public IRelayCommand CreateBlankCommand { get; }
    public IRelayCommand ConvertImageCommand { get; }
    public IRelayCommand OpenComposeCommand { get; }

    public HomeViewModel(Action onCreateBlank, Action onConvertImage, Action onOpenCompose)
    {
        _onCreateBlank = onCreateBlank;
        _onConvertImage = onConvertImage;
        _onOpenCompose = onOpenCompose;

        CreateBlankCommand = new RelayCommand(() => _onCreateBlank());
        ConvertImageCommand = new RelayCommand(() => _onConvertImage());
        OpenComposeCommand = new RelayCommand(() => _onOpenCompose());
    }
}
