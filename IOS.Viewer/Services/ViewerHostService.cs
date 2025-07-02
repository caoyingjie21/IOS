using IOS.Base.Configuration;
using IOS.Base.Mqtt;
using IOS.Base.Services;
using IOS.Base.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace IOS.Viewer.Services;

/// <summary>
/// Viewer应用程序主机服务，管理MQTT连接和消息处理
/// </summary>
public class ViewerHostService : BaseHostService
{
    public ViewerHostService(
        IMqttService mqttService,
        ILogger<ViewerHostService> logger,
        IOptions<StandardMqttOptions> mqttOptions)
        : base(mqttService, logger, mqttOptions)
    {
    }

    protected override async Task OnServiceStartingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Viewer应用程序服务正在初始化...");
        
        await base.OnServiceStartingAsync(cancellationToken);
        
    }

    protected override async Task OnServiceStoppingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Viewer应用程序服务正在清理资源...");
        
        await base.OnServiceStoppingAsync(cancellationToken);
    }

    protected override async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        // Viewer应用主要是监听MQTT消息，不需要持续的后台工作
        // 如果需要定期心跳或状态报告，可以在这里实现
        _logger.LogInformation("Viewer服务后台任务开始运行");
        await Task.CompletedTask;
    }

    protected override async Task HandleMqttMessageAsync(string topic, string message)
    {
        _logger.LogDebug("Viewer收到MQTT消息 - 主题: {Topic}", topic);
        
        try
        {
            // 这里可以添加消息处理逻辑
            // 例如：更新UI状态、处理命令等
            
            // 示例：处理不同类型的消息
            if (topic.Contains("status"))
            {
                await HandleStatusMessage(topic, message);
            }
            else if (topic.Contains("command"))
            {
                await HandleCommandMessage(topic, message);
            }
            else
            {
                _logger.LogDebug("未处理的消息主题: {Topic}", topic);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理MQTT消息失败 - 主题: {Topic}", topic);
        }
    }

    protected override async Task OnMqttConnectedAsync()
    {
        _logger.LogInformation("Viewer MQTT连接已建立，发布在线状态");
        await PublishViewerStatusAsync("在线");
    }

    protected override async Task OnMqttDisconnectedAsync()
    {
        _logger.LogWarning("Viewer MQTT连接已断开");
        // 在连接断开时可以进行一些清理工作
        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理状态消息
    /// </summary>
    private async Task HandleStatusMessage(string topic, string message)
    {
        _logger.LogDebug("处理状态消息: {Topic} - {Message}", topic, message);
        // 在这里添加状态消息处理逻辑
        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理命令消息
    /// </summary>
    private async Task HandleCommandMessage(string topic, string message)
    {
        _logger.LogDebug("处理命令消息: {Topic} - {Message}", topic, message);
        // 在这里添加命令消息处理逻辑
        await Task.CompletedTask;
    }

    /// <summary>
    /// 发布Viewer应用状态
    /// </summary>
    private async Task PublishViewerStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        var statusData = new
        {
            Service = "IOS.Viewer",
            Status = status,
            Timestamp = DateTime.UtcNow,
            Version = _mqttOptions.Messages?.Version ?? "1.0.0",
            Platform = Environment.OSVersion.Platform.ToString()
        };

        // 尝试从配置中获取状态发布主题
        var topic = GetStatusPublishTopic();
        if (!string.IsNullOrEmpty(topic))
        {
            await PublishMessageAsync(topic, statusData, "viewer_status", cancellationToken);
            _logger.LogInformation("已发布Viewer状态: {Status}", status);
        }
        else
        {
            _logger.LogWarning("未配置状态发布主题，无法发布Viewer状态");
        }
    }

    /// <summary>
    /// 获取状态发布主题
    /// </summary>
    private string? GetStatusPublishTopic()
    {
        // 优先查找Viewer专用的状态主题
        if (_mqttOptions.Topics?.Publish?.TryGetValue("ViewerStatus", out var topic) == true)
        {
            return topic;
        }
        
        // 回退到通用状态主题
        if (_mqttOptions.Topics?.Publish?.TryGetValue("Status", out topic) == true)
        {
            return topic;
        }

        return null;
    }
} 