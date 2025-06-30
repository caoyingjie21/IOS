using IOS.Base.Configuration;

namespace IOS.Motion.Configuration;

/// <summary>
/// 电机控制配置选项
/// </summary>
public class MotionControlOptions : IConfigurationOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public static string SectionName => "MotionControl";

    /// <summary>
    /// 以太网类型
    /// </summary>
    public string EtherNet { get; set; } = "CNet";

    public int Speed { get; set; } = 50000;

    public int MinPosition { get; set; } = 0;

    public int MaxPosition { get; set; } = 220000;

    public int SlaveId { get; set; } = 1;

    public int PulseRatio { get; set; } = 1000;

    /// <summary>
    /// 获取配置摘要信息
    /// </summary>
    public string GetSummary()
    {
        return $"EtherNet: {EtherNet}, Range: [{MinPosition}, {MaxPosition}], PulseRatio: {PulseRatio}, 从站Id:{SlaveId}，速度:{Speed}";
    }

    public bool IsValid()
    {
        if(string.IsNullOrWhiteSpace(EtherNet))
        {
            return false;
        }
        return true;
    }
} 