using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace IOS.Base.Messaging;

/// <summary>
/// 消息处理器工厂抽象基类
/// </summary>
public abstract class MessageHandlerFactory
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly ILogger Logger;
    protected readonly Dictionary<string, Type> HandlerMappings;

    protected MessageHandlerFactory(IServiceProvider serviceProvider, ILogger logger)
    {
        ServiceProvider = serviceProvider;
        Logger = logger;
        HandlerMappings = InitializeHandlerMappings();
    }

    /// <summary>
    /// 初始化处理器映射 - 由子类实现
    /// </summary>
    protected abstract Dictionary<string, Type> InitializeHandlerMappings();

    /// <summary>
    /// 获取默认处理器类型 - 由子类实现
    /// </summary>
    protected abstract Type GetDefaultHandlerType();

    /// <summary>
    /// 创建消息处理器
    /// </summary>
    /// <param name="topic">MQTT主题</param>
    /// <returns>消息处理器实例</returns>
    public virtual IMessageHandler CreateHandler(string topic)
    {
        try
        {
            // 精确匹配
            if (HandlerMappings.TryGetValue(topic, out var exactHandlerType))
            {
                return CreateHandlerInstance(exactHandlerType, topic);
            }

            // 模式匹配
            foreach (var mapping in HandlerMappings)
            {
                if (IsTopicMatch(topic, mapping.Key))
                {
                    return CreateHandlerInstance(mapping.Value, topic);
                }
            }

            // 如果没有找到特定处理器，返回默认处理器
            Logger.LogWarning("没有找到主题 {Topic} 的处理器，使用默认处理器", topic);
            return CreateHandlerInstance(GetDefaultHandlerType(), topic);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "创建消息处理器失败，主题: {Topic}", topic);
            throw;
        }
    }

    /// <summary>
    /// 创建处理器实例
    /// </summary>
    protected virtual IMessageHandler CreateHandlerInstance(Type handlerType, string topic)
    {
        var handler = ServiceProvider.GetRequiredService(handlerType) as IMessageHandler;
        if (handler == null)
        {
            throw new InvalidOperationException($"无法创建处理器实例: {handlerType.Name}，主题: {topic}");
        }
        return handler;
    }

    /// <summary>
    /// 检查主题是否匹配模式（支持MQTT通配符）
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