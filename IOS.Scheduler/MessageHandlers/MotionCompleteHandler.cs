using IOS.Base.Messaging;
using IOS.Base.Mqtt;
using IOS.Base.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IOS.Scheduler.MessageHandlers;

/// <summary>
/// 运动完成消息处理器
/// </summary>
public class MotionCompleteHandler : BaseMessageHandler
{
    private readonly IMqttService _mqttService;
    private readonly StandardMqttOptions _mqttOptions;

    public MotionCompleteHandler(
        IMqttService mqttService,
        IOptions<StandardMqttOptions> mqttOptions,
        ILogger<MotionCompleteHandler> logger) : base(logger)
    {
        _mqttService = mqttService;
        _mqttOptions = mqttOptions.Value;
    }

    protected override async Task ProcessMessageAsync(string topic, string message)
    {
        Logger.LogInformation("处理运动完成消息: {Message}", message);
        
        // 运动完成后触发编码器服务
        var coderTopic = GetPublishTopic("coder/service/start");
        if (!string.IsNullOrEmpty(coderTopic))
        {
            var coderMessage = new StandardMessage<object>
            {
                MessageType = "coder_start",
                Sender = "IOS.Scheduler",
                Data = new { Command = "start_coding" }
            };
            
            await _mqttService.PublishAsync(coderTopic, coderMessage);
            Logger.LogDebug("已发送编码器启动消息到主题: {Topic}", coderTopic);
        }
    }

    protected override IEnumerable<string> GetSupportedTopics()
    {
        return new[] { "ios/v1/motion/control/complete" };
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