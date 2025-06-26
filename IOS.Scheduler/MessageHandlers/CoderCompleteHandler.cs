using IOS.Base.Messaging;
using IOS.Base.Mqtt;
using IOS.Base.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IOS.Scheduler.MessageHandlers;

/// <summary>
/// 编码器完成消息处理器
/// </summary>
public class CoderCompleteHandler : SchedulerBaseMessageHandler
{
    public CoderCompleteHandler(
        IMqttService mqttService,
        IOptions<StandardMqttOptions> mqttOptions,
        ILogger<CoderCompleteHandler> logger) : base(mqttService, mqttOptions, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string topic, string message)
    {
        Logger.LogInformation("处理编码器完成消息: {Message}", message);
        
        // 编码完成，可能需要更新订单状态或触发其他操作
        var coderResult = DeserializeMessage<CoderResultData>(message);
        if (coderResult != null)
        {
            if (coderResult.IsSuccess)
            {
                Logger.LogInformation("编码操作成功完成，编码ID: {CodeId}", coderResult.CodeId);
                // 这里可以添加更新订单状态的逻辑
                await UpdateOrderStatusAsync(coderResult);
            }
            else
            {
                Logger.LogError("编码操作失败: {ErrorMessage}", coderResult.ErrorMessage);
                // 这里可以添加错误处理逻辑
            }
        }
    }

    protected override IEnumerable<string> GetSupportedTopics()
    {
        return new[] { "ios/v1/coder/service/complete" };
    }

    private async Task UpdateOrderStatusAsync(CoderResultData coderResult)
    {
        // 可以在这里添加更新订单状态的逻辑
        // 例如发送状态更新消息到订单系统
        Logger.LogDebug("更新订单状态，编码ID: {CodeId}", coderResult.CodeId);
        await Task.CompletedTask;
    }
}

/// <summary>
/// 编码器结果数据模型
/// </summary>
public class CoderResultData
{
    public bool IsSuccess { get; set; }
    public string? CodeId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CompletedAt { get; set; }
} 