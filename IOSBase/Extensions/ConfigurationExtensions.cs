using IOS.Base.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    /// 添加模块配置选项（泛型方法）
    /// </summary>
    /// <typeparam name="T">配置选项类型，必须实现 IConfigurationOptions</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="customValidator">自定义验证器（可选）</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddModuleConfiguration<T>(
        this IServiceCollection services, 
        IConfiguration configuration,
        IValidateOptions<T>? customValidator = null) 
        where T : class, IConfigurationOptions
    {
        // 注册配置选项
        services.Configure<T>(configuration.GetSection(T.SectionName));

        // 注册配置验证器
        if (customValidator != null)
        {
            services.AddSingleton(customValidator);
        }
        else
        {
            services.AddSingleton<IValidateOptions<T>, BaseOptionsValidator<T>>();
        }

        return services;
    }

    /// <summary>
    /// 添加模块配置选项（带自定义验证器类型）
    /// </summary>
    /// <typeparam name="TOptions">配置选项类型</typeparam>
    /// <typeparam name="TValidator">验证器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddModuleConfiguration<TOptions, TValidator>(
        this IServiceCollection services, 
        IConfiguration configuration) 
        where TOptions : class, IConfigurationOptions
        where TValidator : class, IValidateOptions<TOptions>
    {
        // 注册配置选项
        services.Configure<TOptions>(configuration.GetSection(TOptions.SectionName));

        // 注册自定义验证器
        services.AddSingleton<IValidateOptions<TOptions>, TValidator>();

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