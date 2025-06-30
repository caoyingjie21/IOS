using IOS.Base.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace IOS.Motion.MessageHandlers;

/// <summary>
/// 电机消息处理器工厂
/// </summary>
public class MotionMessageHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MotionMessageHandlerFactory> _logger;
    private readonly Dictionary<string, Type> _handlerMappings;

    public MotionMessageHandlerFactory(IServiceProvider serviceProvider, ILogger<MotionMessageHandlerFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _handlerMappings = InitializeHandlerMappings();
    }

    /// <summary>
    /// 根据主题创建消息处理器
    /// </summary>
    public IMessageHandler CreateHandler(string topic)
    {
        _logger.LogDebug("为主题创建处理器: {Topic}", topic);

        // 查找匹配的处理器
        var handlerType = FindHandlerType(topic);
        
        if (handlerType != null)
        {
            var handler = _serviceProvider.GetRequiredService(handlerType) as IMessageHandler;
            if (handler != null)
            {
                _logger.LogDebug("创建处理器成功: {HandlerType} for topic: {Topic}", handlerType.Name, topic);
                return handler;
            }
        }

        // 如果没有找到匹配的处理器，返回默认处理器
        _logger.LogWarning("未找到主题的处理器，使用默认处理器: {Topic}", topic);
        return _serviceProvider.GetRequiredService<DefaultMotionMessageHandler>();
    }

    /// <summary>
    /// 初始化处理器映射
    /// </summary>
    private Dictionary<string, Type> InitializeHandlerMappings()
    {
        return new Dictionary<string, Type>
        {
            // 电机控制相关
            { "ios/v1/motion/control/move", typeof(MotionControlHandler) },
            
            // 电机状态相关
            { "ios/v1/motion/status/request", typeof(MotionStatusHandler) },
            
            // 电机校准相关
            { "ios/v1/motion/calibration/start", typeof(MotionCalibrationHandler) }
        };
    }

    /// <summary>
    /// 查找处理器类型
    /// </summary>
    private Type? FindHandlerType(string topic)
    {
        // 精确匹配
        if (_handlerMappings.TryGetValue(topic, out var exactType))
        {
            return exactType;
        }

        // 模式匹配 - 检查topic是否包含关键词
        foreach (var mapping in _handlerMappings)
        {
            if (IsTopicMatch(topic, mapping.Key))
            {
                return mapping.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// 检查主题是否匹配
    /// </summary>
    private bool IsTopicMatch(string actualTopic, string patternTopic)
    {
        // 支持通配符匹配
        if (patternTopic.Contains("*"))
        {
            var pattern = patternTopic.Replace("*", ".*");
            return System.Text.RegularExpressions.Regex.IsMatch(actualTopic, pattern);
        }

        // 精确匹配
        return string.Equals(actualTopic, patternTopic, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 获取所有支持的主题
    /// </summary>
    public IEnumerable<string> GetSupportedTopics()
    {
        return _handlerMappings.Keys;
    }

    /// <summary>
    /// 注册新的处理器映射
    /// </summary>
    public void RegisterHandler<T>(string topic) where T : class, IMessageHandler
    {
        _handlerMappings[topic] = typeof(T);
        _logger.LogInformation("注册处理器: {Topic} -> {HandlerType}", topic, typeof(T).Name);
    }

    /// <summary>
    /// 移除处理器映射
    /// </summary>
    public bool RemoveHandler(string topic)
    {
        var removed = _handlerMappings.Remove(topic);
        if (removed)
        {
            _logger.LogInformation("移除处理器映射: {Topic}", topic);
        }
        return removed;
    }
} 