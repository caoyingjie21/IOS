using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using IOS.Viewer.Services;
using IOS.Viewer.Views;
using System;
using Avalonia.Threading;

namespace IOS.Viewer.ViewModels;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<SettingsViewModel>? _logger;
    private readonly INavigationService? _navigationService;

    // CoderStatus 服务属性
    [ObservableProperty] private string _coderStatusText = "未连接";
    [ObservableProperty] private string _coderStatusMessage = "等待消息...";

    // DataServerStatus 服务属性
    [ObservableProperty] private string _dataServerStatusText = "未连接";
    [ObservableProperty] private string _dataServerStatusMessage = "等待消息...";

    // SchedulerStatus 服务属性
    [ObservableProperty] private string _schedulerStatusText = "未连接";
    [ObservableProperty] private string _schedulerStatusMessage = "等待消息...";

    // MotionStatus 服务属性
    [ObservableProperty] private string _motionStatusText = "未连接";
    [ObservableProperty] private string _motionStatusMessage = "等待消息...";

    // Vision 服务属性
    [ObservableProperty] private string _visionStatusText = "未连接";
    [ObservableProperty] private string _visionStatusMessage = "等待消息...";

    public SettingsViewModel()
    {
        _logger = App.GetService<ILogger<SettingsViewModel>>();
        _navigationService = App.GetService<INavigationService>();
        
        // 订阅MQTT消息事件
        ViewerHostService.MqttMessageReceived += OnMqttMessageReceived;
        
        _logger?.LogInformation("设置页面已初始化，开始监听5个服务的MQTT消息");
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        // 取消订阅MQTT消息事件
        ViewerHostService.MqttMessageReceived -= OnMqttMessageReceived;
        _logger?.LogInformation("SettingsViewModel 已释放资源");
    }

    /// <summary>
    /// 处理接收到的MQTT消息
    /// </summary>
    private void OnMqttMessageReceived(string topic, string message)
    {
        // 确保在UI线程上更新界面
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                var displayMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
                
                // 根据topic路由到对应的服务
                if (topic.StartsWith("ios/v1/coder/", StringComparison.OrdinalIgnoreCase))
                {
                    UpdateCoderStatus(displayMessage);
                }
                else if (topic.StartsWith("ios/v1/data/", StringComparison.OrdinalIgnoreCase))
                {
                    UpdateDataServerStatus(displayMessage);
                }
                else if (topic.StartsWith("ios/v1/scheduler/", StringComparison.OrdinalIgnoreCase))
                {
                    UpdateSchedulerStatus(displayMessage);
                }
                else if (topic.StartsWith("ios/v1/motion/", StringComparison.OrdinalIgnoreCase))
                {
                    UpdateMotionStatus(displayMessage);
                }
                else if (topic.StartsWith("ios/v1/vision/", StringComparison.OrdinalIgnoreCase))
                {
                    UpdateVisionStatus(displayMessage);
                }
                
                _logger?.LogDebug("已处理MQTT消息: Topic={Topic}, Message={Message}", topic, message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理MQTT消息时发生错误: Topic={Topic}", topic);
            }
        });
    }

    /// <summary>
    /// 更新CoderStatus服务状态
    /// </summary>
    private void UpdateCoderStatus(string message)
    {
        CoderStatusText = "运行中";
        CoderStatusMessage = message;
    }

    /// <summary>
    /// 更新DataServerStatus服务状态
    /// </summary>
    private void UpdateDataServerStatus(string message)
    {
        DataServerStatusText = "运行中";
        DataServerStatusMessage = message;
    }

    /// <summary>
    /// 更新SchedulerStatus服务状态
    /// </summary>
    private void UpdateSchedulerStatus(string message)
    {
        SchedulerStatusText = "运行中";
        SchedulerStatusMessage = message;
    }

    /// <summary>
    /// 更新MotionStatus服务状态
    /// </summary>
    private void UpdateMotionStatus(string message)
    {
        MotionStatusText = "运行中";
        MotionStatusMessage = message;
    }

    /// <summary>
    /// 更新Vision服务状态
    /// </summary>
    private void UpdateVisionStatus(string message)
    {
        VisionStatusText = "运行中";
        VisionStatusMessage = message;
    }

    /// <summary>
    /// 返回主页命令
    /// </summary>
    [RelayCommand]
    private void NavigateBack()
    {
        _logger?.LogInformation("用户点击返回主页");
        _navigationService?.NavigateTo<MainView>();
    }

    /// <summary>
    /// 重启MQTT服务命令
    /// </summary>
    [RelayCommand]
    private void RestartMqttService()
    {
        _logger?.LogInformation("用户请求重启MQTT服务");
        // TODO: 实现MQTT服务重启逻辑
    }

    /// <summary>
    /// 配置MQTT服务命令
    /// </summary>
    [RelayCommand]
    private void ConfigureMqttService()
    {
        _logger?.LogInformation("用户请求配置MQTT服务");
        // TODO: 实现MQTT服务配置逻辑
    }

    /// <summary>
    /// 启动数据处理服务命令
    /// </summary>
    [RelayCommand]
    private void StartDataProcessingService()
    {
        _logger?.LogInformation("用户请求启动数据处理服务");
        // TODO: 实现数据处理服务启动逻辑
    }

    /// <summary>
    /// 配置数据处理服务命令
    /// </summary>
    [RelayCommand]
    private void ConfigureDataProcessingService()
    {
        _logger?.LogInformation("用户请求配置数据处理服务");
        // TODO: 实现数据处理服务配置逻辑
    }

    /// <summary>
    /// 查看日志服务命令
    /// </summary>
    [RelayCommand]
    private void ViewLogService()
    {
        _logger?.LogInformation("用户请求查看日志服务");
        // TODO: 实现日志服务查看逻辑
    }

    /// <summary>
    /// 配置日志服务命令
    /// </summary>
    [RelayCommand]
    private void ConfigureLogService()
    {
        _logger?.LogInformation("用户请求配置日志服务");
        // TODO: 实现日志服务配置逻辑
    }

    /// <summary>
    /// 重启API服务命令
    /// </summary>
    [RelayCommand]
    private void RestartApiService()
    {
        _logger?.LogInformation("用户请求重启API服务");
        // TODO: 实现API服务重启逻辑
    }

    /// <summary>
    /// 配置API服务命令
    /// </summary>
    [RelayCommand]
    private void ConfigureApiService()
    {
        _logger?.LogInformation("用户请求配置API服务");
        // TODO: 实现API服务配置逻辑
    }

    /// <summary>
    /// 查看监控服务详情命令
    /// </summary>
    [RelayCommand]
    private void ViewMonitoringServiceDetails()
    {
        _logger?.LogInformation("用户请求查看监控服务详情");
        // TODO: 实现监控服务详情查看逻辑
    }

    /// <summary>
    /// 配置监控服务命令
    /// </summary>
    [RelayCommand]
    private void ConfigureMonitoringService()
    {
        _logger?.LogInformation("用户请求配置监控服务");
        // TODO: 实现监控服务配置逻辑
    }

    /// <summary>
    /// 生成监控服务报告命令
    /// </summary>
    [RelayCommand]
    private void GenerateMonitoringReport()
    {
        _logger?.LogInformation("用户请求生成监控服务报告");
        // TODO: 实现监控服务报告生成逻辑
    }
} 