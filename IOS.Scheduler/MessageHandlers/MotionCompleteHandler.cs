using IOS.Base.Messaging;
using IOS.Base.Mqtt;
using IOS.Base.Configuration;
using IOS.Base.Services;
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
        SharedDataService sharedDataService,
        ILogger<MotionCompleteHandler> logger) : base(mqttService, mqttOptions, sharedDataService, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string topic, string message)
    {
        Logger.LogInformation("处理运动完成消息: {Message}", message);
        
        // 保存运动完成时间
        SaveSharedData("LastMotionCompleteTime", DateTime.UtcNow);
        SaveSharedData("LastMotionCompleteMessage", message);
        
        // 计算运动执行时间
        if (TryGetSharedData<DateTime>("LastMotionRequestTime", out var requestTime))
        {
            var executionTime = DateTime.UtcNow - requestTime;
            Logger.LogInformation("运动执行时间: {ExecutionTime}ms", executionTime.TotalMilliseconds);
            SaveSharedData("LastMotionExecutionTime", executionTime);
        }
        
        // 运动完成后触发编码器服务
        var coderTopic = GetPublishTopic(TopicType.Coder);
        if (!string.IsNullOrEmpty(coderTopic))
        {
            var coderData = new { 
                Command = "start_coding",
                RequestTime = DateTime.UtcNow,
                Source = "motion_complete"
            };
            
            await MqttService.PublishStandardMessageAsync(coderTopic, coderData, MessageType.Start);
            Logger.LogDebug("已发送编码器启动消息到主题: {Topic}", coderTopic);
            
            // 保存编码器请求时间
            SaveSharedData("LastCoderRequestTime", DateTime.UtcNow);
        }
    }

    protected override IEnumerable<string> GetSupportedTopics()
    {
        return new[] { "ios/v1/motion/control/complete" };
    }
} 