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

using Furion.Validation.Resources;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Furion.Validation;

/// <summary>
///     文件拓展名验证器
/// </summary>
public class FileExtensionsValidator : ValidatorBase
{
    /// <summary>
    ///     <inheritdoc cref="AttributeValueValidator" />
    /// </summary>
    internal readonly AttributeValueValidator _validator;

    /// <summary>
    ///     <inheritdoc cref="FileExtensionsValidator" />
    /// </summary>
    /// <param name="extensions">文件拓展名</param>
    public FileExtensionsValidator(string extensions)
    {
        // 空检查
        ArgumentException.ThrowIfNullOrWhiteSpace(extensions);

        Extensions = extensions;

        _validator = new AttributeValueValidator(new FileExtensionsAttribute { Extensions = extensions });

        UseResourceKey(() => nameof(ValidationMessages.FileExtensionsValidator_ValidationError));
    }

    /// <summary>
    ///     文件拓展名
    /// </summary>
    public string Extensions { get; }

    /// <summary>
    ///     格式化后的文件拓展名列表
    /// </summary>
    internal string ExtensionsFormatted => ExtensionsParsed.Aggregate((left, right) => left + ", " + right);

    /// <summary>
    ///     标准化后的文件拓展名字符串
    /// </summary>
    internal string ExtensionsNormalized =>
        Extensions.Replace(" ", string.Empty).Replace(".", string.Empty).ToLowerInvariant();

    /// <summary>
    ///     解析后的文件拓展名集合
    /// </summary>
    internal IEnumerable<string> ExtensionsParsed => ExtensionsNormalized.Split(',').Select(e => "." + e);

    /// <inheritdoc />
    public override bool IsValid(object? value, IValidationContext? validationContext) =>
        _validator.IsValid(value, validationContext);

    /// <inheritdoc />
    public override string FormatErrorMessage(string name) =>
        string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, ExtensionsFormatted);
}