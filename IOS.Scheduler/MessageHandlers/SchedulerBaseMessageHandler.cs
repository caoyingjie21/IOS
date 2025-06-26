using IOS.Base.Messaging;
using IOS.Base.Mqtt;
using IOS.Base.Configuration;
using IOS.Base.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IOS.Scheduler.MessageHandlers;

/// <summary>
/// 调度器消息处理器基类 - 包含共同的发布功能
/// </summary>
public abstract class SchedulerBaseMessageHandler : BaseMessageHandler
{
    protected readonly IMqttService MqttService;
    protected readonly StandardMqttOptions MqttOptions;

    protected SchedulerBaseMessageHandler(
        IMqttService mqttService,
        IOptions<StandardMqttOptions> mqttOptions,
        ILogger logger) : base(logger)
    {
        MqttService = mqttService;
        MqttOptions = mqttOptions.Value;
    }

    /// <summary>
    /// 获取发布主题（使用TopicType枚举）
    /// </summary>
    protected string? GetPublishTopic(TopicType topic)
    {
        var key = topic.ToString();

        // 从配置中获取发布主题
        if (MqttOptions.Topics.Publish?.ContainsKey(key) == true)
        {
            return MqttOptions.Topics.Publish[key];
        }

        // 如果没有配置，尝试从Publications列表中匹配
        return MqttOptions.Topics.Publications?.FirstOrDefault(t => t.Contains(key));
    }

    /// <summary>
    /// 发布消息的便利方法
    /// </summary>
    protected async Task PublishMessageAsync<T>(string topic, T data, string messageType, CancellationToken cancellationToken = default)
    {
        var message = new StandardMessage<T>
        {
            MessageType = messageType,
            Sender = "IOS.Scheduler",
            Data = data
        };

        await MqttService.PublishAsync(topic, message, cancellationToken);
        Logger.LogDebug("已发布消息到主题: {Topic}, 消息类型: {MessageType}", topic, messageType);
    }
} 