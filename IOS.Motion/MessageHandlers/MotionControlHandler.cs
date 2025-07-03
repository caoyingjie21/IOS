using IOS.Base.Messaging;
using IOS.Base.Mqtt;
using IOS.Base.Configuration;
using IOS.Base.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using IOS.Motion.Configuration;

namespace IOS.Motion.MessageHandlers;

/// <summary>
/// 电机控制消息处理器
/// </summary>
public class MotionControlHandler : MotionBaseMessageHandler
{
    private readonly MotionControlOptions _motionOptions;

    public MotionControlHandler(
        IMqttService mqttService,
        IOptions<StandardMqttOptions> mqttOptions,
        SharedDataService sharedDataService,
        IOptions<MotionControlOptions> motionOptions,
        ILogger<MotionControlHandler> logger) : base(mqttService, mqttOptions, sharedDataService, logger)
    {
        _motionOptions = motionOptions.Value;
    }

    protected override async Task ProcessMessageAsync(string topic, string message)
    {
        Logger.LogInformation("处理电机控制消息: {Message}", message);
        
        try
        {
            // 解析运动控制指令
            var motionCommand = JsonSerializer.Deserialize<MotionCommand>(message);
            if (motionCommand?.Data == null)
            {
                Logger.LogWarning("无效的运动控制消息格式");
                return;
            }

            // 模拟电机控制逻辑
            //var result = await ExecuteMotionAsync(motionCommand.Data);

            // 发布运动完成消息
            var completeTopic = GetPublishTopicByKey("Motion");
            if (!string.IsNullOrEmpty(completeTopic))
            {
                //var completeData = new
                //{
                //    CommandId = motionCommand.Data.CommandId,
                //    Position = motionCommand.Data.Position,
                //    Speed = motionCommand.Data.Speed,
                //    ExecutionTime = result.ExecutionTime,
                //    Status = result.Success ? "Success" : "Failed",
                //    Message = result.Message,
                //    Timestamp = DateTime.UtcNow
                //};

                //await PublishMessageAsync(completeTopic, completeData, "motion_complete");
                Logger.LogInformation("电机运动完成，发布完成消息到主题: {Topic}", completeTopic);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理电机控制消息失败");
            
            // 发布错误消息
            var completeTopic = GetPublishTopicByKey("Motion");
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
                    MessageType = "motion_error",
                    Sender = "IOS.Motion",
                    Data = errorData
                };

                await MqttService.PublishAsync(completeTopic, errorMessage);
            }
        }
    }

    /// <summary>
    /// 执行电机运动
    /// </summary>
    //private async Task<MotionResult> ExecuteMotionAsync(MotionCommandData command)
    //{
    //    Logger.LogInformation("执行电机运动: 位置={Position}, 速度={Speed}", command.Position, command.Speed);
    //    Logger.LogDebug("电机配置: {MotionConfig}", _motionOptions.GetSummary());

    //    var startTime = DateTime.UtcNow;

    //    try
    //    {
    //        // 使用注入的配置选项
    //        var maxThreshold = _motionOptions.MaxThreshold;
    //        var minThreshold = _motionOptions.MinThreshold;
    //        var pulseRatio = _motionOptions.PaulseRatio;

    //        // 验证参数
    //        if (command.Position > maxThreshold || command.Position < minThreshold)
    //        {
    //            return new MotionResult
    //            {
    //                Success = false,
    //                Message = $"位置超出范围 [{minThreshold}, {maxThreshold}]",
    //                ExecutionTime = (DateTime.UtcNow - startTime).TotalMilliseconds
    //            };
    //        }

    //        // 模拟电机运动延时（根据距离和速度计算）
    //        var currentPosition = GetSharedData<double>("CurrentPosition");
    //        var distance = Math.Abs(command.Position - currentPosition);
    //        var moveTime = (distance / command.Speed) * 1000; // 转换为毫秒
            
    //        Logger.LogDebug("模拟电机运动，距离: {Distance}, 预计时间: {Time}ms, 脉冲比例: {PulseRatio}", 
    //            distance, moveTime, pulseRatio);
            
    //        await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(moveTime, 5000))); // 最大延时5秒

    //        // 更新当前位置
    //        SaveSharedData("CurrentPosition", command.Position);
    //        SaveSharedData("LastMoveTime", DateTime.UtcNow);

    //        return new MotionResult
    //        {
    //            Success = true,
    //            Message = "运动完成",
    //            ExecutionTime = (DateTime.UtcNow - startTime).TotalMilliseconds
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        Logger.LogError(ex, "电机运动执行失败");
    //        return new MotionResult
    //        {
    //            Success = false,
    //            Message = ex.Message,
    //            ExecutionTime = (DateTime.UtcNow - startTime).TotalMilliseconds
    //        };
    //    }
    //}

    protected override IEnumerable<string> GetSupportedTopics()
    {
        return new[] { "ios/v1/motion/control/move" };
    }
}

/// <summary>
/// 运动控制指令
/// </summary>
public class MotionCommand
{
    public string? MessageType { get; set; }
    public string? Sender { get; set; }
    public MotionCommandData? Data { get; set; }
}

/// <summary>
/// 运动控制指令数据
/// </summary>
public class MotionCommandData
{
    public string CommandId { get; set; } = Guid.NewGuid().ToString();
    public double Position { get; set; }
    public double Speed { get; set; }
    public double? Acceleration { get; set; }
    public string? MoveType { get; set; } = "Absolute"; // Absolute, Relative
}

/// <summary>
/// 运动结果
/// </summary>
public class MotionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public double ExecutionTime { get; set; }
} 