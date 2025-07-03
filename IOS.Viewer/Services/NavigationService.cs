using System;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace IOS.Viewer.Services;

/// <summary>
/// 导航服务接口
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// 导航到指定页面
    /// </summary>
    void NavigateTo<T>() where T : UserControl, new();
    
    /// <summary>
    /// 返回上一页
    /// </summary>
    void NavigateBack();
    
    /// <summary>
    /// 设置主容器
    /// </summary>
    void SetMainContainer(ContentControl container);
}

/// <summary>
/// 导航服务实现
/// </summary>
public class NavigationService : INavigationService
{
    private readonly ILogger<NavigationService>? _logger;
    private ContentControl? _mainContainer;
    private UserControl? _previousView;
    private UserControl? _currentView;

    public NavigationService()
    {
        _logger = App.GetService<ILogger<NavigationService>>();
    }

    /// <summary>
    /// 设置主容器
    /// </summary>
    public void SetMainContainer(ContentControl container)
    {
        _mainContainer = container;
        _logger?.LogInformation("导航服务主容器已设置");
    }

    /// <summary>
    /// 导航到指定页面
    /// </summary>
    public void NavigateTo<T>() where T : UserControl, new()
    {
        if (_mainContainer == null)
        {
            _logger?.LogWarning("主容器未设置，无法导航");
            return;
        }

        try
        {
            // 保存当前视图作为上一个视图
            _previousView = _currentView;
            
            // 创建新视图
            _currentView = new T();
            
            // 设置到主容器
            _mainContainer.Content = _currentView;
            
            _logger?.LogInformation("已导航到页面: {PageType}", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "导航到页面 {PageType} 时发生错误", typeof(T).Name);
        }
    }

    /// <summary>
    /// 返回上一页
    /// </summary>
    public void NavigateBack()
    {
        if (_mainContainer == null)
        {
            _logger?.LogWarning("主容器未设置，无法返回");
            return;
        }

        if (_previousView != null)
        {
            try
            {
                // 恢复上一个视图
                _mainContainer.Content = _previousView;
                _currentView = _previousView;
                _previousView = null;
                
                _logger?.LogInformation("已返回上一页");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "返回上一页时发生错误");
            }
        }
        else
        {
            _logger?.LogWarning("没有上一页可以返回");
        }
    }
} 