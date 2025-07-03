using IOS.Base.Messaging;
using IOS.Base.Mqtt;
using IOS.Base.Configuration;
using IOS.Base.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IOS.Motion.Configuration;

namespace IOS.Motion.MessageHandlers;

/// <summary>
/// 电机校准消息处理器
/// </summary>
public class MotionCalibrationHandler : MotionBaseMessageHandler
{
    private readonly MotionControlOptions _motionOptions;

    public MotionCalibrationHandler(
        IMqttService mqttService,
        IOptions<StandardMqttOptions> mqttOptions,
        SharedDataService sharedDataService,
        IOptions<MotionControlOptions> motionOptions,
        ILogger<MotionCalibrationHandler> logger) : base(mqttService, mqttOptions, sharedDataService, logger)
    {
        _motionOptions = motionOptions.Value;
    }

    protected override async Task ProcessMessageAsync(string topic, string message)
    {
        Logger.LogInformation("处理电机校准消息: {Message}", message);
        
        try
        {
            // 保存校准开始时间
            SaveSharedData("CalibrationStartTime", DateTime.UtcNow);

            // 执行校准
            var result = await ExecuteCalibrationAsync();

            // 发布校准完成消息
            var completeTopic = GetPublishTopicByKey("MotionCalibration");
            if (!string.IsNullOrEmpty(completeTopic))
            {
                //var completeData = new
                //{
                //    Status = result.Success ? "Success" : "Failed",
                //    Message = result.Message,
                //    HomePosition = result.HomePosition,
                //    CalibrationTime = result.CalibrationTime,
                //    EtherNet = _motionOptions.EtherNet,
                //    PulseRatio = _motionOptions.PaulseRatio,
                //    Timestamp = DateTime.UtcNow
                //};

                //await PublishMessageAsync(completeTopic, completeData, "calibration_complete");
                Logger.LogInformation("电机校准完成，发布完成消息到主题: {Topic}", completeTopic);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理电机校准消息失败");
            
            // 发布错误消息
            var completeTopic = GetPublishTopicByKey("MotionCalibration");
            if (!string.IsNullOrEmpty(completeTopic))
            {
                var errorData = new
                {
                    Status = "Error",
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                };

                var errorMessage = new StandardMessage<object>
                {
                    MessageType = "calibration_error",
                    Sender = "IOS.Motion",
                    Data = errorData
                };

                await MqttService.PublishAsync(completeTopic, errorMessage);
            }
        }
    }

    /// <summary>
    /// 执行电机校准
    /// </summary>
    private async Task<CalibrationResult> ExecuteCalibrationAsync()
    {
        Logger.LogInformation("开始执行电机校准...");
        Logger.LogDebug("电机配置: {MotionConfig}", _motionOptions.GetSummary());

        var startTime = DateTime.UtcNow;

        try
        {
            // 使用注入的配置选项
            var homePosition = _motionOptions.MaxPosition; // 使用最小阈值作为原点
            var pulseRatio = _motionOptions.PulseRatio;

            Logger.LogInformation("校准参数: 原点位置={HomePosition}, 脉冲比例={PulseRatio}, 以太网类型={EtherNet}", 
                homePosition, pulseRatio, _motionOptions.EtherNet);

            // 模拟校准过程
            Logger.LogDebug("正在寻找原点...");
            await Task.Delay(2000); // 模拟寻找原点的时间

            Logger.LogDebug("设置原点位置...");
            await Task.Delay(500); // 模拟设置原点的时间

            // 更新当前位置为原点
            SaveSharedData("CurrentPosition", (double)homePosition);
            SaveSharedData("IsCalibrated", true);
            SaveSharedData("CalibrationTime", DateTime.UtcNow);

            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            Logger.LogInformation("电机校准完成，耗时: {Time}ms", executionTime);

            return new CalibrationResult
            {
                Success = true,
                Message = "校准成功",
                HomePosition = homePosition,
                CalibrationTime = executionTime
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "电机校准执行失败");
            return new CalibrationResult
            {
                Success = false,
                Message = ex.Message,
                HomePosition = 0,
                CalibrationTime = (DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    protected override IEnumerable<string> GetSupportedTopics()
    {
        return new[] { "ios/v1/motion/calibration/start" };
    }
}

/// <summary>
/// 校准结果
/// </summary>
public class CalibrationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public double HomePosition { get; set; }
    public double CalibrationTime { get; set; }
} 