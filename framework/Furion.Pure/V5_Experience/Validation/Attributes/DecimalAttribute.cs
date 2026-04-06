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
///     验证值是否为有效的 <see cref="decimal" /> 类型验证特性
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class DecimalAttribute : ValidationBaseAttribute
{
    /// <inheritdoc cref="DecimalValidator" />
    internal readonly DecimalValidator _validator;

    /// <summary>
    ///     <inheritdoc cref="DecimalAttribute" />
    /// </summary>
    /// <param name="precision">总位数（精度），默认值为：<c>18</c></param>
    /// <param name="scale">小数位数（标度），默认值为：<c>2</c></param>
    public DecimalAttribute(int precision = 18, int scale = 2)
    {
        Precision = precision;
        Scale = scale;
        _validator = new DecimalValidator(precision, scale);

        UseResourceKey(GetResourceKey);
    }

    /// <summary>
    ///     总位数（精度）
    /// </summary>
    public int Precision { get; }

    /// <summary>
    ///     小数位数（标度）
    /// </summary>
    public int Scale { get; }

    /// <summary>
    ///     是否允许负数
    /// </summary>
    /// <remarks>默认值为：<c>false</c>。</remarks>
    public bool AllowNegative
    {
        get;
        set
        {
            field = value;
            _validator.AllowNegative = value;
        }
    }

    /// <summary>
    ///     是否允许字符串数值
    /// </summary>
    /// <remarks>默认值为：<c>false</c>。</remarks>
    public bool AllowStringValues
    {
        get;
        set
        {
            field = value;
            _validator.AllowStringValues = value;
        }
    }

    /// <inheritdoc />
    public override bool IsValid(object? value) => _validator.IsValid(value);

    /// <inheritdoc />
    public override string FormatErrorMessage(string name) =>
        string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, Precision, Scale);

    /// <summary>
    ///     获取错误信息对应的资源键
    /// </summary>
    /// <returns>
    ///     <see cref="string" />
    /// </returns>
    internal string GetResourceKey() =>
        AllowNegative
            ? nameof(ValidationMessages.DecimalValidator_ValidationError_AllowNegative)
            : nameof(ValidationMessages.DecimalValidator_ValidationError);
}