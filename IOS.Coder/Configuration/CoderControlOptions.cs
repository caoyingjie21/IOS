using IOS.Base.Configuration;

namespace IOS.Coder.Configuration;

/// <summary>
/// 读码器控制配置选项
/// </summary>
public class CoderControlOptions : IConfigurationOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public static string SectionName => "CoderControl";

    /// <summary>
    /// Socket服务器监听端口
    /// </summary>
    public int ListenPort { get; set; } = 8080;

    /// <summary>
    /// Socket服务器监听IP地址
    /// </summary>
    public string ListenAddress { get; set; } = "0.0.0.0";

    /// <summary>
    /// 接收缓冲区大小（字节）
    /// </summary>
    public int BufferSize { get; set; } = 1024;

    /// <summary>
    /// 连接超时时间（毫秒）
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30000;

    /// <summary>
    /// 读取超时时间（毫秒）
    /// </summary>
    public int ReadTimeout { get; set; } = 5000;

    /// <summary>
    /// 最大同时连接数
    /// </summary>
    public int MaxConnections { get; set; } = 10;

    /// <summary>
    /// 是否启用KeepAlive
    /// </summary>
    public bool EnableKeepAlive { get; set; } = true;

    /// <summary>
    /// KeepAlive间隔（毫秒）
    /// </summary>
    public int KeepAliveInterval { get; set; } = 30000;

    /// <summary>
    /// 是否启用自动重启
    /// </summary>
    public bool EnableAutoRestart { get; set; } = true;

    /// <summary>
    /// 重启间隔（毫秒）
    /// </summary>
    public int RestartInterval { get; set; } = 5000;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ListenAddress) &&
               ListenPort > 0 && ListenPort <= 65535 &&
               BufferSize > 0 &&
               ConnectionTimeout > 0 &&
               ReadTimeout > 0 &&
               MaxConnections > 0 &&
               KeepAliveInterval > 0 &&
               RestartInterval > 0 &&
               MaxRetries > 0;
    }

    /// <summary>
    /// 获取配置摘要信息
    /// </summary>
    public string GetSummary()
    {
        return $"Listen: {ListenAddress}:{ListenPort}, MaxConn: {MaxConnections}, Buffer: {BufferSize}B";
    }
} 