using IOS.Base.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
namespace IOS.Base.Extensions;

/// <summary>
/// 配置扩展方法
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// 添加IOSBase配置选项
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddIOSBaseConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册MQTT配置
        services.Configure<MqttOptions>(configuration.GetSection(MqttOptions.SectionName));
        services.Configure<StandardMqttOptions>(configuration.GetSection(StandardMqttOptions.SectionName));

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });
        return services;
    }

    /// <summary>
    /// 添加MQTT配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    //public static IServiceCollection AddMqttConfiguration(this IServiceCollection services, IConfiguration configuration)
    //{
    //    services.Configure<MqttOptions>(configuration.GetSection(MqttOptions.SectionName));
    //    services.Configure<StandardMqttOptions>(configuration.GetSection(StandardMqttOptions.SectionName));
    //    return services;
    //}
} 