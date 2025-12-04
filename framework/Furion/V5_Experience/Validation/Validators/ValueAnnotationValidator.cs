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

using System.ComponentModel.DataAnnotations;

namespace Furion.Validation;

/// <summary>
///     单个值验证特性验证器
/// </summary>
public class ValueAnnotationValidator : ValidatorBase
{
    /// <summary>
    ///     验证上下文数据
    /// </summary>
    internal readonly IDictionary<object, object?>? _items;

    /// <inheritdoc cref="IServiceProvider" />
    internal readonly IServiceProvider? _serviceProvider;

    /// <summary>
    ///     <inheritdoc cref="ValueAnnotationValidator" />
    /// </summary>
    /// <param name="attributes">验证特性列表</param>
    /// <exception cref="ArgumentException"></exception>
    public ValueAnnotationValidator(params ValidationAttribute[] attributes)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(attributes);

        // 确保数组元素不存在 null 值
        if (attributes.Any(u => (ValidationAttribute?)u is null))
        {
            // ReSharper disable once LocalizableElement
            throw new ArgumentException("Attributes cannot contain null elements.", nameof(attributes));
        }

        Attributes = attributes;
        ErrorMessageResourceAccessor = () => null!;
    }

    /// <summary>
    ///     <inheritdoc cref="ValueAnnotationValidator" />
    /// </summary>
    /// <param name="attributes">验证特性列表</param>
    /// <param name="items">验证上下文数据</param>
    public ValueAnnotationValidator(ValidationAttribute[] attributes, IDictionary<object, object?>? items)
        : this(attributes, null, items)
    {
    }

    /// <summary>
    ///     <inheritdoc cref="ValueAnnotationValidator" />
    /// </summary>
    /// <param name="attributes">验证特性列表</param>
    /// <param name="serviceProvider">
    ///     <see cref="IServiceProvider" />
    /// </param>
    /// <param name="items">验证上下文数据</param>
    public ValueAnnotationValidator(ValidationAttribute[] attributes, IServiceProvider? serviceProvider,
        IDictionary<object, object?>? items)
        : this(attributes)
    {
        _items = items;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    ///     验证特性列表
    /// </summary>
    public ValidationAttribute[] Attributes { get; }

    /// <inheritdoc />
    public override bool IsValid(object? value) =>
        Validator.TryValidateValue(value, new ValidationContext(new object(), _serviceProvider, _items), null,
            Attributes);

    /// <inheritdoc />
    public override List<ValidationResult>? GetValidationResults(object? value, string name)
    {
        // 初始化属性名称和验证结果集合
        var (memberName, validationResults) = (GetMemberName(name), new List<ValidationResult>());

        Validator.TryValidateValue(value,
            new ValidationContext(new object(), _serviceProvider, _items) { MemberName = memberName },
            validationResults, Attributes);

        // 如果验证未通过且配置了自定义错误信息，则在首部添加自定义错误信息
        if (validationResults.Count > 0 && (string?)ErrorMessageString is not null)
        {
            validationResults.Insert(0, new ValidationResult(FormatErrorMessage(memberName), [memberName]));
        }

        return validationResults.ToResults();
    }

    /// <inheritdoc />
    public override void Validate(object? value, string name)
    {
        // 获取显示名称
        var memberName = GetMemberName(name);

        try
        {
            Validator.ValidateValue(value,
                new ValidationContext(new object(), _serviceProvider, _items) { MemberName = memberName }, Attributes);
        }
        // 如果验证未通过且配置了自定义错误信息，则重新抛出异常
        catch (ValidationException e) when (ErrorMessageString is not null)
        {
            throw new ValidationException(new ValidationResult(FormatErrorMessage(memberName), [memberName]),
                e.ValidationAttribute, e.Value) { Source = e.Source };
        }
    }

    /// <inheritdoc />
    public override string? FormatErrorMessage(string name) =>
        (string?)ErrorMessageString is null ? null : base.FormatErrorMessage(GetMemberName(name));

    /// <summary>
    ///     获取显示名称
    /// </summary>
    /// <param name="name">显示名称</param>
    /// <returns>
    ///     <see cref="string" />
    /// </returns>
    internal static string GetMemberName(string name) => string.IsNullOrEmpty(name) ? "Value" : name;
}