using IOS.Base.Configuration;
using Microsoft.Extensions.Options;

namespace IOS.Motion.Configuration;

/// <summary>
/// MotionControl 配置选项验证器
/// </summary>
public class MotionControlOptionsValidator : IOptionsValidator<MotionControlOptions>
{
    public ValidateOptionsResult Validate(string? name, MotionControlOptions options)
    {
        var errors = new List<string>();

        // 验证阈值范围
        if (options.MaxPosition <= options.MinPosition)
        {
            errors.Add("最大阈值必须大于最小阈值");
        }

        // 验证脉冲比例
        if (options.PulseRatio <= 0)
        {
            errors.Add("脉冲比例必须大于0");
        }

        // 验证以太网类型
        if (string.IsNullOrWhiteSpace(options.EtherNet))
        {
            errors.Add("以太网类型不能为空");
        }

        if (errors.Any())
        {
            var errorMessage = $"MotionControl 配置验证失败：{string.Join("; ", errors)}";
            return ValidateOptionsResult.Fail(errorMessage);
        }

        return ValidateOptionsResult.Success;
    }
} 