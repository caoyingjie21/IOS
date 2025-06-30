using IOS.Base.Configuration;
using IOS.Base.Services;
using IOS.Base.Mqtt;
using IOS.Base.Messaging;
using IOS.Coder.Configuration;
using IOS.Coder.MessageHandlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IOS.Base.Enums;

namespace IOS.Coder.Services;

/// <summary>
/// 读码器主机服务
/// </summary>
public class CoderHostService : BaseHostService
{
    private readonly IOptions<CoderControlOptions> _coderOptions;
    private readonly CoderService _coderService;
    private readonly SharedDataService _sharedDataService;
    private readonly CoderMessageHandlerFactory _messageHandlerFactory;

    public CoderHostService(
        IMqttService mqttService,
        ILogger<CoderHostService> logger,
        IOptions<StandardMqttOptions> mqttOptions,
        IOptions<CoderControlOptions> coderOptions,
        CoderService coderService,
        SharedDataService sharedDataService,
        CoderMessageHandlerFactory messageHandlerFactory)
        : base(mqttService, logger, mqttOptions)
    {
        _coderOptions = coderOptions;
        _coderService = coderService;
        _sharedDataService = sharedDataService;
        _messageHandlerFactory = messageHandlerFactory;
    }

    protected override async Task OnServiceStartingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("读码器服务正在初始化...");

        // 设置读码器事件处理器
        SetupCoderEventHandlers();

        // 启动Socket服务器
        var startResult = await _coderService.StartAsync();
        if (!startResult)
        {
            throw new InvalidOperationException("读码器Socket服务器启动失败");
        }

        _logger.LogInformation("读码器Socket服务器启动成功，监听端口: {Port}", _coderOptions.Value.ListenPort);

        await base.OnServiceStartingAsync(cancellationToken);
    }

    protected override async Task OnServiceStoppingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("读码器服务正在清理资源...");

        // 发布服务停止状态
        try
        {
            await PublishCoderStatusAsync("停止", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布服务停止状态失败");
        }

        // 停止Socket服务器
        await _coderService.StopAsync();

        await base.OnServiceStoppingAsync(cancellationToken);
    }

    protected override Task DoWorkAsync(CancellationToken stoppingToken)
    {
        //// 定期发布状态
        //while (!stoppingToken.IsCancellationRequested)
        //{
        //    try
        //    {
        //        await PublishServiceStatusAsync("运行中", stoppingToken);
        //        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        break;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "发布状态消息失败");
        //        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        //    }
        //}
        return Task.CompletedTask;
    }

    protected override async Task HandleMqttMessageAsync(string topic, string message)
    {
        _logger.LogDebug("读码器收到MQTT消息 - 主题: {Topic}", topic);

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
        _logger.LogInformation("读码器MQTT连接已建立，发布上线状态");
        //await PublishServiceStatusAsync("在线");
        await Task.CompletedTask;
    }

    protected override async Task OnMqttDisconnectedAsync()
    {
        _logger.LogWarning("读码器MQTT连接已断开");
        await Task.CompletedTask;
    }

    /// <summary>
    /// 设置读码器事件处理器
    /// </summary>
    private void SetupCoderEventHandlers()
    {
        // 数据接收事件
        _coderService.DataReceived += async (sender, e) =>
        {
            try
            {
                _logger.LogInformation("接收到读码器数据: {Data} from {ClientId}", e.Data, e.ClientId);

                // 保存到共享数据服务
                _sharedDataService.SetData("LastCoderData", e.Data);
                _sharedDataService.SetData("LastCoderDataTime", e.Timestamp);
                _sharedDataService.SetData("LastCoderClientId", e.ClientId);
                _sharedDataService.SetData("LastCoderClientEndpoint", e.ClientEndpoint);

                // 发布到MQTT
                var publishData = new
                {
                    Data = e.Data,
                    ClientId = e.ClientId,
                    ClientEndpoint = e.ClientEndpoint,
                    Timestamp = e.Timestamp,
                    Source = "Coder"
                };

                await PublishCoderDataAsync(publishData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理读码器数据失败");
            }
        };

        // 状态变化事件
        _coderService.StatusChanged += async (sender, e) =>
        {
            try
            {
                _logger.LogInformation("读码器状态变化: {Status}", e.Status);

                // 保存状态
                _sharedDataService.SetData("CoderStatus", e.Status);
                _sharedDataService.SetData("CoderStatusTime", e.Timestamp);
                _sharedDataService.SetData("CoderIsRunning", e.IsRunning);

                // 发布状态变化
                var statusData = new
                {
                    Status = e.Status,
                    IsRunning = e.IsRunning,
                    Timestamp = e.Timestamp,
                    ConnectedClients = _coderService.ConnectedClients
                };

                await PublishCoderStatusAsync(statusData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理读码器状态变化失败");
            }
        };

        // 客户端连接事件
        _coderService.ClientConnected += async (sender, e) =>
        {
            try
            {
                _logger.LogInformation("读码器客户端连接: {ClientId} from {Endpoint}", e.ClientId, e.ClientEndpoint);

                var clientData = new
                {
                    ClientId = e.ClientId,
                    ClientEndpoint = e.ClientEndpoint,
                    Action = "Connected",
                    Timestamp = e.Timestamp,
                    TotalClients = _coderService.ConnectedClients
                };

                await PublishCoderStatusAsync(clientData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理客户端连接事件失败");
            }
        };

        // 客户端断开事件
        _coderService.ClientDisconnected += async (sender, e) =>
        {
            try
            {
                _logger.LogInformation("读码器客户端断开: {ClientId} from {Endpoint}", e.ClientId, e.ClientEndpoint);

                var clientData = new
                {
                    ClientId = e.ClientId,
                    ClientEndpoint = e.ClientEndpoint,
                    Action = "Disconnected",
                    Timestamp = e.Timestamp,
                    TotalClients = _coderService.ConnectedClients
                };

                await PublishCoderStatusAsync(clientData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理客户端断开事件失败");
            }
        };
    }

    /// <summary>
    /// 发布读码器数据
    /// </summary>
    private async Task PublishCoderDataAsync(object data, CancellationToken cancellationToken = default)
    {
        var topic = GetPublishTopic(TopicType.CoderResult);
        if (!string.IsNullOrEmpty(topic))
        {
            await PublishMessageAsync(topic, data, "CoderData", cancellationToken);
        }
    }

    /// <summary>
    /// 发布读码器状态
    /// </summary>
    private async Task PublishCoderStatusAsync(object data, CancellationToken cancellationToken = default)
    {
        var topic = GetPublishTopic(TopicType.CoderStatus);
        if (!string.IsNullOrEmpty(topic))
        {
            await PublishMessageAsync(topic, data, "CoderStatus", cancellationToken);
        }
    }

    /// <summary>
    /// 发布服务状态
    /// </summary>
    //private async Task PublishServiceStatusAsync(string status, CancellationToken cancellationToken = default)
    //{
    //    var statusData = new
    //    {
    //        Service = "IOS.Coder",
    //        Status = status,
    //        Timestamp = DateTime.UtcNow,
    //        Version = _mqttOptions.Messages.Version,
    //        Configuration = _coderOptions.Value.GetSummary(),
    //        ConnectedClients = _coderService.ConnectedClients,
    //        IsRunning = _coderService.IsRunning
    //    };

    //    var topic = GetPublishTopic(TopicType.Sensor);
    //    if (!string.IsNullOrEmpty(topic))
    //    {
    //        await PublishMessageAsync(topic, statusData, "service_status", cancellationToken);
    //    }
    //}

    /// <summary>
    /// 获取发布主题
    /// </summary>
    private string? GetPublishTopic(TopicType topicType)
    {
        var key = topicType.ToString();

        // 从配置中获取发布主题
        if (_mqttOptions.Topics.Publish?.ContainsKey(key) == true)
        {
            return _mqttOptions.Topics.Publish[key];
        }

        // 如果没有配置，尝试从Publish字典的值中匹配
        return _mqttOptions.Topics.Publish?.Values.FirstOrDefault(t => t.Contains(key));
    }
} 