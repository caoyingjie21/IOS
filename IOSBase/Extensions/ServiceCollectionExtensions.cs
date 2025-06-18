using IOS.Base.Services;
using IOS.Base.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IOS.Base.Mqtt;

namespace IOS.Base.Extensions;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加IOSBase核心服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddIOSBase(this IServiceCollection services, IConfiguration configuration)
    {
        // 添加配置选项
        services.AddIOSBaseConfiguration(configuration);

        // 添加Mqtt服务
        services.AddSingleton<IMqttService, MqttService>();

        // 添加核心服务
        services.AddIOSBaseServices();

        return services;
    }

    /// <summary>
    /// 添加IOSBase服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddIOSBaseServices(this IServiceCollection services)
    {
        // 注册共享数据服务
        services.AddSingleton<SharedDataService>();

        return services;
    }

    /// <summary>
    /// 添加IOSBase消息处理服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    //public static IServiceCollection AddIOSBaseMessaging(this IServiceCollection services, IConfiguration configuration)
    //{
    //    // 添加MQTT配置
    //    services.AddMqttConfiguration(configuration);

    //    return services;
    //}
} 