using IOS.Base.Messaging;
using IOS.Base.Mqtt;
using IOS.Base.Configuration;
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
        ILogger<CameraResultHandler> logger) : base(mqttService, mqttOptions, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string topic, string message)
    {
        Logger.LogInformation("处理相机结果消息: {Message}", message);
        
        // 可以解析相机结果并决定下一步动作
        var cameraResult = DeserializeMessage<CameraResultData>(message);
        if (cameraResult != null)
        {
            // 根据检测结果决定是否触发运动控制
            if (cameraResult.IsValid)
            {
                await TriggerMotionControlAsync();
            }
            else
            {
                Logger.LogWarning("相机检测结果无效，跳过运动控制");
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
            var motionData = new { Command = "move_to_position" };
            await PublishMessageAsync(motionTopic, motionData, "motion_control");
            Logger.LogDebug("已发送运动控制消息到主题: {Topic}", motionTopic);
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