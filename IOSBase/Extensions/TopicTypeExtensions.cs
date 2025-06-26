using System.ComponentModel;
using IOS.Base.Enums;

namespace IOS.Base.Extensions
{
    /// <summary>
    /// TopicType 枚举扩展方法
    /// </summary>
    public static class TopicTypeExtensions
    {
        /// <summary>
        /// 获取枚举对应的字符串值
        /// </summary>
        /// <param name="topicType">主题类型枚举</param>
        /// <returns>对应的字符串值</returns>
        public static string ToTopicString(this TopicType topicType)
        {
            var field = topicType.GetType().GetField(topicType.ToString());
            var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field!, typeof(DescriptionAttribute));
            return attribute?.Description ?? topicType.ToString();
        }

        /// <summary>
        /// 从字符串解析为枚举
        /// </summary>
        /// <param name="topicString">主题字符串</param>
        /// <returns>对应的枚举值，如果找不到则返回null</returns>
        public static TopicType? FromTopicString(string topicString)
        {
            foreach (TopicType topicType in System.Enum.GetValues<TopicType>())
            {
                if (topicType.ToTopicString().Equals(topicString, StringComparison.OrdinalIgnoreCase))
                {
                    return topicType;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取所有枚举值及其对应的字符串
        /// </summary>
        /// <returns>枚举值和字符串的字典</returns>
        public static Dictionary<TopicType, string> GetAllTopicMappings()
        {
            var mappings = new Dictionary<TopicType, string>();
            foreach (TopicType topicType in System.Enum.GetValues<TopicType>())
            {
                mappings[topicType] = topicType.ToTopicString();
            }
            return mappings;
        }
    }
} 