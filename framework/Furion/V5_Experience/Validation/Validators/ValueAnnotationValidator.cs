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
public class ValueAnnotationValidator : ValidatorBase, IValidatorInitializer
{
    /// <summary>
    ///     用于值验证的 <see cref="ValidationContext" /> 占位对象
    /// </summary>
    internal static readonly object _sentinel = new();

    /// <summary>
    ///     <see cref="IServiceProvider" /> 委托
    /// </summary>
    internal Func<Type, object?>? _serviceProvider;

    /// <summary>
    ///     <inheritdoc cref="ValueAnnotationValidator" />
    /// </summary>
    /// <param name="attributes">验证特性列表</param>
    /// <exception cref="ArgumentException"></exception>
    public ValueAnnotationValidator(params ValidationAttribute[] attributes)
        : this(attributes, null, null)
    {
    }

    /// <summary>
    ///     <inheritdoc cref="ValueAnnotationValidator" />
    /// </summary>
    /// <param name="attributes">验证特性列表</param>
    /// <param name="items">共享数据</param>
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
    /// <param name="items">共享数据</param>
    public ValueAnnotationValidator(ValidationAttribute[] attributes, IServiceProvider? serviceProvider,
        IDictionary<object, object?>? items)
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

        // 空检查
        if (serviceProvider is not null)
        {
            _serviceProvider = serviceProvider.GetService;
        }

        Items = items is not null ? new Dictionary<object, object?>(items) : new Dictionary<object, object?>();

        ErrorMessageResourceAccessor = () => null!;
    }

    /// <summary>
    ///     验证特性列表
    /// </summary>
    public ValidationAttribute[] Attributes { get; }

    /// <summary>
    ///     共享数据
    /// </summary>
    public IDictionary<object, object?> Items { get; }

    /// <inheritdoc />
    void IValidatorInitializer.InitializeServiceProvider(Func<Type, object?>? serviceProvider) =>
        InitializeServiceProvider(serviceProvider);

    /// <inheritdoc />
    public override bool IsValid(object? value, IValidationContext? validationContext) =>
        Validator.TryValidateValue(value, CreateValidationContext(value, validationContext?.DisplayName), null,
            Attributes);

    /// <inheritdoc />
    public override List<ValidationResult>? GetValidationResults(object? value, IValidationContext? validationContext)
    {
        // 初始化验证结果集合和成员名称列表
        var validationResults = new List<ValidationResult>();

        Validator.TryValidateValue(value, CreateValidationContext(value, validationContext?.DisplayName),
            validationResults, Attributes);

        // 如果验证未通过且配置了自定义错误信息，则在首部添加自定义错误信息
        if (validationResults.Count > 0 && (string?)ErrorMessageString is not null)
        {
            validationResults.Insert(0,
                new ValidationResult(FormatErrorMessage(validationContext?.DisplayName!),
                    validationContext?.MemberNames));
        }

        return validationResults.ToResults();
    }

    /// <inheritdoc />
    public override void Validate(object? value, IValidationContext? validationContext)
    {
        try
        {
            Validator.ValidateValue(value, CreateValidationContext(value, validationContext?.DisplayName), Attributes);
        }
        // 如果验证未通过且配置了自定义错误信息，则重新抛出异常
        catch (ValidationException e) when (ErrorMessageString is not null)
        {
            throw new ValidationException(
                new ValidationResult(FormatErrorMessage(validationContext?.DisplayName!),
                    validationContext?.MemberNames),
                e.ValidationAttribute, e.Value) { Source = e.Source };
        }
    }

    /// <inheritdoc />
    public override string? FormatErrorMessage(string name) =>
        (string?)ErrorMessageString is null ? null : base.FormatErrorMessage(name);

    /// <summary>
    ///     创建 <see cref="ValidationContext" /> 实例
    /// </summary>
    /// <param name="value">对象</param>
    /// <param name="name">显示名称</param>
    /// <returns>
    ///     <see cref="ValidationContext" />
    /// </returns>
    internal ValidationContext CreateValidationContext(object? value, string? name)
    {
        // 初始化 ValidationContext 实例
        var validationContext = new ValidationContext(value ?? _sentinel, Items);

        // 空检查
        if (name is not null)
        {
            validationContext.DisplayName = name;
        }

        // 同步 IServiceProvider 委托
        validationContext.InitializeServiceProvider(_serviceProvider!);

        return validationContext;
    }

    /// <inheritdoc cref="IValidatorInitializer.InitializeServiceProvider" />
    internal void InitializeServiceProvider(Func<Type, object?>? serviceProvider) => _serviceProvider = serviceProvider;
}