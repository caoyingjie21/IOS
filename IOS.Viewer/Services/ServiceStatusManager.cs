using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace IOS.Viewer.Services;

/// <summary>
/// 服务状态管理器，负责管理所有服务的状态和消息历史
/// </summary>
public partial class ServiceStatusManager : ObservableObject
{
    private readonly ILogger<ServiceStatusManager> _logger;
    private const int MaxMessageCount = 100;

    // CoderStatus 服务属性
    [ObservableProperty] private string _coderStatusText = "未连接";
    [ObservableProperty] private string _coderStatusColor = "#E74C3C";
    [ObservableProperty] private ObservableCollection<string> _coderStatusMessages = new();

    // DataServerStatus 服务属性
    [ObservableProperty] private string _dataServerStatusText = "未连接";
    [ObservableProperty] private string _dataServerStatusColor = "#E74C3C";
    [ObservableProperty] private ObservableCollection<string> _dataServerStatusMessages = new();

    // SchedulerStatus 服务属性
    [ObservableProperty] private string _schedulerStatusText = "未连接";
    [ObservableProperty] private string _schedulerStatusColor = "#E74C3C";
    [ObservableProperty] private ObservableCollection<string> _schedulerStatusMessages = new();

    // MotionStatus 服务属性
    [ObservableProperty] private string _motionStatusText = "未连接";
    [ObservableProperty] private string _motionStatusColor = "#E74C3C";
    [ObservableProperty] private ObservableCollection<string> _motionStatusMessages = new();

    // Vision 服务属性
    [ObservableProperty] private string _visionStatusText = "未连接";
    [ObservableProperty] private string _visionStatusColor = "#E74C3C";
    [ObservableProperty] private ObservableCollection<string> _visionStatusMessages = new();

    public ServiceStatusManager(ILogger<ServiceStatusManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 处理MQTT消息
    /// </summary>
    public void HandleMqttMessage(string topic, string message)
    {
        try
        {
            // 格式化消息，包含topic和美化的JSON
            var formattedMessage = FormatMessage(topic, message);
            
            // 根据topic路由到对应的服务
            if (topic.StartsWith("ios/v1/coder/", StringComparison.OrdinalIgnoreCase))
            {
                UpdateCoderStatus(formattedMessage);
            }
            else if (topic.StartsWith("ios/v1/data/", StringComparison.OrdinalIgnoreCase))
            {
                UpdateDataServerStatus(formattedMessage);
            }
            else if (topic.StartsWith("ios/v1/scheduler/", StringComparison.OrdinalIgnoreCase))
            {
                UpdateSchedulerStatus(formattedMessage);
            }
            else if (topic.StartsWith("ios/v1/motion/", StringComparison.OrdinalIgnoreCase))
            {
                UpdateMotionStatus(formattedMessage);
            }
            else if (topic.StartsWith("ios/v1/vision/", StringComparison.OrdinalIgnoreCase))
            {
                UpdateVisionStatus(formattedMessage);
            }
            
            _logger?.LogDebug("已处理MQTT消息: Topic={Topic}, Message={Message}", topic, message);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "处理MQTT消息时发生错误: Topic={Topic}", topic);
        }
    }

    /// <summary>
    /// 格式化消息，包含topic和美化的JSON
    /// </summary>
    private string FormatMessage(string topic, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var formattedMessage = $"[{timestamp}] Topic: {topic}\n";
        
        // 尝试解析和美化JSON
        try
        {
            // 检查消息是否为JSON格式
            if (IsValidJson(message))
            {
                var jsonDocument = JsonDocument.Parse(message);
                var formattedJson = JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                formattedMessage += $"Message:\n{formattedJson}";
            }
            else
            {
                formattedMessage += $"Message: {message}";
            }
        }
        catch (JsonException)
        {
            // 如果不是有效的JSON，直接显示原始消息
            formattedMessage += $"Message: {message}";
        }
        
        return formattedMessage;
    }

    /// <summary>
    /// 检查字符串是否为有效的JSON
    /// </summary>
    private bool IsValidJson(string jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
            return false;
            
        jsonString = jsonString.Trim();
        return (jsonString.StartsWith("{") && jsonString.EndsWith("}")) ||
               (jsonString.StartsWith("[") && jsonString.EndsWith("]"));
    }

    /// <summary>
    /// 添加消息到集合并保持最多100条记录
    /// </summary>
    private void AddMessageToCollection(ObservableCollection<string> collection, string message)
    {
        // 在开头添加新消息（最新的在顶部）
        collection.Insert(0, message);
        
        // 如果超过最大数量，移除最旧的消息
        while (collection.Count > MaxMessageCount)
        {
            collection.RemoveAt(collection.Count - 1);
        }
    }

    /// <summary>
    /// 更新CoderStatus服务状态
    /// </summary>
    private void UpdateCoderStatus(string message)
    {
        CoderStatusText = "运行中";
        CoderStatusColor = "#27AE60";
        AddMessageToCollection(CoderStatusMessages, message);
    }

    /// <summary>
    /// 更新DataServerStatus服务状态
    /// </summary>
    private void UpdateDataServerStatus(string message)
    {
        DataServerStatusText = "运行中";
        DataServerStatusColor = "#27AE60";
        AddMessageToCollection(DataServerStatusMessages, message);
    }

    /// <summary>
    /// 更新SchedulerStatus服务状态
    /// </summary>
    private void UpdateSchedulerStatus(string message)
    {
        SchedulerStatusText = "运行中";
        SchedulerStatusColor = "#27AE60";
        AddMessageToCollection(SchedulerStatusMessages, message);
    }

    /// <summary>
    /// 更新MotionStatus服务状态
    /// </summary>
    private void UpdateMotionStatus(string message)
    {
        MotionStatusText = "运行中";
        MotionStatusColor = "#27AE60";
        AddMessageToCollection(MotionStatusMessages, message);
    }

    /// <summary>
    /// 更新Vision服务状态
    /// </summary>
    private void UpdateVisionStatus(string message)
    {
        VisionStatusText = "运行中";
        VisionStatusColor = "#27AE60";
        AddMessageToCollection(VisionStatusMessages, message);
    }
} 