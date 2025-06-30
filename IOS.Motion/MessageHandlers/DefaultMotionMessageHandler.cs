using IOS.Base.Messaging;
using IOS.Base.Mqtt;
using IOS.Base.Configuration;
using IOS.Base.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IOS.Motion.MessageHandlers;

/// <summary>
/// 默认电机消息处理器
/// </summary>
public class DefaultMotionMessageHandler : MotionBaseMessageHandler
{
    public DefaultMotionMessageHandler(
        IMqttService mqttService,
        IOptions<StandardMqttOptions> mqttOptions,
        SharedDataService sharedDataService,
        ILogger<DefaultMotionMessageHandler> logger) : base(mqttService, mqttOptions, sharedDataService, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string topic, string message)
    {
        Logger.LogWarning("收到未处理的电机消息 - 主题: {Topic}, 消息: {Message}", topic, message);
        await Task.CompletedTask;
    }

    protected override IEnumerable<string> GetSupportedTopics()
    {
        return Array.Empty<string>();
    }
} 