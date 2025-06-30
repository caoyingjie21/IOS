using IOS.Base.Messaging;
using IOS.Base.Mqtt;
using IOS.Base.Configuration;
using IOS.Base.Services;
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
        SharedDataService sharedDataService,
        ILogger<CoderCompleteHandler> logger) : base(mqttService, mqttOptions, sharedDataService, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string topic, string message)
    {
        Logger.LogInformation("处理编码器完成消息: {Message}", message);
        
        // 编码完成，可能需要更新订单状态或触发其他操作
        var coderResult = DeserializeMessage<CoderResultData>(message);
        if (coderResult != null)
        {
            // 保存编码完成信息
            SaveSharedData("LastCoderCompleteTime", DateTime.UtcNow);
            SaveSharedData("LastCoderResult", coderResult);
            
            // 计算编码执行时间
            if (TryGetSharedData<DateTime>("LastCoderRequestTime", out var requestTime))
            {
                var executionTime = DateTime.UtcNow - requestTime;
                Logger.LogInformation("编码执行时间: {ExecutionTime}ms", executionTime.TotalMilliseconds);
                SaveSharedData("LastCoderExecutionTime", executionTime);
            }
            
            if (coderResult.IsSuccess)
            {
                Logger.LogInformation("编码操作成功完成，编码ID: {CodeId}", coderResult.CodeId);
                
                // 更新成功统计
                var successCount = GetSharedData<int>("CoderSuccessCount");
                SaveSharedData("CoderSuccessCount", successCount + 1);
                
                // 计算整个流程的总时间
                if (TryGetSharedData<DateTime>("LastGratingTriggerTime", out var triggerTime))
                {
                    var totalProcessTime = DateTime.UtcNow - triggerTime;
                    Logger.LogInformation("整个流程总时间: {TotalTime}ms", totalProcessTime.TotalMilliseconds);
                    SaveSharedData("LastTotalProcessTime", totalProcessTime);
                }
                
                await UpdateOrderStatusAsync(coderResult);
            }
            else
            {
                Logger.LogError("编码操作失败: {ErrorMessage}", coderResult.ErrorMessage);
                
                // 更新失败统计
                var failureCount = GetSharedData<int>("CoderFailureCount");
                SaveSharedData("CoderFailureCount", failureCount + 1);
                SaveSharedData("LastCoderError", coderResult.ErrorMessage);
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
        
        // 保存订单更新信息
        SaveSharedData("LastOrderUpdateTime", DateTime.UtcNow);
        SaveSharedData("LastOrderId", coderResult.CodeId);
        
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