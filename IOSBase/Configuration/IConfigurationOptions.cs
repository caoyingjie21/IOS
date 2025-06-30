namespace IOS.Base.Configuration;

/// <summary>
/// 配置选项接口
/// </summary>
public interface IConfigurationOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    static abstract string SectionName { get; }

    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    /// <returns>配置是否有效</returns>
    bool IsValid();
} 