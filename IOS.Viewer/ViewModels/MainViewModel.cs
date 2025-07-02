using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia;
using System.Runtime.InteropServices;
using Material.Icons;
using System.Collections.ObjectModel;
using System;
using IOS.Viewer.Services;
using Avalonia.Threading;

namespace IOS.Viewer.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    [ObservableProperty] private int _stepIndex = 1;
    [ObservableProperty] private double _progressValue = 52;
    [ObservableProperty] private bool _isTextVisible = true;
    [ObservableProperty] private bool _isIndeterminate;
    
    // 导航相关属性
    [ObservableProperty] private bool _isPanelSelected = true;
    [ObservableProperty] private bool _isSettingsSelected = false;
    
    // MQTT消息相关属性
    [ObservableProperty] private string _currentMqttMessage = "等待MQTT消息...";
    [ObservableProperty] private ObservableCollection<string> _mqttMessages = new();
    
    // MaterialIcon 属性
    public MaterialIconKind PanelIcon => MaterialIconKind.Home;
    public MaterialIconKind SettingsIcon => MaterialIconKind.Settings;
    
    public MainViewModel()
    {
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
    /// 切换到面板视图
    /// </summary>
    [RelayCommand]
    private void ShowPanel()
    {
        IsPanelSelected = true;
        IsSettingsSelected = false;
    }
    
    /// <summary>
    /// 切换到设置视图
    /// </summary>
    [RelayCommand]
    private void ShowSettings()
    {
        IsPanelSelected = false;
        IsSettingsSelected = true;
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
}
