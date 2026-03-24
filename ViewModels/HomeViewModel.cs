using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace pixel_edit.ViewModels;

/// <summary>
/// 首页视图模型，负责将首页按钮动作转发到主窗口导航逻辑。
/// </summary>
public sealed class HomeViewModel : ObservableObject
{
    /// <summary>
    /// 创建空白编辑器的回调。
    /// </summary>
    private readonly Action _onCreateBlank;

    /// <summary>
    /// 打开图片转换编辑器的回调。
    /// </summary>
    private readonly Action _onConvertImage;

    /// <summary>
    /// 打开组合页面的回调。
    /// </summary>
    private readonly Action _onOpenCompose;

    /// <summary>
    /// 创建白板工程命令。
    /// </summary>
    public IRelayCommand CreateBlankCommand { get; }

    /// <summary>
    /// 打开图片转像素功能命令。
    /// </summary>
    public IRelayCommand ConvertImageCommand { get; }

    /// <summary>
    /// 打开拼接/堆叠功能命令。
    /// </summary>
    public IRelayCommand OpenComposeCommand { get; }

    /// <summary>
    /// 初始化首页视图模型。
    /// </summary>
    /// <param name="onCreateBlank">点击“创建白板”后的处理逻辑。</param>
    /// <param name="onConvertImage">点击“图片转像素”后的处理逻辑。</param>
    /// <param name="onOpenCompose">点击“拼接/堆叠”后的处理逻辑。</param>
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
