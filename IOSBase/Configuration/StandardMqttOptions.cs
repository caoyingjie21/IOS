namespace IOS.Base.Configuration;

/// <summary>
/// 标准MQTT配置选项
/// </summary>
public class StandardMqttOptions
{
    public const string SectionName = "StandardMqtt";

    /// <summary>
    /// 主题配置
    /// </summary>
    public TopicsOptions Topics { get; set; } = new();

    /// <summary>
    /// 消息配置
    /// </summary>
    public MessagesOptions Messages { get; set; } = new();
}

/// <summary>
/// 主题配置选项
/// </summary>
public class TopicsOptions
{
    ///// <summary>
    ///// 订阅主题列表
    ///// </summary>
    //public List<string> Subscriptions { get; set; } = new();

    ///// <summary>
    ///// 发布主题列表
    ///// </summary>
    //public List<string> Publications { get; set; } = new();

    /// <summary>
    /// 订阅主题字典（键值对形式）
    /// </summary>
    public Dictionary<string, string> Subscribe { get; set; } = new();

    /// <summary>
    /// 发布主题字典（键值对形式）
    /// </summary>
    public Dictionary<string, string> Publish { get; set; } = new();
}

/// <summary>
/// 消息配置选项
/// </summary>
public class MessagesOptions
{
    /// <summary>
    /// 消息版本
    /// </summary>
    public string Version { get; set; } = "v1";

    /// <summary>
    /// 启用消息验证
    /// </summary>
    public bool EnableValidation { get; set; } = true;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 消息压缩
    /// </summary>
    public bool EnableCompression { get; set; } = false;

    /// <summary>
    /// 消息加密
    /// </summary>
    public bool EnableEncryption { get; set; } = false;
} 