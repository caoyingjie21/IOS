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
        
        // 为每个订阅主题寻找对应的处理器
        if (_mqttOptions?.Topics?.Subscriptions != null)
        {
            foreach (var topic in _mqttOptions.Topics.Subscriptions)
            {
                var handlerType = FindHandlerForTopic(topic, handlerTypes);
                if (handlerType != null)
                {
                    mappings[topic] = handlerType;
                    Logger.LogInformation("自动映射: {Topic} -> {HandlerType}", topic, handlerType.Name);
                }
                else
                {
                    Logger.LogWarning("未找到主题 {Topic} 对应的处理器，将使用默认处理器", topic);
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
    /// 根据主题查找对应的处理器 - 基于命名约定
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