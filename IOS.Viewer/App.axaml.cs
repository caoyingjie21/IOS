using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Controls;

using IOS.Viewer.ViewModels;
using IOS.Viewer.Views;
using IOS.Viewer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IOS.Base.Extensions;
using IOS.Base.Mqtt;
using IOS.Base.Configuration;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Threading.Tasks;

namespace IOS.Viewer;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
    private IConfiguration? _configuration;
    private IHost? _host;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // 明确设置主题变体为暗色模式
        RequestedThemeVariant = ThemeVariant.Dark;

        // 配置服务
        ConfigureServices();
    }

    /// <summary>
    /// 配置依赖注入服务
    /// </summary>
    private void ConfigureServices()
    {
        // 构建配置 - 使用AppContext.BaseDirectory确保在所有平台上都能找到配置文件
        var basePath = AppContext.BaseDirectory;
        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        _configuration = builder.Build();

        var logsDirectory = Path.Combine(basePath, "logs");
        if (!Directory.Exists(logsDirectory))
        {
            Directory.CreateDirectory(logsDirectory);
        }
        
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(_configuration)
            .CreateLogger();

        // 创建Host Builder
        var hostBuilder = Host.CreateDefaultBuilder()
            .UseSerilog(Log.Logger)
            .ConfigureServices((context, services) =>
            {
                // 注册配置
                services.AddSingleton<IConfiguration>(_configuration);

                // 注册日志服务
                services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddSerilog(Log.Logger);
                });

                services.AddIOSBase(_configuration);

                services.AddTransient<MainViewModel>();

                services.AddHostedService<ViewerHostService>();
            });

        // 构建Host
        _host = hostBuilder.Build();
        _serviceProvider = _host.Services;

        // 获取日志器并记录启动信息
        var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        logger.LogInformation("IOS.Viewer 应用程序正在启动...");
        logger.LogInformation("配置文件基础路径: {BasePath}", basePath);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        // 再次确保主题设置
        RequestedThemeVariant = ThemeVariant.Dark;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 桌面应用程序
            var mainViewModel = _serviceProvider?.GetRequiredService<MainViewModel>() ?? new MainViewModel();
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel,
                RequestedThemeVariant = ThemeVariant.Dark
            };

            // 注册应用程序退出事件
            desktop.ShutdownRequested += OnShutdownRequested;

            // 启动后台服务
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_host != null)
                    {
                        await _host.StartAsync();
                        var logger = _serviceProvider?.GetRequiredService<ILogger<App>>();
                        logger?.LogInformation("后台服务已启动");
                    }
                }
                catch (Exception ex)
                {
                    var logger = _serviceProvider?.GetRequiredService<ILogger<App>>();
                    logger?.LogError(ex, "启动后台服务时发生错误");
                }
            });
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // 移动应用程序
            var mainViewModel = _serviceProvider?.GetRequiredService<MainViewModel>() ?? new MainViewModel();
            
            var mainView = new MainView
            {
                DataContext = mainViewModel
            };
            
            // 移动平台上，主题由Application级别和原生配置控制
            // UserControl 没有 RequestedThemeVariant 属性
            singleViewPlatform.MainView = mainView;

            // 启动后台服务
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_host != null)
                    {
                        await _host.StartAsync();
                        var logger = _serviceProvider?.GetRequiredService<ILogger<App>>();
                        logger?.LogInformation("后台服务已启动（移动平台）");
                    }
                }
                catch (Exception ex)
                {
                    var logger = _serviceProvider?.GetRequiredService<ILogger<App>>();
                    logger?.LogError(ex, "启动后台服务时发生错误（移动平台）");
                }
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 处理应用程序退出事件
    /// </summary>
    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        try
        {
            var logger = _serviceProvider?.GetRequiredService<ILogger<App>>();
            logger?.LogInformation("应用程序正在退出...");

            // 停止后台服务
            if (_host != null)
            {
                _host.StopAsync(TimeSpan.FromSeconds(10)).Wait(TimeSpan.FromSeconds(15));
                _host.Dispose();
                logger?.LogInformation("后台服务已停止");
            }

            // 释放服务提供者
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
                logger?.LogInformation("服务提供者已释放");
            }

            // 关闭日志器
            Log.CloseAndFlush();
        }
        catch (Exception ex)
        {
            // 在应用程序退出时记录错误
            Log.Error(ex, "应用程序退出时发生错误");
        }
    }

    /// <summary>
    /// 获取服务实例
    /// </summary>
    public static T? GetService<T>() where T : class
    {
        if (Current is App app && app._serviceProvider != null)
        {
            return app._serviceProvider.GetService<T>();
        }
        return null;
    }
}
