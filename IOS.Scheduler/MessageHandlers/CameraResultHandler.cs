using IOS.Base.Messaging;
using IOS.Base.Mqtt;
using IOS.Base.Configuration;
using IOS.Base.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IOS.Base.Enums;

namespace IOS.Scheduler.MessageHandlers;

/// <summary>
/// 相机结果消息处理器
/// </summary>
public class CameraResultHandler : SchedulerBaseMessageHandler
{
    public CameraResultHandler(
        IMqttService mqttService,
        IOptions<StandardMqttOptions> mqttOptions,
        SharedDataService sharedDataService,
        ILogger<CameraResultHandler> logger) : base(mqttService, mqttOptions, sharedDataService, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string topic, string message)
    {
        Logger.LogInformation("处理相机结果消息: {Message}", message);
        
        // 可以解析相机结果并决定下一步动作
        var cameraResult = DeserializeMessage<CameraResultData>(message);
        if (cameraResult != null)
        {
            // 保存检测结果到共享数据
            SaveSharedData("LastDetectionResult", cameraResult.IsValid ? "Valid" : "Invalid");
            SaveSharedData("LastDetectionTime", DateTime.UtcNow);
            SaveSharedData("LastCameraResult", cameraResult);
            
            // 获取光栅触发时间进行关联
            if (TryGetSharedData<DateTime>("LastGratingTriggerTime", out var triggerTime))
            {
                var processingTime = DateTime.UtcNow - triggerTime;
                Logger.LogInformation("检测处理时间: {ProcessingTime}ms", processingTime.TotalMilliseconds);
                SaveSharedData("LastProcessingTime", processingTime);
            }
            
            // 根据检测结果决定是否触发运动控制
            if (cameraResult.IsValid)
            {
                await TriggerMotionControlAsync();
            }
            else
            {
                Logger.LogWarning("相机检测结果无效，跳过运动控制");
                SaveSharedData("LastErrorMessage", cameraResult.ErrorMessage ?? "检测结果无效");
            }
        }
    }

    protected override IEnumerable<string> GetSupportedTopics()
    {
        return new[] { "ios/v1/vision/camera/result" };
    }

    private async Task TriggerMotionControlAsync()
    {
        var motionTopic = GetPublishTopic(TopicType.Motion);
        if (!string.IsNullOrEmpty(motionTopic))
        {
            var motionData = new { 
                Command = "move_to_position",
                RequestTime = DateTime.UtcNow,
                Source = "camera_detection"
            };
            
            await MqttService.PublishStandardMessageAsync(motionTopic, motionData, MessageType.Data);
            Logger.LogDebug("已发送运动控制消息到主题: {Topic}", motionTopic);
            
            // 保存运动控制请求时间
            SaveSharedData("LastMotionRequestTime", DateTime.UtcNow);
        }
    }
}

/// <summary>
/// 相机结果数据模型
/// </summary>
public class CameraResultData
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? DetectionResults { get; set; }
} 