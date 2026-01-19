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
using System.Diagnostics.CodeAnalysis;

namespace Furion.Validation;

/// <summary>
///     自定义验证特性验证器
/// </summary>
public class CustomValidationValidator : ValidatorBase, IDisposable
{
    /// <summary>
    ///     需要监听属性变更的属性名集合
    /// </summary>
    internal readonly string[] _observedPropertyNames =
    [
        nameof(ErrorMessage), nameof(ErrorMessageResourceType), nameof(ErrorMessageResourceName)
    ];

    /// <summary>
    ///     <inheritdoc cref="AttributeValueValidator" />
    /// </summary>
    internal readonly AttributeValueValidator _validator;

    /// <summary>
    ///     <inheritdoc cref="CustomValidationValidator" />
    /// </summary>
    /// <param name="validatorType">执行自定义验证的类型</param>
    /// <param name="method">验证方法</param>
    public CustomValidationValidator(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        Type validatorType, string method)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(validatorType);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        ValidatorType = validatorType;
        Method = method;

        // 订阅属性变更事件
        PropertyChanged += OnPropertyChanged;

        _validator = new AttributeValueValidator(new CustomValidationAttribute(validatorType, method));

        UseResourceKey(() => nameof(ValidationMessages.CustomValidationValidator_ValidationError));
    }

    /// <summary>
    ///     执行自定义验证的类型
    /// </summary>
    public Type ValidatorType { get; }

    /// <summary>
    ///     验证方法
    /// </summary>
    public string Method { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public override bool IsValid(object? value, IValidationContext? validationContext) =>
        _validator.IsValid(value, validationContext);

    /// <inheritdoc />
    public override List<ValidationResult>?
        GetValidationResults(object? value, IValidationContext? validationContext) =>
        _validator.GetValidationResults(value, validationContext);

    /// <inheritdoc />
    public override void Validate(object? value, IValidationContext? validationContext) =>
        _validator.Validate(value, validationContext);

    /// <inheritdoc />
    public override string? FormatErrorMessage(string name) => _validator.FormatErrorMessage(name);

    /// <summary>
    ///     释放资源
    /// </summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // 移除属性变更事件
            PropertyChanged -= OnPropertyChanged;
        }
    }

    /// <summary>
    ///     订阅属性变更事件
    /// </summary>
    /// <param name="sender">事件源</param>
    /// <param name="eventArgs">
    ///     <see cref="ValidationPropertyChangedEventArgs" />
    /// </param>
    internal void OnPropertyChanged(object? sender, ValidationPropertyChangedEventArgs eventArgs)
    {
        // 检查是否是需要同步的属性名
        if (!_observedPropertyNames.Contains(eventArgs.PropertyName))
        {
            return;
        }

        // 应用属性变更到 CustomValidationAttribute 对应的属性中
        typeof(CustomValidationAttribute).GetProperty(eventArgs.PropertyName!)
            ?.SetValue(_validator.Attributes[0], eventArgs.PropertyValue);
    }
}