namespace IOS.Base.Configuration;

/// <summary>
/// MQTT配置选项
/// </summary>
public class MqttOptions
{
    public const string SectionName = "Mqtt";

    /// <summary>
    /// MQTT代理地址
    /// </summary>
    public string Broker { get; set; } = "localhost";

    /// <summary>
    /// MQTT端口
    /// </summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    /// 客户端ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 保活周期（秒）
    /// </summary>
    public int KeepAlivePeriod { get; set; } = 30;

    /// <summary>
    /// 重连延迟（毫秒）
    /// </summary>
    public int ReconnectDelay { get; set; } = 2000;

    /// <summary>
    /// 连接超时（秒）
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// 是否使用TLS
    /// </summary>
    public bool UseTls { get; set; } = false;

    /// <summary>
    /// 清除会话
    /// </summary>
    public bool CleanSession { get; set; } = true;

    /// <summary>
    /// 重连间隔（秒）
    /// </summary>
    public int ReconnectInterval { get; set; } = 5;
} 