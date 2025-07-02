using IOS.Base.Configuration;
using IOS.Base.Enums;
using IOS.Base.Mqtt;
using IOS.Base.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IOS.Scheduler.MessageHandlers
{
    /// <summary>
    /// 高度结果消息处理器
    /// </summary>
    public class HeightResultHandler : SchedulerBaseMessageHandler
    {
        public HeightResultHandler(
            IMqttService mqttService,
            IOptions<StandardMqttOptions> mqttOptions,
            SharedDataService sharedDataService,
            ILogger<HeightResultHandler> logger) : base(mqttService, mqttOptions, sharedDataService, logger)
        {
        }

        protected override async Task ProcessMessageAsync(string topic, string message)
        {
            Logger.LogInformation("收到高度信息: {Message}", message);
            
            // 保存高度检测结果
            SaveSharedData("LastHeightResult", message);
            SaveSharedData("LastHeightDetectionTime", DateTime.UtcNow);
            
            // 获取光栅触发时间进行关联
            if (TryGetSharedData<DateTime>("LastGratingTriggerTime", out var triggerTime))
            {
                var processingTime = DateTime.UtcNow - triggerTime;
                Logger.LogInformation("高度检测处理时间: {ProcessingTime}ms", processingTime.TotalMilliseconds);
                SaveSharedData("LastHeightProcessingTime", processingTime);
            }
            

            var motionTopic = GetPublishTopic(TopicType.Motion);
            if (!string.IsNullOrEmpty(motionTopic))
            {
                var motionData = new { 
                    Command = "move_to_height",
                    Paulse = message,
                    RequestTime = DateTime.UtcNow,
                    Source = "height_detection"
                };
                await PublishMessageAsync(motionTopic, motionData, "motion_control");
                Logger.LogDebug("已发送电机消息到主题: {Topic}", motionTopic);
                
                // 保存运动控制请求时间
                SaveSharedData("LastMotionRequestTime", DateTime.UtcNow);
            }
        }

        protected override IEnumerable<string> GetSupportedTopics()
        {
            return new[] { "ios/v1/vision/height/result" };
        }
    }
}
