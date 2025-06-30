using IOS.Base.Messaging;
using IOS.Base.Mqtt;
using IOS.Base.Configuration;
using IOS.Base.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IOS.Motion.Configuration;

namespace IOS.Motion.MessageHandlers;

/// <summary>
/// 电机状态消息处理器
/// </summary>
public class MotionStatusHandler : MotionBaseMessageHandler
{
    private readonly MotionControlOptions _motionOptions;

    public MotionStatusHandler(
        IMqttService mqttService,
        IOptions<StandardMqttOptions> mqttOptions,
        SharedDataService sharedDataService,
        IOptions<MotionControlOptions> motionOptions,
        ILogger<MotionStatusHandler> logger) : base(mqttService, mqttOptions, sharedDataService, logger)
    {
        _motionOptions = motionOptions.Value;
    }

    protected override async Task ProcessMessageAsync(string topic, string message)
    {
        Logger.LogInformation("处理电机状态查询消息: {Message}", message);
        
        try
        {
            // 获取当前电机状态
            //var status = GetCurrentMotionStatus();

            // 发布状态响应
            var responseTopic = GetPublishTopicByKey("MotionStatus");
            if (!string.IsNullOrEmpty(responseTopic))
            {
                //await PublishMessageAsync(responseTopic, status, "motion_status");
                Logger.LogDebug("已发布电机状态响应到主题: {Topic}", responseTopic);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理电机状态查询失败");
        }
    }

    /// <summary>
    /// 获取当前电机状态
    /// </summary>
    //private MotionStatus GetCurrentMotionStatus()
    //{
    //    var currentPosition = GetSharedData<double>("CurrentPosition");
    //    var lastMoveTime = GetSharedData<DateTime?>("LastMoveTime");
    //    var lastMotionCommand = GetSharedData<MotionCommandData>("LastMotionCommand");
        
    //    return new MotionStatus
    //    {
    //        CurrentPosition = currentPosition,
    //        IsMoving = IsMotorMoving(),
    //        LastMoveTime = lastMoveTime,
    //        LastCommand = lastMotionCommand,
    //        Configuration = new MotionConfiguration
    //        {
    //            EtherNet = _motionOptions.EtherNet,
    //            MaxThreshold = _motionOptions.MaxThreshold,
    //            MinThreshold = _motionOptions.MinThreshold,
    //            PulseRatio = _motionOptions.PaulseRatio
    //        },
    //        Timestamp = DateTime.UtcNow
    //    };
    //}

    /// <summary>
    /// 判断电机是否在运动中
    /// </summary>
    private bool IsMotorMoving()
    {
        var lastMoveTime = GetSharedData<DateTime?>("LastMoveTime");
        if (lastMoveTime == null) return false;

        // 如果最后运动时间在3秒内，认为还在运动中
        return DateTime.UtcNow - lastMoveTime.Value < TimeSpan.FromSeconds(3);
    }

    protected override IEnumerable<string> GetSupportedTopics()
    {
        return new[] { "ios/v1/motion/status/request" };
    }
}

/// <summary>
/// 电机状态
/// </summary>
public class MotionStatus
{
    public double CurrentPosition { get; set; }
    public bool IsMoving { get; set; }
    public DateTime? LastMoveTime { get; set; }
    public MotionCommandData? LastCommand { get; set; }
    public MotionConfiguration? Configuration { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// 电机配置
/// </summary>
public class MotionConfiguration
{
    public string EtherNet { get; set; } = string.Empty;
    public int MaxThreshold { get; set; }
    public int MinThreshold { get; set; }
    public int PulseRatio { get; set; }
} 