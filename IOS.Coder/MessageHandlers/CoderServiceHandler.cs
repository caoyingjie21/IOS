using IOS.Base.Configuration;
using IOS.Base.Mqtt;
using IOS.Base.Services;
using IOS.Coder.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IOS.Coder.MessageHandlers;

/// <summary>
/// 读码器服务启动消息处理器
/// </summary>
public class CoderServiceHandler : CoderBaseMessageHandler
{
    private readonly CoderService _coderService;

    public CoderServiceHandler(
        IMqttService mqttService,
        IOptions<StandardMqttOptions> mqttOptions,
        SharedDataService sharedDataService,
        CoderService coderService,
        ILogger<CoderServiceHandler> logger) : base(mqttService, mqttOptions, sharedDataService, logger)
    {
        _coderService = coderService;
    }

    protected override async Task ProcessMessageAsync(string topic, string message)
    {
        Logger.LogInformation("处理读码器启动消息: {Message}", message);
        
        try
        {
            // 解析启动指令
            var serviceCommand = JsonSerializer.Deserialize<CoderServiceCommand>(message);
            if (serviceCommand?.Data == null)
            {
                Logger.LogWarning("无效的读码器服务消息格式");
                return;
            }

            // 保存服务请求时间
            SaveSharedData("LastCoderServiceRequestTime", DateTime.UtcNow);
            SaveSharedData("LastCoderServiceCommand", serviceCommand.Data);

            // 执行读码器服务
            var result = await ExecuteCoderServiceAsync(serviceCommand.Data.Action);

            // 发布服务完成消息
            var completeData = new
            {
                CommandId = serviceCommand.Data.CommandId,
                Action = serviceCommand.Data.Action,
                Status = result.Success ? "Success" : "Failed",
                Message = result.Message,
                ExecutionTime = result.ExecutionTime,
                Timestamp = DateTime.UtcNow
            };

            await PublishCoderCompleteAsync(completeData, "coder_service_complete");
            Logger.LogInformation("读码器服务完成，发布完成消息");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理读码器服务消息失败");
            
            // 发布错误消息
            var errorData = new
            {
                Status = "Error",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            };

            await PublishCoderCompleteAsync(errorData, "coder_service_error");
        }
    }

    /// <summary>
    /// 执行读码器服务操作
    /// </summary>
    private async Task<CoderServiceResult> ExecuteCoderServiceAsync(string action)
    {
        try
        {
            switch (action.ToLower())
            {
                case "start":
                    var startResult = await _coderService.StartAsync();
                    Logger.LogInformation("读码器服务启动结果: {Result}", startResult);
                    return new CoderServiceResult { Success = startResult, Message = startResult ? "读码器服务启动成功" : "读码器服务启动失败" };

                case "stop":
                    await _coderService.StopAsync();
                    Logger.LogInformation("读码器服务停止成功");
                    return new CoderServiceResult { Success = true, Message = "读码器服务停止成功" };

                case "connect":
                    var connectResult = await _coderService.StartAsync();
                    Logger.LogInformation("读码器连接结果: {Result}", connectResult);
                    return new CoderServiceResult { Success = connectResult, Message = connectResult ? "连接成功" : "连接失败" };

                case "disconnect":
                    await DisconnectCoderAsync();
                    Logger.LogInformation("读码器断开连接成功");
                    return new CoderServiceResult { Success = true, Message = "断开连接成功" };

                default:
                    Logger.LogWarning("未知的读码器服务操作: {Action}", action);
                    return new CoderServiceResult { Success = false, Message = $"未知操作: {action}" };
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "执行读码器服务操作失败: {Action}", action);
            return new CoderServiceResult { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// 断开读码器连接
    /// </summary>
    private async Task DisconnectCoderAsync()
    {
        await _coderService.StopAsync();
    }

    protected override IEnumerable<string> GetSupportedTopics()
    {
        return new[] { "ios/v1/coder/service/start" };
    }
}

/// <summary>
/// 读码器服务指令
/// </summary>
public class CoderServiceCommand
{
    public string? MessageType { get; set; }
    public string? Sender { get; set; }
    public CoderServiceCommandData? Data { get; set; }
}

/// <summary>
/// 读码器服务指令数据
/// </summary>
public class CoderServiceCommandData
{
    public string CommandId { get; set; } = Guid.NewGuid().ToString();
    public string Action { get; set; } = "start"; // start, stop, connect, disconnect
    public Dictionary<string, object>? Parameters { get; set; }
}

/// <summary>
/// 读码器服务结果
/// </summary>
public class CoderServiceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public double ExecutionTime { get; set; }
} 