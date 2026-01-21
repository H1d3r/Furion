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
///     对象验证特性验证器
/// </summary>
/// <remarks>支持使用 <c>[ValidateNever]</c> 特性来跳过对特定属性的验证，仅限于 ASP.NET Core 应用项目。</remarks>
public class AttributeObjectValidator : ValidatorBase, IValidatorInitializer
{
    /// <summary>
    ///     <see cref="IServiceProvider" /> 委托
    /// </summary>
    internal Func<Type, object?>? _serviceProvider;

    /// <summary>
    ///     <inheritdoc cref="AttributeObjectValidator" />
    /// </summary>
    public AttributeObjectValidator()
        : this(null, null)
    {
    }

    /// <summary>
    ///     <inheritdoc cref="AttributeObjectValidator" />
    /// </summary>
    /// <param name="items">共享数据</param>
    public AttributeObjectValidator(IDictionary<object, object?>? items)
        : this(null, items)
    {
    }

    /// <summary>
    ///     <inheritdoc cref="AttributeObjectValidator" />
    /// </summary>
    /// <param name="serviceProvider">
    ///     <see cref="IServiceProvider" />
    /// </param>
    /// <param name="items">共享数据</param>
    public AttributeObjectValidator(IServiceProvider? serviceProvider, IDictionary<object, object?>? items)
    {
        // 空检查
        if (serviceProvider is not null)
        {
            _serviceProvider = serviceProvider.GetService;
        }

        Items = items is not null ? new Dictionary<object, object?>(items) : new Dictionary<object, object?>();

        ErrorMessageResourceAccessor = () => null!;
    }

    /// <summary>
    ///     是否验证所有属性的验证特性
    /// </summary>
    /// <remarks>
    ///     该属性用于控制是否执行属性级别的验证逻辑，默认值为 <c>true</c>。
    ///     若设置为 <c>true</c>，则会同时验证所有属性以及 <see cref="IValidatableObject.Validate" /> 方法；
    ///     若设置为 <c>false</c>，则仅验证 <see cref="IValidatableObject.Validate" /> 方法。
    /// </remarks>
    public bool ValidateAllProperties { get; set; } = true;

    /// <summary>
    ///     共享数据
    /// </summary>
    public IDictionary<object, object?> Items { get; }

    /// <inheritdoc />
    void IValidatorInitializer.InitializeServiceProvider(Func<Type, object?>? serviceProvider) =>
        InitializeServiceProvider(serviceProvider);

    /// <inheritdoc />
    public override bool IsValid(object? value, IValidationContext? validationContext)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(value);

        return Validator.TryValidateObject(value, CreateValidationContext(value, validationContext?.RuleSets), null,
            ValidateAllProperties);
    }

    /// <inheritdoc />
    public override List<ValidationResult>? GetValidationResults(object? value, IValidationContext? validationContext)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(value);

        // 初始化验证结果列表
        var validationResults = new List<ValidationResult>();

        /*
         * 只有在所有属性级验证均失败的情况下，才会执行 IValidatableObject.Validate 方法的验证。
         * 此时，验证结果才会包含该方法返回的错误信息；否则，结果中仅包含属性级验证失败的信息。
         *
         * 参考源码：
         * https://github.com/dotnet/runtime/blob/5535e31a712343a63f5d7d796cd874e563e5ac14/src/libraries/System.ComponentModel.Annotations/src/System/ComponentModel/DataAnnotations/Validator.cs#L423-L430
         */
        Validator.TryValidateObject(value, CreateValidationContext(value, validationContext?.RuleSets),
            validationResults, ValidateAllProperties);

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
        // 空检查
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            Validator.ValidateObject(value, CreateValidationContext(value, validationContext?.RuleSets),
                ValidateAllProperties);
        }
        // 如果验证未通过且配置了自定义错误信息，则重新抛出异常
        catch (ValidationException e) when (ErrorMessageString is not null)
        {
            throw new ValidationException(
                new ValidationResult(FormatErrorMessage(validationContext?.DisplayName!),
                    validationContext?.MemberNames), e.ValidationAttribute, e.Value) { Source = e.Source };
        }
    }

    /// <inheritdoc cref="IValidatorInitializer.InitializeServiceProvider" />
    internal void InitializeServiceProvider(Func<Type, object?>? serviceProvider) => _serviceProvider = serviceProvider;

    /// <summary>
    ///     创建 <see cref="ValidationContext" /> 实例
    /// </summary>
    /// <param name="value">对象</param>
    /// <param name="ruleSets">规则集</param>
    /// <returns>
    ///     <see cref="ValidationContext" />
    /// </returns>
    internal ValidationContext CreateValidationContext(object value, string?[]? ruleSets)
    {
        // 初始化 ValidationContext 实例
        var validationContext = new ValidationContext(value, Items);

        // 空检查
        if (ruleSets is not null)
        {
            // 设置规则集
            validationContext.WithRuleSets(ruleSets);
        }

        // 同步 IServiceProvider 委托
        validationContext.InitializeServiceProvider(_serviceProvider!);

        return validationContext;
    }
}