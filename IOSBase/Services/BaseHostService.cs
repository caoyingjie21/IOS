using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOS.Base.Configuration;
using IOS.Base.Mqtt;
using IOS.Base.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IOS.Base.Enums;

namespace IOS.Base.Services
{
    /// <summary>
    /// 基础主机服务，提供MQTT通信和基础功能
    /// </summary>
    public abstract class BaseHostService : BackgroundService
    {
        protected readonly IMqttService _mqttService;
        protected readonly ILogger _logger;
        protected readonly StandardMqttOptions _mqttOptions;

        public BaseHostService(IMqttService mqttService, ILogger logger, IOptions<StandardMqttOptions> mqttOptions) 
        {
            _mqttService = mqttService;
            _logger = logger;
            _mqttOptions = mqttOptions.Value;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("正在启动 {ServiceName} 服务...", GetType().Name);
                
                // 设置MQTT事件处理
                _mqttService.OnMessageReceived += OnMqttMessageReceived;
                _mqttService.OnConnectionChanged += OnMqttConnectionChanged;
                
                // 启动MQTT服务
                await _mqttService.StartAsync(cancellationToken);
                
                // 订阅主题
                await SubscribeToTopicsAsync(cancellationToken);
                
                // 调用派生类的初始化方法
                await OnServiceStartingAsync(cancellationToken);
                
                await base.StartAsync(cancellationToken);
                
                _logger.LogInformation("{ServiceName} 服务启动成功", GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ServiceName} 服务启动失败", GetType().Name);
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("正在停止 {ServiceName} 服务...", GetType().Name);
                
                // 调用派生类的清理方法
                await OnServiceStoppingAsync(cancellationToken);
                
                // 取消订阅所有主题
                await UnsubscribeFromTopicsAsync(cancellationToken);
                
                // 停止MQTT服务
                if (_mqttService != null)
                {
                    _mqttService.OnMessageReceived -= OnMqttMessageReceived;
                    _mqttService.OnConnectionChanged -= OnMqttConnectionChanged;
                    await _mqttService.StopAsync(cancellationToken);
                }
                
                await base.StopAsync(cancellationToken);
                
                _logger.LogInformation("{ServiceName} 服务停止成功", GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ServiceName} 服务停止失败", GetType().Name);
                throw;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{ServiceName} 开始执行主循环", GetType().Name);
            
            try
            {
                await DoWorkAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("{ServiceName} 服务被取消", GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ServiceName} 执行过程中发生异常", GetType().Name);
                throw;
            }
        }

        /// <summary>
        /// 执行具体工作的抽象方法，由派生类实现
        /// </summary>
        protected abstract Task DoWorkAsync(CancellationToken stoppingToken);

        /// <summary>
        /// 服务启动时的初始化方法，派生类可重写
        /// </summary>
        protected virtual Task OnServiceStartingAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 服务停止时的清理方法，派生类可重写
        /// </summary>
        protected virtual Task OnServiceStoppingAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// MQTT消息接收处理
        /// </summary>
        protected virtual async Task OnMqttMessageReceived(string topic, string message)
        {
            try
            {
                _logger.LogDebug("收到MQTT消息 - 主题: {Topic}, 消息: {Message}", topic, message);
                await HandleMqttMessageAsync(topic, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理MQTT消息失败 - 主题: {Topic}", topic);
            }
        }

        /// <summary>
        /// MQTT连接状态变化处理
        /// </summary>
        protected virtual async Task OnMqttConnectionChanged(bool isConnected)
        {
            if (isConnected)
            {
                _logger.LogInformation("MQTT连接已建立");
                await OnMqttConnectedAsync();
            }
            else
            {
                _logger.LogWarning("MQTT连接已断开");
                await OnMqttDisconnectedAsync();
            }
        }

        /// <summary>
        /// 处理MQTT消息，派生类可重写
        /// </summary>
        protected virtual Task HandleMqttMessageAsync(string topic, string message)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// MQTT连接建立时的处理，派生类可重写
        /// </summary>
        protected virtual Task OnMqttConnectedAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// MQTT连接断开时的处理，派生类可重写
        /// </summary>
        protected virtual Task OnMqttDisconnectedAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 订阅MQTT主题
        /// </summary>
        protected virtual async Task SubscribeToTopicsAsync(CancellationToken cancellationToken)
        {
            if (_mqttOptions.Topics.Subscribe?.Any() == true)
            {
                foreach (var topicPair in _mqttOptions.Topics.Subscribe)
                {
                    try
                    {
                        await _mqttService.SubscribeAsync(topicPair.Value, cancellationToken);
                        _logger.LogInformation("已订阅主题: {Topic} (服务: {Service})", topicPair.Value, topicPair.Key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "订阅主题失败: {Topic} (服务: {Service})", topicPair.Value, topicPair.Key);
                    }
                }
            }
        }

        /// <summary>
        /// 取消订阅MQTT主题
        /// </summary>
        protected virtual async Task UnsubscribeFromTopicsAsync(CancellationToken cancellationToken)
        {
            if (_mqttOptions.Topics.Subscribe?.Any() == true)
            {
                foreach (var topicPair in _mqttOptions.Topics.Subscribe)
                {
                    try
                    {
                        await _mqttService.UnsubscribeAsync(topicPair.Value, cancellationToken);
                        _logger.LogInformation("已取消订阅主题: {Topic} (服务: {Service})", topicPair.Value, topicPair.Key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "取消订阅主题失败: {Topic} (服务: {Service})", topicPair.Value, topicPair.Key);
                    }
                }
            }
        }
    }
}
