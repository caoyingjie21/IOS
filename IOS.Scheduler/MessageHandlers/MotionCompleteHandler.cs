using IOS.Base.Messaging;
using IOS.Base.Mqtt;
using IOS.Base.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IOS.Base.Enums;

namespace IOS.Scheduler.MessageHandlers;

/// <summary>
/// 运动完成消息处理器
/// </summary>
public class MotionCompleteHandler : SchedulerBaseMessageHandler
{
    public MotionCompleteHandler(
        IMqttService mqttService,
        IOptions<StandardMqttOptions> mqttOptions,
        ILogger<MotionCompleteHandler> logger) : base(mqttService, mqttOptions, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string topic, string message)
    {
        Logger.LogInformation("处理运动完成消息: {Message}", message);
        
        // 运动完成后触发编码器服务
        var coderTopic = GetPublishTopic(TopicType.Coder);
        if (!string.IsNullOrEmpty(coderTopic))
        {
            var coderData = new { Command = "start_coding" };
            await PublishMessageAsync(coderTopic, coderData, "coder_start");
            Logger.LogDebug("已发送编码器启动消息到主题: {Topic}", coderTopic);
        }
    }

    protected override IEnumerable<string> GetSupportedTopics()
    {
        return new[] { "ios/v1/motion/control/complete" };
    }
} 