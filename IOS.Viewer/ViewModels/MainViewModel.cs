using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia;
using System.Runtime.InteropServices;
using Material.Icons;
using System.Collections.ObjectModel;
using System;
using IOS.Viewer.Services;
using IOS.Viewer.Views;
using Avalonia.Threading;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Linq;

namespace IOS.Viewer.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    [ObservableProperty] private int _stepIndex = 1;
    [ObservableProperty] private double _progressValue = 52;
    [ObservableProperty] private bool _isTextVisible = true;
    [ObservableProperty] private bool _isIndeterminate;
    
    // MQTT消息相关属性
    [ObservableProperty] private string _currentMqttMessage = "等待MQTT消息...";
    [ObservableProperty] private ObservableCollection<string> _mqttMessages = new();
    
    // MaterialIcon 属性
    public MaterialIconKind SettingsIcon => MaterialIconKind.Settings;
    
    private readonly INavigationService? _navigationService;
    
    public MainViewModel()
    {
        // 获取导航服务
        _navigationService = App.GetService<INavigationService>();
        
        // 订阅MQTT消息事件
        ViewerHostService.MqttMessageReceived += OnMqttMessageReceived;
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        // 取消订阅MQTT消息事件
        ViewerHostService.MqttMessageReceived -= OnMqttMessageReceived;
    }
    
    /// <summary>
    /// 处理接收到的MQTT消息
    /// </summary>
    private void OnMqttMessageReceived(string topic, string message)
    {
        // 确保在UI线程上更新界面
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var displayMessage = $"[{topic}] {message}";
            UpdateMqttMessage(displayMessage);
        });
    }
    
    /// <summary>
    /// 更新MQTT消息
    /// </summary>
    /// <param name="message">接收到的MQTT消息</param>
    public void UpdateMqttMessage(string message)
    {
        CurrentMqttMessage = message;
        
        // 添加到消息历史，保持最新的50条消息
        MqttMessages.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
        if (MqttMessages.Count > 50)
        {
            MqttMessages.RemoveAt(MqttMessages.Count - 1);
        }
    }
    
    /// <summary>
    /// 清空MQTT消息历史
    /// </summary>
    [RelayCommand]
    private void ClearMqttMessages()
    {
        MqttMessages.Clear();
        CurrentMqttMessage = "消息已清空";
    }
    
    /// <summary>
    /// 导航到设置页面
    /// </summary>
    [RelayCommand]
    private void NavigateToSettings()
    {
        var logger = App.GetService<ILogger<MainViewModel>>();
        logger?.LogInformation("用户点击设置按钮");
        
        _navigationService?.NavigateTo<SettingsView>();
    }
    
    /// <summary>
    /// 检测是否为移动平台
    /// </summary>
    public bool IsMobile
    {
        get
        {
            try
            {
                // 检测Android平台
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID")))
                    return true;
                
                // 检测iOS平台  
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("IOS")))
                    return true;
                
                return false;
            }
            catch
            {
                // 如果无法检测，默认为桌面平台
                return false;
            }
        }
    }
    
    /// <summary>
    /// 检测是否为Android平台
    /// </summary>
    public bool IsAndroid
    {
        get
        {
            try
            {
                return OperatingSystem.IsAndroid();
            }
            catch
            {
                var runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;
                return runtimeIdentifier.Contains("android", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
    
    /// <summary>
    /// 获取日志目录路径
    /// </summary>
    private string GetLogDirectoryPath()
    {
        if (IsAndroid)
        {
            // Android平台：直接使用Documents目录
            return "/storage/emulated/0/Documents/IOSViewer_logs";
        }
        else
        {
            // 其他平台使用应用基础目录
            return Path.Combine(AppContext.BaseDirectory, "logs");
        }
    }
    
    /// <summary>
    /// 打开日志文件
    /// </summary>
    [RelayCommand]
    private void OpenLogFile()
    {
        try
        {
            var logPath = GetLogDirectoryPath();
            var logger = App.GetService<ILogger<MainViewModel>>();
            
            if (IsAndroid)
            {
                // Android平台：通过反射调用平台特定的方法打开日志目录
                try
                {
                    var mainActivityType = Type.GetType("IOS.Viewer.Android.MainActivity, IOS.Viewer.Android");
                    if (mainActivityType != null)
                    {
                        var openFileManagerMethod = mainActivityType.GetMethod("OpenFileManager", 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (openFileManagerMethod != null)
                        {
                            openFileManagerMethod.Invoke(null, new object[] { logPath });
                            logger?.LogInformation("已调用Android文件管理器打开日志目录: {LogPath}", logPath);
                        }
                        else
                        {
                            logger?.LogWarning("未找到OpenFileManager方法");
                        }
                    }
                    else
                    {
                        logger?.LogWarning("未找到MainActivity类型");
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "调用Android文件管理器失败");
                }
            }
            else
            {
                // 桌面平台：尝试打开最新的日志文件
                if (Directory.Exists(logPath))
                {
                    // 查找最新的日志文件
                    var logFiles = Directory.GetFiles(logPath, "ios-viewer-*.txt")
                        .OrderByDescending(f => File.GetLastWriteTime(f))
                        .ToArray();
                    
                    if (logFiles.Length > 0)
                    {
                        var latestLogFile = logFiles[0];
                        try
                        {
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                Process.Start(new ProcessStartInfo(latestLogFile) { UseShellExecute = true });
                            }
                            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                            {
                                Process.Start("open", latestLogFile);
                            }
                            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                            {
                                Process.Start("xdg-open", latestLogFile);
                            }
                            logger?.LogInformation("已打开日志文件: {LogFile}", latestLogFile);
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "无法直接打开日志文件，尝试打开目录: {LogFile}", latestLogFile);
                            // 如果无法打开文件，回退到打开目录
                            OpenLogDirectory();
                        }
                    }
                    else
                    {
                        logger?.LogWarning("未找到日志文件，打开日志目录");
                        OpenLogDirectory();
                    }
                }
                else
                {
                    logger?.LogWarning("日志目录不存在: {LogPath}", logPath);
                }
            }
        }
        catch (Exception ex)
        {
            var logger = App.GetService<ILogger<MainViewModel>>();
            logger?.LogError(ex, "打开日志文件时发生错误");
        }
    }

    /// <summary>
    /// 打开日志目录
    /// </summary>
    [RelayCommand]
    private void OpenLogDirectory()
    {
        try
        {
            var logPath = GetLogDirectoryPath();
            var logger = App.GetService<ILogger<MainViewModel>>();
            logger?.LogInformation("尝试打开日志目录: {LogPath}", logPath);
            
            if (IsAndroid)
            {
                // Android平台：通过反射调用平台特定的方法
                try
                {
                    var mainActivityType = Type.GetType("IOS.Viewer.Android.MainActivity, IOS.Viewer.Android");
                    if (mainActivityType != null)
                    {
                        var openFileManagerMethod = mainActivityType.GetMethod("OpenFileManager", 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (openFileManagerMethod != null)
                        {
                            openFileManagerMethod.Invoke(null, new object[] { logPath });
                            logger?.LogInformation("已调用Android文件管理器打开: {LogPath}", logPath);
                        }
                        else
                        {
                            logger?.LogWarning("未找到OpenFileManager方法");
                        }
                    }
                    else
                    {
                        logger?.LogWarning("未找到MainActivity类型");
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "调用Android文件管理器失败");
                }
            }
            else
            {
                // 桌面平台：使用Process.Start打开文件夹
                if (Directory.Exists(logPath))
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Process.Start("explorer.exe", logPath);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        Process.Start("open", logPath);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Process.Start("xdg-open", logPath);
                    }
                }
                else
                {
                    logger?.LogWarning("日志目录不存在: {LogPath}", logPath);
                }
            }
        }
        catch (Exception ex)
        {
            var logger = App.GetService<ILogger<MainViewModel>>();
            logger?.LogError(ex, "打开日志目录时发生错误");
        }
    }
}

