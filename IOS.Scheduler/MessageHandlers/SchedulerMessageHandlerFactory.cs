using IOS.Base.Messaging;
using IOS.Base.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace IOS.Scheduler.MessageHandlers;

/// <summary>
/// 调度器消息处理器工厂 - 基于反射和命名约定的动态映射
/// </summary>
public class SchedulerMessageHandlerFactory : MessageHandlerFactory
{
    private readonly StandardMqttOptions _mqttOptions;
    private readonly object _initLock = new object();
    private bool _isInitialized = false;

    public SchedulerMessageHandlerFactory(
        IServiceProvider serviceProvider, 
        ILogger<SchedulerMessageHandlerFactory> logger,
        IOptions<StandardMqttOptions> mqttOptions) 
        : base(serviceProvider, logger)
    {
        _mqttOptions = mqttOptions.Value;
    }

    protected override Dictionary<string, Type> InitializeHandlerMappings()
    {
        // 在构造函数调用时，返回空字典，延迟到实际使用时初始化
        return new Dictionary<string, Type>();
    }

    /// <summary>
    /// 延迟初始化处理器映射
    /// </summary>
    private void EnsureInitialized()
    {
        if (_isInitialized) return;

        lock (_initLock)
        {
            if (_isInitialized) return;

            var mappings = BuildHandlerMappings();
            
            // 清空并重新填充映射
            HandlerMappings.Clear();
            foreach (var mapping in mappings)
            {
                HandlerMappings[mapping.Key] = mapping.Value;
            }

            _isInitialized = true;
        }
    }

    /// <summary>
    /// 构建处理器映射
    /// </summary>
    private Dictionary<string, Type> BuildHandlerMappings()
    {
        var mappings = new Dictionary<string, Type>();
        
        // 自动发现所有处理器类
        var handlerTypes = GetAllHandlerTypes();
        Logger.LogInformation("发现 {Count} 个消息处理器类", handlerTypes.Count);
        
        // 从配置的Subscribe字典中获取主题映射
        if (_mqttOptions?.Topics?.Subscribe != null)
        {
            foreach (var topicPair in _mqttOptions.Topics.Subscribe)
            {
                var topicKey = topicPair.Key;      // 例如: "Sensor", "Vision", "Motion" 等
                var topicValue = topicPair.Value;  // 例如: "ios/v1/sensor/grating/trigger"
                
                var handlerType = FindHandlerByTopicKey(topicKey, handlerTypes);
                if (handlerType != null)
                {
                    mappings[topicValue] = handlerType;
                    Logger.LogInformation("字典映射: {TopicKey}({TopicValue}) -> {HandlerType}", 
                        topicKey, topicValue, handlerType.Name);
                }
                else
                {
                    Logger.LogWarning("未找到主题键 {TopicKey}({TopicValue}) 对应的处理器，将使用默认处理器", 
                        topicKey, topicValue);
                }
            }
        }

        Logger.LogInformation("完成消息处理器映射，共 {Count} 个映射", mappings.Count);
        return mappings;
    }

    /// <summary>
    /// 创建消息处理器（重写以支持延迟初始化）
    /// </summary>
    public override IMessageHandler CreateHandler(string topic)
    {
        EnsureInitialized();
        return base.CreateHandler(topic);
    }

    /// <summary>
    /// 获取所有消息处理器类型
    /// </summary>
    private List<Type> GetAllHandlerTypes()
    {
        var handlerTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseMessageHandler)) && 
                       !t.IsAbstract && 
                       t != typeof(DefaultMessageHandler))
            .ToList();

        foreach (var type in handlerTypes)
        {
            Logger.LogDebug("发现处理器类: {HandlerType}", type.Name);
        }

        return handlerTypes;
    }

    /// <summary>
    /// 根据主题键查找对应的处理器 - 基于配置字典的键
    /// </summary>
    private Type? FindHandlerByTopicKey(string topicKey, List<Type> handlerTypes)
    {
        Logger.LogDebug("为主题键 {TopicKey} 查找处理器", topicKey);
        
        // 直接根据主题键匹配处理器名称
        foreach (var handlerType in handlerTypes)
        {
            if (IsHandlerMatchTopicKey(handlerType.Name, topicKey))
            {
                Logger.LogDebug("找到匹配的处理器: {TopicKey} -> {HandlerType}", topicKey, handlerType.Name);
                return handlerType;
            }
        }
        
        Logger.LogDebug("未找到主题键 {TopicKey} 的匹配处理器", topicKey);
        return null;
    }

    /// <summary>
    /// 检查处理器名称是否匹配主题键
    /// </summary>
    private bool IsHandlerMatchTopicKey(string handlerName, string topicKey)
    {
        // 移除Handler后缀
        var baseName = handlerName.Replace("Handler", "");
        
        Logger.LogTrace("检查处理器 {HandlerName} (基名: {BaseName}) 是否匹配主题键 {TopicKey}", 
            handlerName, baseName, topicKey);
        
        // 直接匹配或包含关系
        bool isMatch = baseName.Contains(topicKey, StringComparison.OrdinalIgnoreCase) ||
                       topicKey.Contains(baseName, StringComparison.OrdinalIgnoreCase);
        
        // 特殊映射规则
        if (!isMatch)
        {
            isMatch = topicKey switch
            {
                "Sensor" => baseName.Contains("Grating", StringComparison.OrdinalIgnoreCase),
                "Vision" => baseName.Contains("Camera", StringComparison.OrdinalIgnoreCase),
                "VisionHeight" => baseName.Contains("Height", StringComparison.OrdinalIgnoreCase),
                "Motion" => baseName.Contains("Motion", StringComparison.OrdinalIgnoreCase),
                "Coder" => baseName.Contains("Coder", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
        
        Logger.LogTrace("处理器 {HandlerName} 与主题键 {TopicKey} 匹配结果: {IsMatch}", 
            handlerName, topicKey, isMatch);
        
        return isMatch;
    }

    /// <summary>
    /// 根据主题查找对应的处理器 - 基于命名约定（保留用于兼容性）
    /// </summary>
    private Type? FindHandlerForTopic(string topic, List<Type> handlerTypes)
    {
        Logger.LogDebug("为主题 {Topic} 查找处理器", topic);
        
        // 从主题路径提取关键词
        var topicParts = topic.Split('/');
        var keywords = ExtractKeywords(topicParts);
        
        Logger.LogDebug("从主题 {Topic} 提取关键词: {Keywords}", topic, string.Join(", ", keywords));
        
        // 查找最匹配的处理器
        foreach (var handlerType in handlerTypes)
        {
            if (IsHandlerMatchTopic(handlerType.Name, keywords))
            {
                Logger.LogDebug("找到匹配的处理器: {Topic} -> {HandlerType}", topic, handlerType.Name);
                return handlerType;
            }
        }
        
        Logger.LogDebug("未找到主题 {Topic} 的匹配处理器", topic);
        return null;
    }

    /// <summary>
    /// 从主题路径提取关键词
    /// </summary>
    private List<string> ExtractKeywords(string[] topicParts)
    {
        var keywords = new List<string>();
        
        foreach (var part in topicParts)
        {
            // 跳过版本号和通用前缀
            if (part == "ios" || part.StartsWith("v") || string.IsNullOrEmpty(part))
                continue;
                
            // 将关键词转为Pascal命名格式
            var keyword = ToPascalCase(part);
            keywords.Add(keyword);
        }
        
        return keywords;
    }

    /// <summary>
    /// 检查处理器名称是否匹配主题关键词
    /// </summary>
    private bool IsHandlerMatchTopic(string handlerName, List<string> keywords)
    {
        // 移除Handler后缀
        var baseName = handlerName.Replace("Handler", "");
        
        Logger.LogTrace("检查处理器 {HandlerName} (基名: {BaseName}) 是否匹配关键词 {Keywords}", 
            handlerName, baseName, string.Join(", ", keywords));
        
        // 检查是否包含主要关键词的组合
        var matchCount = 0;
        foreach (var keyword in keywords)
        {
            if (baseName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                matchCount++;
                Logger.LogTrace("关键词 {Keyword} 在处理器 {HandlerName} 中匹配", keyword, handlerName);
            }
        }
        
        // 至少匹配一半的关键词才算匹配成功
        var isMatch = matchCount >= Math.Max(1, keywords.Count / 2);
        Logger.LogTrace("处理器 {HandlerName} 匹配度: {MatchCount}/{TotalCount}, 是否匹配: {IsMatch}", 
            handlerName, matchCount, keywords.Count, isMatch);
        
        return isMatch;
    }

    /// <summary>
    /// 转换为Pascal命名格式
    /// </summary>
    private string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        
        return char.ToUpper(input[0]) + input.Substring(1).ToLower();
    }

    protected override Type GetDefaultHandlerType()
    {
        return typeof(DefaultMessageHandler);
    }
} 