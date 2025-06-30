using IOS.Base.Messaging;
using IOS.Base.Mqtt;
using IOS.Base.Configuration;
using IOS.Base.Enums;
using IOS.Base.Services;
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
    protected readonly SharedDataService SharedDataService;

    protected SchedulerBaseMessageHandler(
        IMqttService mqttService,
        IOptions<StandardMqttOptions> mqttOptions,
        SharedDataService sharedDataService,
        ILogger logger) : base(logger)
    {
        MqttService = mqttService;
        MqttOptions = mqttOptions.Value;
        SharedDataService = sharedDataService;
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

        // 如果没有配置，尝试从Publish字典的值中匹配
        return MqttOptions.Topics.Publish?.Values.FirstOrDefault(t => t.Contains(key));
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

    /// <summary>
    /// 保存共享数据的便利方法
    /// </summary>
    protected void SaveSharedData<T>(string key, T value)
    {
        SharedDataService.SetData(key, value);
        Logger.LogDebug("保存共享数据: {Key}", key);
    }

    /// <summary>
    /// 获取共享数据的便利方法
    /// </summary>
    protected T? GetSharedData<T>(string key)
    {
        var data = SharedDataService.GetData<T>(key);
        Logger.LogDebug("获取共享数据: {Key}, 存在: {Exists}", key, data != null);
        return data;
    }

    /// <summary>
    /// 尝试获取共享数据的便利方法
    /// </summary>
    protected bool TryGetSharedData<T>(string key, out T? value)
    {
        var success = SharedDataService.TryGetData<T>(key, out value);
        Logger.LogDebug("尝试获取共享数据: {Key}, 成功: {Success}", key, success);
        return success;
    }
} 