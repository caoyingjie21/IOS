using Avalonia.Controls;
using IOS.Viewer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace IOS.Viewer.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        // 通过依赖注入获取ViewModel（如果需要的话）
        // DataContext = App.GetService<SettingsViewModel>() ?? new SettingsViewModel();
        DataContext = new SettingsViewModel();
    }
} 