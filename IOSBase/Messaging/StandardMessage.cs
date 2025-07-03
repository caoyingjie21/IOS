using IOS.Base.Enums;
using System.Text.Json.Serialization;

namespace IOS.Base.Messaging
{
    /// <summary>
    /// 标准消息格式
    /// </summary>
    /// <typeparam name="T">消息数据类型</typeparam>
    public class StandardMessage<T>
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        [JsonPropertyName("messageId")]
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 消息版本
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "v1";

        /// <summary>
        /// 时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 消息类型
        /// </summary>
        [JsonPropertyName("messageType")]
        public MessageType MessageType { get; set; }

        /// <summary>
        /// 发送者
        /// </summary>
        [JsonPropertyName("sender")]
        public string Sender { get; set; } = string.Empty;

        /// <summary>
        /// 消息数据
        /// </summary>
        [JsonPropertyName("data")]
        public T? Data { get; set; }

        /// <summary>
        /// 元数据
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
} 