using IOS.Base.Messaging;
using IOS.Base.Mqtt;
using IOS.Base.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IOS.Scheduler.MessageHandlers;

/// <summary>
/// 相机结果消息处理器
/// </summary>
public class CameraResultHandler : BaseMessageHandler
{
    private readonly IMqttService _mqttService;
    private readonly StandardMqttOptions _mqttOptions;

    public CameraResultHandler(
        IMqttService mqttService,
        IOptions<StandardMqttOptions> mqttOptions,
        ILogger<CameraResultHandler> logger) : base(logger)
    {
        _mqttService = mqttService;
        _mqttOptions = mqttOptions.Value;
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
        return new[] { "*/vision/camera/result", "+/vision/camera/result" };
    }

    private async Task TriggerMotionControlAsync()
    {
        var motionTopic = GetPublishTopic("motion/control/move");
        if (!string.IsNullOrEmpty(motionTopic))
        {
            var motionMessage = new StandardMessage<object>
            {
                MessageType = "motion_control",
                Sender = "IOS.Scheduler",
                Data = new { Command = "move_to_position" }
            };
            
            await _mqttService.PublishAsync(motionTopic, motionMessage);
            Logger.LogDebug("已发送运动控制消息到主题: {Topic}", motionTopic);
        }
    }

    private string? GetPublishTopic(string key)
    {
        // 从配置中获取发布主题
        if (_mqttOptions.Topics.Publish?.ContainsKey(key) == true)
        {
            return _mqttOptions.Topics.Publish[key];
        }
        
        // 如果没有配置，尝试从Publications列表中匹配
        return _mqttOptions.Topics.Publications?.FirstOrDefault(t => t.Contains(key));
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