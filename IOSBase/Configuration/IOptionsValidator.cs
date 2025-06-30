using Microsoft.Extensions.Options;

namespace IOS.Base.Configuration;

/// <summary>
/// 泛型配置验证器接口
/// </summary>
/// <typeparam name="T">配置选项类型</typeparam>
public interface IOptionsValidator<T> : IValidateOptions<T> where T : class, IConfigurationOptions
{
}

/// <summary>
/// 基础配置验证器
/// </summary>
/// <typeparam name="T">配置选项类型</typeparam>
public class BaseOptionsValidator<T> : IOptionsValidator<T> where T : class, IConfigurationOptions
{
    public ValidateOptionsResult Validate(string? name, T options)
    {
        if (!options.IsValid())
        {
            return ValidateOptionsResult.Fail($"{typeof(T).Name} 配置无效：请检查配置参数设置");
        }

        return ValidateOptionsResult.Success;
    }
} 