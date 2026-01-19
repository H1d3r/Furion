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

using Furion.Validation;
using Furion.Validation.Resources;
using System.Globalization;

namespace System.ComponentModel.DataAnnotations;

/// <summary>
///     <inheritdoc cref="EnumAttribute" />
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class EnumAttribute<TEnum> : EnumAttribute
    where TEnum : struct, Enum
{
    /// <inheritdoc />
    public EnumAttribute() : base(typeof(TEnum))
    {
    }
}

/// <summary>
///     枚举验证特性
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class EnumAttribute : ValidationBaseAttribute
{
    /// <inheritdoc cref="EnumValidator" />
    internal readonly EnumValidator _validator;

    /// <summary>
    ///     <inheritdoc cref="EnumAttribute" />
    /// </summary>
    public EnumAttribute(Type enumType)
    {
        EnumType = enumType;
        _validator = new EnumValidator(enumType);

        UseResourceKey(GetResourceKey);
    }

    /// <summary>
    ///     枚举类型
    /// </summary>
    public Type EnumType { get; }

    /// <summary>
    ///     是否支持 Flags 模式
    /// </summary>
    /// <remarks>默认值为：<c>false</c>。</remarks>
    public bool SupportFlags
    {
        get;
        set
        {
            field = value;
            _validator.SupportFlags = value;
        }
    }

    /// <inheritdoc />
    public override bool IsValid(object? value) => _validator.IsValid(value);

    /// <inheritdoc />
    public override string FormatErrorMessage(string name) =>
        string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, EnumType.Name);

    /// <summary>
    ///     获取错误信息对应的资源键
    /// </summary>
    /// <returns>
    ///     <see cref="string" />
    /// </returns>
    internal string GetResourceKey() =>
        SupportFlags
            ? nameof(ValidationMessages.EnumValidator_ValidationError_SupportFlags)
            : nameof(ValidationMessages.EnumValidator_ValidationError);
}