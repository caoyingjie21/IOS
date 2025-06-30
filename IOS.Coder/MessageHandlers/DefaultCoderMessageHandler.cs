using IOS.Base.Configuration;
using IOS.Base.Mqtt;
using IOS.Base.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IOS.Coder.MessageHandlers;

/// <summary>
/// 默认读码器消息处理器
/// </summary>
public class DefaultCoderMessageHandler : CoderBaseMessageHandler
{
    public DefaultCoderMessageHandler(
        IMqttService mqttService,
        IOptions<StandardMqttOptions> mqttOptions,
        SharedDataService sharedDataService,
        ILogger<DefaultCoderMessageHandler> logger) : base(mqttService, mqttOptions, sharedDataService, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string topic, string message)
    {
        Logger.LogInformation("默认处理器处理未知消息 - 主题: {Topic}, 消息: {Message}", topic, message);
        await Task.CompletedTask;
    }

    protected override IEnumerable<string> GetSupportedTopics()
    {
        return Array.Empty<string>();
    }
} 