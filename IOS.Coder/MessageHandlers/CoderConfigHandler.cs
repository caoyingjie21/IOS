using IOS.Base.Configuration;
using IOS.Base.Mqtt;
using IOS.Base.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IOS.Coder.MessageHandlers;

/// <summary>
/// 读码器配置消息处理器
/// </summary>
public class CoderConfigHandler : CoderBaseMessageHandler
{
    public CoderConfigHandler(
        IMqttService mqttService,
        IOptions<StandardMqttOptions> mqttOptions,
        SharedDataService sharedDataService,
        ILogger<CoderConfigHandler> logger) : base(mqttService, mqttOptions, sharedDataService, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string topic, string message)
    {
        Logger.LogInformation("处理读码器配置消息: {Message}", message);
        
        try
        {
            // 解析配置指令
            var configCommand = JsonSerializer.Deserialize<CoderConfigCommand>(message);
            if (configCommand?.Data == null)
            {
                Logger.LogWarning("无效的读码器配置消息格式");
                return;
            }

            // 保存配置更新时间
            SaveSharedData("LastCoderConfigUpdateTime", DateTime.UtcNow);
            SaveSharedData("LastCoderConfig", configCommand.Data);

            // 应用配置（这里可以实现配置更新逻辑）
            var result = await ApplyConfigurationAsync(configCommand.Data);

            // 发布配置完成响应
            var responseData = new
            {
                CommandId = configCommand.Data.CommandId,
                Status = result.Success ? "Success" : "Failed",
                Message = result.Message,
                AppliedConfig = configCommand.Data.Configuration,
                Timestamp = DateTime.UtcNow
            };

            await PublishCoderStatusAsync(responseData, "coder_config_response");
            Logger.LogInformation("读码器配置处理完成");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理读码器配置消息失败");
            
            // 发布错误响应
            var errorData = new
            {
                Status = "Error",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            };

            await PublishCoderStatusAsync(errorData, "coder_config_error");
        }
    }

    /// <summary>
    /// 应用配置
    /// </summary>
    private async Task<ConfigResult> ApplyConfigurationAsync(CoderConfigData configData)
    {
        Logger.LogInformation("应用读码器配置");

        try
        {
            // 这里可以实现实际的配置应用逻辑
            // 例如：更新串口参数、读码器设置等
            
            await Task.Delay(100); // 模拟配置应用时间

            Logger.LogInformation("读码器配置应用成功");
            return new ConfigResult
            {
                Success = true,
                Message = "配置应用成功"
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "应用读码器配置失败");
            return new ConfigResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    protected override IEnumerable<string> GetSupportedTopics()
    {
        return new[] { "ios/v1/coder/config/set" };
    }
}

/// <summary>
/// 读码器配置指令
/// </summary>
public class CoderConfigCommand
{
    public string? MessageType { get; set; }
    public string? Sender { get; set; }
    public CoderConfigData? Data { get; set; }
}

/// <summary>
/// 读码器配置数据
/// </summary>
public class CoderConfigData
{
    public string CommandId { get; set; } = Guid.NewGuid().ToString();
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// 配置结果
/// </summary>
public class ConfigResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
} 