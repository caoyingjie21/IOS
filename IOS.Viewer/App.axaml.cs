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
using System.Runtime.InteropServices;

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
    /// 获取日志目录路径
    /// </summary>
    private string GetLogDirectory()
    {
        var basePath = AppContext.BaseDirectory;
        bool isAndroid = false;
        
        // 检测是否为Android平台
        try
        {
            // 优先使用OperatingSystem.IsAndroid()方法（.NET 6+）
            isAndroid = OperatingSystem.IsAndroid();
        }
        catch
        {
            // 如果不支持，使用RuntimeInformation检测
            var runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;
            isAndroid = runtimeIdentifier.Contains("android", StringComparison.OrdinalIgnoreCase);
        }
        
        string logsDirectory;
        if (isAndroid)
        {
            // Android平台：直接使用Documents目录
            logsDirectory = "/storage/emulated/0/Documents/IOSViewer_logs";
        }
        else
        {
            // 其他平台：使用应用程序基础目录
            logsDirectory = Path.Combine(basePath, "logs");
        }

        // 确保目录存在
        if (!Directory.Exists(logsDirectory))
        {
            Directory.CreateDirectory(logsDirectory);
        }
        
        // 测试目录写入权限
        try
        {
            var testFile = Path.Combine(logsDirectory, "test_write.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            Console.WriteLine($"日志目录写入权限验证成功: {logsDirectory}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"日志目录写入权限验证失败: {logsDirectory}, 错误: {ex.Message}");
        }

        return logsDirectory;
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

        // 获取日志目录并配置Serilog
        var logsDirectory = GetLogDirectory();
        var logFilePath = Path.Combine(logsDirectory, "ios-viewer-.txt");
        Console.WriteLine($"配置日志文件路径: {logFilePath}");
        
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(_configuration)
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();

        // 立即写入测试日志并刷新，确保文件创建
        Log.Information("=== IOS.Viewer 日志系统初始化完成 ===");
        Log.Information("日志文件路径: {LogFilePath}", logFilePath);
        Log.Information("日志目录: {LogsDirectory}", logsDirectory);
        Log.CloseAndFlush();
        
        // 重新初始化Logger
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(_configuration)
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
            )
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

                services.AddSingleton<MainViewModel>();
                
                // 注册导航服务
                services.AddSingleton<INavigationService, NavigationService>();

                services.AddHostedService<ViewerHostService>();
            });

        // 构建Host
        _host = hostBuilder.Build();
        _serviceProvider = _host.Services;

        // 获取日志器并记录启动信息
        var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        logger.LogInformation("IOS.Viewer 应用程序正在启动...");
        logger.LogInformation("配置文件基础路径: {BasePath}", basePath);
        logger.LogInformation("日志文件存储路径: {LogsDirectory}", logsDirectory);
        logger.LogInformation("检测到的平台: {Platform}", OperatingSystem.IsAndroid() ? "Android" : "其他平台");
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
