using IOS.Base.Messaging;
using IOS.Base.Mqtt;
using IOS.Base.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IOS.Scheduler.MessageHandlers;

/// <summary>
/// 光栅触发消息处理器
/// </summary>
public class GratingTriggerHandler : BaseMessageHandler
{
    private readonly IMqttService _mqttService;
    private readonly StandardMqttOptions _mqttOptions;

    public GratingTriggerHandler(
        IMqttService mqttService,
        IOptions<StandardMqttOptions> mqttOptions,
        ILogger<GratingTriggerHandler> logger) : base(logger)
    {
        _mqttService = mqttService;
        _mqttOptions = mqttOptions.Value;
    }

    protected override async Task ProcessMessageAsync(string topic, string message)
    {
        Logger.LogInformation("处理光栅触发消息: {Message}", message);
        
        // 解析消息并触发相应的视觉检测
        var visionTopic = GetPublishTopic("vision/camera/start");
        if (!string.IsNullOrEmpty(visionTopic))
        {
            var visionMessage = new StandardMessage<object>
            {
                MessageType = "vision_start",
                Sender = "IOS.Scheduler", 
                Data = new { Command = "start_detection" }
            };
            
            await _mqttService.PublishAsync(visionTopic, visionMessage);
            Logger.LogDebug("已发送视觉检测启动消息到主题: {Topic}", visionTopic);
        }
    }

    protected override IEnumerable<string> GetSupportedTopics()
    {
        return new[] { "ios/v1/sensor/grating/trigger" };
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