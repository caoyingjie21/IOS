using System.ComponentModel;

namespace IOS.Base.Enums
{
    /// <summary>
    /// 主题类型枚举，用于标识不同的MQTT主题类型
    /// </summary>
    public enum TopicType
    {
        /// <summary>
        /// 传感器相关主题
        /// </summary>
        [Description("Sensor")]
        Sensor,

        /// <summary>
        /// 视觉系统相关主题
        /// </summary>
        [Description("Vision")]
        Vision,

        /// <summary>
        /// 运动控制相关主题
        /// </summary>
        [Description("Motion")]
        Motion,

        /// <summary>
        /// 编码器相关主题
        /// </summary>
        [Description("Coder")]
        Coder,

        /// <summary>
        /// 读码器结果数据主题
        /// </summary>
        [Description("CoderResult")]
        CoderResult,

        /// <summary>
        /// 读码器状态主题
        /// </summary>
        [Description("CoderStatus")]
        CoderStatus,

        /// <summary>
        /// 调度器相关主题
        /// </summary>
        [Description("Scheduler")]
        Scheduler,

        /// <summary>
        /// 相机高度
        /// </summary>
        [Description("VisionHeight")]
        VisionHeight


    }
} 