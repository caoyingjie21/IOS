using IOS.Base.Messaging;
using Microsoft.Extensions.Logging;

namespace IOS.Coder.MessageHandlers;

/// <summary>
/// 读码器消息处理器工厂
/// </summary>
public class CoderMessageHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CoderMessageHandlerFactory> _logger;

    public CoderMessageHandlerFactory(IServiceProvider serviceProvider, ILogger<CoderMessageHandlerFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 根据主题创建消息处理器
    /// </summary>
    public IMessageHandler CreateHandler(string topic)
    {
        _logger.LogDebug("为主题创建消息处理器: {Topic}", topic);

        return topic switch
        {
            "ios/v1/coder/service/start" => GetService<CoderServiceHandler>(),
            "ios/v1/coder/config/set" => GetService<CoderConfigHandler>(),
            _ => GetService<DefaultCoderMessageHandler>()
        };
    }

    /// <summary>
    /// 获取服务实例
    /// </summary>
    private T GetService<T>() where T : class
    {
        var service = _serviceProvider.GetService<T>();
        if (service == null)
        {
            _logger.LogError("无法获取服务实例: {ServiceType}", typeof(T).Name);
            throw new InvalidOperationException($"服务 {typeof(T).Name} 未注册");
        }
        return service;
    }
} 