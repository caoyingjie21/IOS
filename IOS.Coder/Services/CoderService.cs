using IOS.Coder.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;

namespace IOS.Coder.Services;

/// <summary>
/// 读码器Socket服务类
/// </summary>
public class CoderService : IDisposable
{
    private readonly IOptions<CoderControlOptions> _options;
    private readonly ILogger<CoderService> _logger;
    private TcpListener? _tcpListener;
    private bool _isRunning;
    private readonly object _lock = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly ConcurrentDictionary<string, TcpClient> _clients = new();
    private Task? _listenTask;

    /// <summary>
    /// 数据接收事件
    /// </summary>
    public event EventHandler<CoderDataEventArgs>? DataReceived;

    /// <summary>
    /// 状态变化事件
    /// </summary>
    public event EventHandler<CoderStatusEventArgs>? StatusChanged;

    /// <summary>
    /// 客户端连接事件
    /// </summary>
    public event EventHandler<CoderClientEventArgs>? ClientConnected;

    /// <summary>
    /// 客户端断开事件
    /// </summary>
    public event EventHandler<CoderClientEventArgs>? ClientDisconnected;

    /// <summary>
    /// 服务运行状态
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// 当前连接的客户端数量
    /// </summary>
    public int ConnectedClients => _clients.Count;

    public CoderService(IOptions<CoderControlOptions> options, ILogger<CoderService> logger)
    {
        _options = options;
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// 启动Socket服务器
    /// </summary>
    public Task<bool> StartAsync()
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                _logger.LogWarning("Socket服务器已运行");
                return Task.FromResult(true);
            }

            try
            {
                var config = _options.Value;
                _logger.LogInformation("启动Socket服务器 - 地址: {Address}:{Port}", 
                    config.ListenAddress, config.ListenPort);

                var ipAddress = IPAddress.Parse(config.ListenAddress);
                _tcpListener = new TcpListener(ipAddress, config.ListenPort);
                _tcpListener.Start();

                _isRunning = true;
                _cancellationTokenSource = new CancellationTokenSource();

                // 启动监听任务
                _listenTask = Task.Run(async () => await ListenForClientsAsync(_cancellationTokenSource.Token));

                _logger.LogInformation("Socket服务器启动成功，监听端口: {Port}", config.ListenPort);
                OnStatusChanged(new CoderStatusEventArgs("服务器已启动", true));

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动Socket服务器失败");
                OnStatusChanged(new CoderStatusEventArgs($"启动失败: {ex.Message}", false));
                return Task.FromResult(false);
            }
        }
    }

    /// <summary>
    /// 停止Socket服务器
    /// </summary>
    public async Task StopAsync()
    {
        bool shouldStop = false;
        TcpListener? listenerToStop = null;
        List<TcpClient> clientsToClose = new();
        Task? taskToWait = null;

        lock (_lock)
        {
            if (!_isRunning)
            {
                _logger.LogWarning("Socket服务器未运行");
                return;
            }

            shouldStop = true;
            _isRunning = false;
            _cancellationTokenSource?.Cancel();

            // 收集需要关闭的资源
            listenerToStop = _tcpListener;
            clientsToClose.AddRange(_clients.Values);
            taskToWait = _listenTask;

            // 清空集合
            _clients.Clear();
            _tcpListener = null;
        }

        if (shouldStop)
        {
            try
            {
                // 断开所有客户端连接
                foreach (var client in clientsToClose)
                {
                    try
                    {
                        client.Close();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "关闭客户端连接时发生错误");
                    }
                }

                // 停止监听
                listenerToStop?.Stop();

                // 等待监听任务完成
                if (taskToWait != null)
                {
                    await taskToWait;
                }

                _logger.LogInformation("Socket服务器已停止");
                OnStatusChanged(new CoderStatusEventArgs("服务器已停止", false));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止Socket服务器失败");
            }
        }
    }

    /// <summary>
    /// 监听客户端连接
    /// </summary>
    private async Task ListenForClientsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始监听客户端连接");

        while (_isRunning && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var tcpClient = await _tcpListener!.AcceptTcpClientAsync();
                var clientId = Guid.NewGuid().ToString();
                var clientEndpoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "Unknown";

                _logger.LogInformation("新客户端连接: {ClientId} from {Endpoint}", clientId, clientEndpoint);

                _clients.TryAdd(clientId, tcpClient);
                OnClientConnected(new CoderClientEventArgs(clientId, clientEndpoint));

                // 为每个客户端启动处理任务
                _ = Task.Run(async () => await HandleClientAsync(clientId, tcpClient, cancellationToken));
            }
            catch (ObjectDisposedException)
            {
                // 服务器已停止，正常退出
                break;
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    _logger.LogError(ex, "接受客户端连接时发生错误");
                    await Task.Delay(1000, cancellationToken); // 短暂延迟后重试
                }
            }
        }

        _logger.LogInformation("停止监听客户端连接");
    }

    /// <summary>
    /// 处理客户端连接
    /// </summary>
    private async Task HandleClientAsync(string clientId, TcpClient tcpClient, CancellationToken cancellationToken)
    {
        var clientEndpoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        _logger.LogDebug("开始处理客户端: {ClientId}", clientId);

        try
        {
            var config = _options.Value;
            var buffer = new byte[config.BufferSize];
            var stream = tcpClient.GetStream();

            // 设置超时
            stream.ReadTimeout = config.ReadTimeout;

            while (_isRunning && tcpClient.Connected && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead == 0)
                    {
                        _logger.LogInformation("客户端 {ClientId} 断开连接", clientId);
                        break;
                    }

                    var data = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    if (!string.IsNullOrEmpty(data))
                    {
                        _logger.LogDebug("接收到来自客户端 {ClientId} 的数据: {Data}", clientId, data);
                        OnDataReceived(new CoderDataEventArgs(data, clientId, clientEndpoint));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理客户端 {ClientId} 数据时发生错误", clientId);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理客户端 {ClientId} 时发生错误", clientId);
        }
        finally
        {
            // 清理客户端连接
            _clients.TryRemove(clientId, out _);
            tcpClient.Close();
            OnClientDisconnected(new CoderClientEventArgs(clientId, clientEndpoint));
            _logger.LogInformation("客户端 {ClientId} 连接已关闭", clientId);
        }
    }

    /// <summary>
    /// 向指定客户端发送数据
    /// </summary>
    public async Task<bool> SendToClientAsync(string clientId, string data)
    {
        if (!_clients.TryGetValue(clientId, out var client) || !client.Connected)
        {
            _logger.LogWarning("客户端 {ClientId} 不存在或未连接", clientId);
            return false;
        }

        try
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            var stream = client.GetStream();
            await stream.WriteAsync(bytes, 0, bytes.Length);
            await stream.FlushAsync();
            
            _logger.LogDebug("向客户端 {ClientId} 发送数据: {Data}", clientId, data);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "向客户端 {ClientId} 发送数据失败", clientId);
            return false;
        }
    }

    /// <summary>
    /// 向所有客户端广播数据
    /// </summary>
    public async Task<int> BroadcastAsync(string data)
    {
        int successCount = 0;
        var tasks = new List<Task<bool>>();

        foreach (var clientId in _clients.Keys)
        {
            tasks.Add(SendToClientAsync(clientId, data));
        }

        var results = await Task.WhenAll(tasks);
        successCount = results.Count(r => r);

        _logger.LogDebug("广播数据到 {Total} 个客户端，成功 {Success} 个", _clients.Count, successCount);
        return successCount;
    }

    /// <summary>
    /// 触发数据接收事件
    /// </summary>
    private void OnDataReceived(CoderDataEventArgs e)
    {
        DataReceived?.Invoke(this, e);
    }

    /// <summary>
    /// 触发状态变化事件
    /// </summary>
    private void OnStatusChanged(CoderStatusEventArgs e)
    {
        StatusChanged?.Invoke(this, e);
    }

    /// <summary>
    /// 触发客户端连接事件
    /// </summary>
    private void OnClientConnected(CoderClientEventArgs e)
    {
        ClientConnected?.Invoke(this, e);
    }

    /// <summary>
    /// 触发客户端断开事件
    /// </summary>
    private void OnClientDisconnected(CoderClientEventArgs e)
    {
        ClientDisconnected?.Invoke(this, e);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        StopAsync().Wait();
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
}

/// <summary>
/// 读码器数据事件参数
/// </summary>
public class CoderDataEventArgs : EventArgs
{
    public string Data { get; }
    public string ClientId { get; }
    public string ClientEndpoint { get; }
    public DateTime Timestamp { get; }

    public CoderDataEventArgs(string data, string clientId = "", string clientEndpoint = "")
    {
        Data = data;
        ClientId = clientId;
        ClientEndpoint = clientEndpoint;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// 读码器状态事件参数
/// </summary>
public class CoderStatusEventArgs : EventArgs
{
    public string Status { get; }
    public bool IsRunning { get; }
    public DateTime Timestamp { get; }

    public CoderStatusEventArgs(string status, bool isRunning)
    {
        Status = status;
        IsRunning = isRunning;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// 读码器客户端事件参数
/// </summary>
public class CoderClientEventArgs : EventArgs
{
    public string ClientId { get; }
    public string ClientEndpoint { get; }
    public DateTime Timestamp { get; }

    public CoderClientEventArgs(string clientId, string clientEndpoint)
    {
        ClientId = clientId;
        ClientEndpoint = clientEndpoint;
        Timestamp = DateTime.UtcNow;
    }
} 