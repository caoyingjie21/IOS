using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IOS.Base.Messaging;

/// <summary>
/// 消息处理器基类
/// </summary>
public abstract class BaseMessageHandler : IMessageHandler
{
    protected readonly ILogger Logger;

    protected BaseMessageHandler(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// 处理消息的主入口
    /// </summary>
    public async Task HandleMessageAsync(string topic, string message)
    {
        try
        {
            Logger.LogDebug("开始处理消息 - 主题: {Topic}, 消息: {Message}", topic, message);
            
            if (!CanHandle(topic))
            {
                Logger.LogWarning("无法处理主题: {Topic}", topic);
                return;
            }

            await ProcessMessageAsync(topic, message);
            Logger.LogDebug("消息处理完成 - 主题: {Topic}", topic);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理消息失败 - 主题: {Topic}, 消息: {Message}", topic, message);
        }
    }

    /// <summary>
    /// 检查是否可以处理指定主题
    /// </summary>
    public virtual bool CanHandle(string topic)
    {
        var supportedTopics = GetSupportedTopics();
        return supportedTopics.Any(supportedTopic => IsTopicMatch(topic, supportedTopic));
    }

    /// <summary>
    /// 处理具体消息的抽象方法
    /// </summary>
    protected abstract Task ProcessMessageAsync(string topic, string message);

    /// <summary>
    /// 获取支持的主题列表
    /// </summary>
    protected abstract IEnumerable<string> GetSupportedTopics();

    /// <summary>
    /// 反序列化JSON消息
    /// </summary>
    protected T? DeserializeMessage<T>(string message) where T : class
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Logger.LogWarning("消息内容为空");
                return null;
            }

            return JsonSerializer.Deserialize<T>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "反序列化消息失败, 消息: {Message}", message);
            return null;
        }
    }

    /// <summary>
    /// 序列化对象为JSON
    /// </summary>
    protected string SerializeObject<T>(T obj)
    {
        try
        {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "序列化对象失败");
            throw;
        }
    }

    /// <summary>
    /// 检查主题是否匹配（支持通配符）
    /// </summary>
    protected static bool IsTopicMatch(string topic, string pattern)
    {
        if (pattern == topic) return true;
        if (!pattern.Contains('+') && !pattern.Contains('#')) return false;

        var topicParts = topic.Split('/');
        var patternParts = pattern.Split('/');

        return IsTopicMatchRecursive(topicParts, patternParts, 0, 0);
    }

    /// <summary>
    /// 递归匹配主题模式
    /// </summary>
    private static bool IsTopicMatchRecursive(string[] topicParts, string[] patternParts, int topicIndex, int patternIndex)
    {
        if (patternIndex >= patternParts.Length)
            return topicIndex >= topicParts.Length;

        if (topicIndex >= topicParts.Length)
            return patternParts[patternIndex] == "#";

        var patternPart = patternParts[patternIndex];

        if (patternPart == "#")
            return true;

        if (patternPart == "+")
            return IsTopicMatchRecursive(topicParts, patternParts, topicIndex + 1, patternIndex + 1);

        if (patternPart == topicParts[topicIndex])
            return IsTopicMatchRecursive(topicParts, patternParts, topicIndex + 1, patternIndex + 1);

        return false;
    }
} 