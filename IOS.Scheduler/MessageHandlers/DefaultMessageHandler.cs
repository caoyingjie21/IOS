using IOS.Base.Messaging;
using Microsoft.Extensions.Logging;

namespace IOS.Scheduler.MessageHandlers;

/// <summary>
/// 默认消息处理器 - 处理未识别的消息
/// </summary>
public class DefaultMessageHandler : BaseMessageHandler
{
    public DefaultMessageHandler(ILogger<DefaultMessageHandler> logger) : base(logger)
    {
    }

    protected override async Task ProcessMessageAsync(string topic, string message)
    {
        Logger.LogWarning("收到未处理的消息 - 主题: {Topic}, 消息: {Message}", topic, message);
        await Task.CompletedTask;
    }

    protected override IEnumerable<string> GetSupportedTopics()
    {
        // 默认处理器支持所有主题
        return new[] { "#" };
    }
} 