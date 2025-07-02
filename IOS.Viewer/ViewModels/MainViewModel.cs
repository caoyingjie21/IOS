using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia;
using System.Runtime.InteropServices;
using Material.Icons;

namespace IOS.Viewer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private int _stepIndex = 1;
    [ObservableProperty] private double _progressValue = 52;
    [ObservableProperty] private bool _isTextVisible = true;
    [ObservableProperty] private bool _isIndeterminate;
    
    // 导航相关属性
    [ObservableProperty] private bool _isPanelSelected = true;
    [ObservableProperty] private bool _isSettingsSelected = false;
    
    // MaterialIcon 属性
    public MaterialIconKind PanelIcon => MaterialIconKind.Home;
    public MaterialIconKind SettingsIcon => MaterialIconKind.Settings;
    
    /// <summary>
    /// 切换到面板视图
    /// </summary>
    [RelayCommand]
    private void ShowPanel()
    {
        IsPanelSelected = true;
        IsSettingsSelected = false;
    }
    
    /// <summary>
    /// 切换到设置视图
    /// </summary>
    [RelayCommand]
    private void ShowSettings()
    {
        IsPanelSelected = false;
        IsSettingsSelected = true;
    }
    
    /// <summary>
    /// 检测是否为移动平台
    /// </summary>
    public bool IsMobile
    {
        get
        {
            try
            {
                // 检测Android平台
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID")))
                    return true;
                
                // 检测iOS平台  
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("IOS")))
                    return true;
                
                return false;
            }
            catch
            {
                // 如果无法检测，默认为桌面平台
                return false;
            }
        }
    }
}
