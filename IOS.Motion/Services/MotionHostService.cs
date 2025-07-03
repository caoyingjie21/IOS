using Core.Net.EtherCAT;
using Core.Net.EtherCAT.SeedWork;
using IOS.Base.Configuration;
using IOS.Base.Enums;
using IOS.Base.Messaging;
using IOS.Base.Mqtt;
using IOS.Base.Services;
using IOS.Motion.Configuration;
using IOS.Motion.MessageHandlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IOS.Motion.Services
{
    /// <summary>
    /// 电机模块主机服务
    /// </summary>
    public class MotionHostService : BaseHostService
    {
        private readonly MotionMessageHandlerFactory _messageHandlerFactory;
        private readonly EtherCATMaster _etherCATMaster;
        private IEtherCATSlave_CiA402? _axis;
        private readonly MotionControlOptions _motionOptions;
        
        public MotionHostService(
            IMqttService mqttService, 
            ILogger<MotionHostService> logger, 
            IOptions<StandardMqttOptions> mqttOptions,
            MotionMessageHandlerFactory messageHandlerFactory, 
            EtherCATMaster etherCATMaster, 
            IOptions<MotionControlOptions> motionControlOptions) 
            : base(mqttService, logger, mqttOptions)
        {
            _messageHandlerFactory = messageHandlerFactory;
            _etherCATMaster = etherCATMaster;
            _motionOptions = motionControlOptions.Value;
        }

        protected override async Task OnServiceStartingAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("电机模块服务正在初始化...");
            var m = _motionOptions;
            // 检查网络接口配置
            //if (string.IsNullOrEmpty(_motionOptions.EtherNet))
            //{
            //    throw new InvalidOperationException("请配置EtherNet地址");
            //}

            //// 创建轴实例
            //_axis = new EtherCATSlave_CiA402_1(_etherCATMaster, _motionOptions.SlaveId);

            //// 启动EtherCAT主站
            //_etherCATMaster.StartActivity(_motionOptions.EtherNet);
            //await Task.Delay(500);

            //// 配置轴参数
            //_etherCATMaster.WriteSDO<uint>(1, 0x6091, 0x01, 1);
            //_etherCATMaster.WriteSDO<uint>(1, 0x6091, 0x02, 1);
            //_etherCATMaster.WriteSDO<uint>(1, 0x6092, 0x01, (uint)_motionOptions.PulseRatio);
            //_etherCATMaster.WriteSDO<uint>(1, 0x6092, 0x02, 1);

            //// 复位并上电
            //_axis.Reset();
            //_axis.PowerOn();

            await base.OnServiceStartingAsync(cancellationToken);
        }

        protected override async Task OnServiceStoppingAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("电机模块服务正在清理资源...");
            
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
            _logger.LogDebug("电机模块收到MQTT消息 - 主题: {Topic}", topic);
            
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
            _logger.LogInformation("电机模块MQTT连接已建立，发布上线状态");
            await PublishServiceStatusAsync("在线");
        }

        protected override async Task OnMqttDisconnectedAsync()
        {
            _logger.LogWarning("电机模块MQTT连接已断开");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 发布服务状态
        /// </summary>
        private async Task PublishServiceStatusAsync(string status, CancellationToken cancellationToken = default)
        {
            var statusData = new
            {
                Service = "IOS.Motion",
                Status = status,
                Timestamp = DateTime.UtcNow,
                Version = _mqttOptions.Messages.Version
            };

            var topic = GetPublishTopic(TopicType.Sensor);
            if (!string.IsNullOrEmpty(topic))
            {
                var message = new StandardMessage<object>
                {
                    MessageType = "service_status",
                    Sender = "IOS.Motion",
                    Data = statusData
                };
                
                await _mqttService.PublishAsync(topic, message, cancellationToken);
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