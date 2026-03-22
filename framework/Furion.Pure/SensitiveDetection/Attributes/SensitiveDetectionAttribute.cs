// ------------------------------------------------------------------------
// 版权信息
// 版权归百小僧及百签科技（广东）有限公司所有。
// 所有权利保留。
// 官方网站：https://baiqian.com
//
// 许可证信息
// Furion 项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。
// 许可证的完整文本可以在源代码树根目录中的 LICENSE-APACHE 和 LICENSE-MIT 文件中找到。
// 官方网站：https://furion.net
//
// 使用条款
// 使用本代码应遵守相关法律法规和许可证的要求。
//
// 免责声明
// 对于因使用本代码而产生的任何直接、间接、偶然、特殊或后果性损害，我们不承担任何责任。
//
// 其他重要信息
// Furion 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。
// 有关 Furion 项目的其他详细信息，请参阅位于源代码树根目录中的 COPYRIGHT 和 DISCLAIMER 文件。
//
// 更多信息
// 请访问 https://gitee.com/dotnetchina/Furion 获取更多关于 Furion 项目的许可证和版权信息。
// ------------------------------------------------------------------------

using Furion.SensitiveDetection;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel.DataAnnotations;

/// <summary>
/// 脱敏词汇检查（脱敏处理）
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public class SensitiveDetectionAttribute : ValidationAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SensitiveDetectionAttribute()
        : base("The field {0} contains sensitive words.")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="transfer"></param>
    public SensitiveDetectionAttribute(char transfer)
        : this()
    {
        Transfer = transfer;
    }

    /// <summary>
    /// 替换为指定字符
    /// </summary>
    public char Transfer { get; set; }

    /// <summary>
    /// 是否显示命中的脱敏词汇
    /// </summary>
    public bool ShowSensitiveWords { get; set; }

    /// <summary>
    /// 验证逻辑
    /// </summary>
    /// <param name="value"></param>
    /// <param name="validationContext"></param>
    /// <returns></returns>
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        // null 、非字符串和空字符串跳过检查
        if (value == null || value.GetType() != typeof(string) || (value is string str && string.IsNullOrWhiteSpace(str)))
        {
            return ValidationResult.Success;
        }

        // 获取脱敏提供器（如果未注册，直接跳过，而不是抛异常）
        if (validationContext.GetService<ISensitiveDetectionProvider>() is not { } sensitiveWordsProvider)
        {
            return ValidationResult.Success;
        }

        var strValue = value.ToString();

        // 如果没有传入替换字符，则直接校验
        if (Transfer == default)
        {
            // 判断是否显示敏感词汇
            if (!ShowSensitiveWords)
            {
                // 判断符合
                var isValid = sensitiveWordsProvider.IsValid(strValue);

                if (!isValid)
                {
                    return new ValidationResult(FormatErrorMessage(validationContext.DisplayName), validationContext.MemberName is null ? null : [validationContext.MemberName]);
                }

                // 验证成功
                return ValidationResult.Success;
            }
            else
            {
                // 获取命中的敏感词汇集合
                var foundSets = sensitiveWordsProvider.FoundSensitiveWords(strValue);
                if (foundSets.Count > 0)
                {
                    return new ValidationResult(FormatErrorMessage(validationContext.DisplayName, foundSets), validationContext.MemberName is null ? null : [validationContext.MemberName]);
                }

                // 验证成功
                return ValidationResult.Success;
            }
        }
        // 替换敏感词汇
        else
        {
            // 单个值已在模型绑定中处理
            if (validationContext.ObjectType == typeof(string)) return ValidationResult.Success;

            // 替换字符
            var newValue = sensitiveWordsProvider.Replace(strValue, Transfer);

            // 如果不包含敏感词汇直接返回
            if (newValue == strValue) return ValidationResult.Success;

            // 将对象属性值进行替换
            validationContext.ObjectType.GetProperty(validationContext.MemberName, BindingFlags.Public | BindingFlags.Instance)
                .SetValue(validationContext.ObjectInstance, newValue);

            // 验证成功
            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// 格式化消息
    /// </summary>
    /// <param name="name"></param>
    /// <param name="foundSets"></param>
    /// <returns></returns>
    private string FormatErrorMessage(string name, Dictionary<string, List<int>>? foundSets)
    {
        if (foundSets is null || foundSets.Count == 0)
        {
            return FormatErrorMessage(name);
        }

        var sensitiveWords = string.Join(" ", foundSets.Select(kv => $"[{kv.Key}@{string.Join(",", kv.Value)}]"));
        return string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, sensitiveWords);
    }
}