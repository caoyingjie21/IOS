using IOS.Base.Messaging;
using IOS.Base.Mqtt;
using IOS.Base.Configuration;
using IOS.Base.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IOS.Base.Enums;

namespace IOS.Scheduler.MessageHandlers;

/// <summary>
/// 光栅触发消息处理器
/// </summary>
public class GratingTriggerHandler : SchedulerBaseMessageHandler
{
    public GratingTriggerHandler(
        IMqttService mqttService,
        IOptions<StandardMqttOptions> mqttOptions,
        SharedDataService sharedDataService,
        ILogger<GratingTriggerHandler> logger) : base(mqttService, mqttOptions, sharedDataService, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string topic, string message)
    {
        Logger.LogInformation("处理光栅触发消息: {Message}", message);
        
        // 保存光栅触发时间到共享数据
        SaveSharedData("TriggerTime", DateTime.UtcNow);
        SaveSharedData("TriggerMessage", message);
        
        // 解析消息并触发相应的视觉检测
        var visionTopic = GetPublishTopic(TopicType.VisionHeight);
        if (!string.IsNullOrEmpty(visionTopic))
        {
            var visionData = new { 
                Command = "start_detection",
                TriggerTime = DateTime.UtcNow,
                TriggerSource = "grating_sensor",
                Direction = message
            };
            
            await MqttService.PublishStandardMessageAsync(visionTopic, visionData, MessageType.Start);
            Logger.LogDebug("已发送视觉检测启动消息到主题: {Topic}", visionTopic);
        }
    }

    protected override IEnumerable<string> GetSupportedTopics()
    {
        return new[] { "ios/v1/sensor/grating/trigger" };
    }
} 