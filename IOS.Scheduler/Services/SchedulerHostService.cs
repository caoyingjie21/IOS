using System.Text.Json;
using IOS.Base.Configuration;
using IOS.Base.Mqtt;
using IOS.Base.Services;
using IOS.Base.Messaging;
using IOS.Scheduler.MessageHandlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IOS.Base.Enums;

namespace IOS.Scheduler.Services
{
    /// <summary>
    /// 调度器主机服务
    /// </summary>
    public class SchedulerHostService : BaseHostService
    {
        private readonly SchedulerMessageHandlerFactory _messageHandlerFactory;

        public SchedulerHostService(
            IMqttService mqttService, 
            ILogger<SchedulerHostService> logger, 
            IOptions<StandardMqttOptions> mqttOptions,
            SchedulerMessageHandlerFactory messageHandlerFactory) 
            : base(mqttService, logger, mqttOptions)
        {
            _messageHandlerFactory = messageHandlerFactory;
         
        }

        protected override async Task OnServiceStartingAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("调度器服务正在初始化...");
            
            await base.OnServiceStartingAsync(cancellationToken);
        }

        protected override async Task OnServiceStoppingAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("调度器服务正在清理资源...");
            
            // 发布服务停止状态
            try
            {
                await PublishServiceStatusAsync("停止", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布服务停止状态失败");
            }
            
            await base.OnServiceStoppingAsync(cancellationToken);
        }

        protected override async Task DoWorkAsync(CancellationToken stoppingToken)
        {
            await Task.CompletedTask;
        }

        protected override async Task HandleMqttMessageAsync(string topic, string message)
        {
            _logger.LogDebug("调度器收到MQTT消息 - 主题: {Topic}", topic);
            
            try
            {
                // 使用消息处理器工厂创建合适的处理器
                var handler = _messageHandlerFactory.CreateHandler(topic);
                await handler.HandleMessageAsync(topic, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理MQTT消息失败 - 主题: {Topic}", topic);
            }
        }

        protected override async Task OnMqttConnectedAsync()
        {
            _logger.LogInformation("调度器MQTT连接已建立，发布上线状态");
            await PublishServiceStatusAsync("在线");
        }

        protected override async Task OnMqttDisconnectedAsync()
        {
            _logger.LogWarning("调度器MQTT连接已断开");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 发布服务状态
        /// </summary>
        private async Task PublishServiceStatusAsync(string status, CancellationToken cancellationToken = default)
        {
            var statusData = new
            {
                Service = "IOS.Scheduler",
                Status = status,
                Timestamp = DateTime.UtcNow,
                Version = _mqttOptions.Messages.Version
            };

            var topic = GetPublishTopic(TopicType.Sensor);
            _logger.LogInformation(topic);
            if (!string.IsNullOrEmpty(topic))
            {
                await _mqttService.PublishStandardMessageAsync(topic, statusData, MessageType.Data, cancellationToken);
            }
        }

        /// <summary>
        /// 获取发布主题
        /// </summary>
        private string? GetPublishTopic(TopicType topic)
        {
            var key = topic.ToString();
            
            // 从配置中获取发布主题
            if (_mqttOptions.Topics.Publish?.ContainsKey(key) == true)
            {
                return _mqttOptions.Topics.Publish[key];
            }
            
            // 如果没有配置，尝试从Publish字典的值中匹配
            return _mqttOptions.Topics.Publish?.Values.FirstOrDefault(t => t.Contains(key));
        }
    }
}
