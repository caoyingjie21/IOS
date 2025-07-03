using Avalonia.Controls;
using IOS.Viewer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IOS.Viewer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // 初始化导航服务
        InitializeNavigation();
    }
    
    private void InitializeNavigation()
    {
        var navigationService = App.GetService<INavigationService>();
        if (navigationService != null && MainContentContainer != null)
        {
            navigationService.SetMainContainer(MainContentContainer);
        }
    }
}
