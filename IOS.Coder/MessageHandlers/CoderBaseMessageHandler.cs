using IOS.Base.Configuration;
using IOS.Base.Messaging;
using IOS.Base.Mqtt;
using IOS.Base.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IOS.Coder.MessageHandlers;

/// <summary>
/// 读码器消息处理器基类
/// </summary>
public abstract class CoderBaseMessageHandler : BaseMessageHandler
{
    protected readonly IMqttService MqttService;
    protected readonly IOptions<StandardMqttOptions> MqttOptions;
    protected readonly SharedDataService SharedDataService;

    protected CoderBaseMessageHandler(
        IMqttService mqttService,
        IOptions<StandardMqttOptions> mqttOptions,
        SharedDataService sharedDataService,
        ILogger logger) : base(logger)
    {
        MqttService = mqttService;
        MqttOptions = mqttOptions;
        SharedDataService = sharedDataService;
    }

    /// <summary>
    /// 根据键获取发布主题
    /// </summary>
    protected string? GetPublishTopicByKey(string key)
    {
        return MqttOptions.Value.Topics.Publish.TryGetValue(key, out var topic) ? topic : null;
    }

    /// <summary>
    /// 根据键获取订阅主题
    /// </summary>
    protected string? GetSubscribeTopicByKey(string key)
    {
        return MqttOptions.Value.Topics.Subscribe.TryGetValue(key, out var topic) ? topic : null;
    }

    /// <summary>
    /// 发布读码器结果数据
    /// </summary>
    protected async Task PublishCoderResultAsync(object data, string? topicKey = "coder_data")
    {
        var topic = GetPublishTopicByKey(topicKey ?? "coder_data");
        if (!string.IsNullOrEmpty(topic))
        {
            var message = new StandardMessage<object>
            {
                MessageType = "CoderResult",
                Sender = "IOS.Coder",
                Data = data,
                Timestamp = DateTime.UtcNow
            };
            await MqttService.PublishAsync(topic, message);
        }
    }

    /// <summary>
    /// 发布读码器完成消息
    /// </summary>
    protected async Task PublishCoderCompleteAsync(object data, string? topicKey = "coder_complete")
    {
        var topic = GetPublishTopicByKey(topicKey ?? "coder_complete");
        if (!string.IsNullOrEmpty(topic))
        {
            var message = new StandardMessage<object>
            {
                MessageType = "CoderComplete",
                Sender = "IOS.Coder",
                Data = data,
                Timestamp = DateTime.UtcNow
            };
            await MqttService.PublishAsync(topic, message);
        }
    }

    /// <summary>
    /// 发布读码器状态
    /// </summary>
    protected async Task PublishCoderStatusAsync(object data, string? topicKey = "coder_status")
    {
        var topic = GetPublishTopicByKey(topicKey ?? "coder_status");
        if (!string.IsNullOrEmpty(topic))
        {
            var message = new StandardMessage<object>
            {
                MessageType = "CoderStatus",
                Sender = "IOS.Coder",
                Data = data,
                Timestamp = DateTime.UtcNow
            };
            await MqttService.PublishAsync(topic, message);
        }
    }

    /// <summary>
    /// 保存共享数据
    /// </summary>
    protected void SaveSharedData<T>(string key, T value)
    {
        SharedDataService.SetData(key, value);
    }

    /// <summary>
    /// 获取共享数据
    /// </summary>
    protected T? GetSharedData<T>(string key)
    {
        return SharedDataService.GetData<T>(key);
    }
} 