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

using Furion.Extensions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace Furion.Validation;

/// <summary>
///     属性验证特性验证器
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
/// <typeparam name="TProperty">属性类型</typeparam>
public class PropertyAnnotationValidator<T, TProperty> : PropertyAnnotationValidator<T>
{
    /// <summary>
    ///     <inheritdoc cref="PropertyAnnotationValidator{T,TProperty}" />
    /// </summary>
    /// <param name="selector">属性选择器</param>
    public PropertyAnnotationValidator(Expression<Func<T, TProperty>> selector)
        : base(ConvertExpression(selector))
    {
    }

    /// <summary>
    ///     <inheritdoc cref="PropertyAnnotationValidator{T,TProperty}" />
    /// </summary>
    /// <param name="selector">属性选择器</param>
    /// <param name="items">验证上下文数据</param>
    public PropertyAnnotationValidator(Expression<Func<T, TProperty>> selector, IDictionary<object, object?>? items)
        : base(ConvertExpression(selector), items)
    {
    }

    /// <summary>
    ///     <inheritdoc cref="PropertyAnnotationValidator{T,TProperty}" />
    /// </summary>
    /// <param name="selector">属性选择器</param>
    /// <param name="serviceProvider">
    ///     <see cref="IServiceProvider" />
    /// </param>
    /// <param name="items">验证上下文数据</param>
    public PropertyAnnotationValidator(Expression<Func<T, TProperty>> selector, IServiceProvider? serviceProvider,
        IDictionary<object, object?>? items)
        : base(ConvertExpression(selector), serviceProvider, items)
    {
    }

    /// <summary>
    ///     将属性表达式转换为 <![CDATA[Func<T, object?>]]> 类型
    /// </summary>
    /// <param name="selector">属性选择器</param>
    /// <returns>
    ///     <see cref="Expression{TDelegate}" />
    /// </returns>
    internal static Expression<Func<T, object?>> ConvertExpression(Expression<Func<T, TProperty>> selector)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(selector);

        return Expression.Lambda<Func<T, object?>>(Expression.Convert(selector.Body, typeof(object)),
            selector.Parameters);
    }

    /// <summary>
    ///     获取属性值
    /// </summary>
    /// <param name="instance">对象</param>
    /// <returns>
    ///     <typeparamref name="TProperty" />
    /// </returns>
    public new TProperty GetValue(T instance)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);

        return (TProperty)base.GetValue(instance)!;
    }
}

/// <summary>
///     属性验证特性验证器
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
public class PropertyAnnotationValidator<T> : ValidatorBase<T>, IValidatorInitializer
{
    /// <summary>
    ///     属性值访问器
    /// </summary>
    internal readonly Func<T, object?> _getter;

    /// <summary>
    ///     验证上下文数据
    /// </summary>
    internal readonly IDictionary<object, object?>? _items;

    /// <summary>
    ///     <see cref="IServiceProvider" /> 委托
    /// </summary>
    internal Func<Type, object?>? _serviceProvider;

    /// <summary>
    ///     <inheritdoc cref="PropertyAnnotationValidator{T}" />
    /// </summary>
    /// <param name="selector">属性选择器</param>
    public PropertyAnnotationValidator(Expression<Func<T, object?>> selector)
        : this(selector, null, null)
    {
    }

    /// <summary>
    ///     <inheritdoc cref="PropertyAnnotationValidator{T}" />
    /// </summary>
    /// <param name="selector">属性选择器</param>
    /// <param name="items">验证上下文数据</param>
    public PropertyAnnotationValidator(Expression<Func<T, object?>> selector, IDictionary<object, object?>? items)
        : this(selector, null, items)
    {
    }

    /// <summary>
    ///     <inheritdoc cref="PropertyAnnotationValidator{T}" />
    /// </summary>
    /// <param name="selector">属性选择器</param>
    /// <param name="serviceProvider">
    ///     <see cref="IServiceProvider" />
    /// </param>
    /// <param name="items">验证上下文数据</param>
    public PropertyAnnotationValidator(Expression<Func<T, object?>> selector, IServiceProvider? serviceProvider,
        IDictionary<object, object?>? items)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(selector);

        Property = selector.GetProperty();
        _getter = selector.Compile();

        // 空检查
        if (serviceProvider is not null)
        {
            _serviceProvider = serviceProvider.GetService;
        }

        _items = items;
        ErrorMessageResourceAccessor = () => null!;
    }

    /// <summary>
    ///     <inheritdoc cref="PropertyInfo" />
    /// </summary>
    public PropertyInfo Property { get; }

    /// <inheritdoc />
    void IValidatorInitializer.InitializeServiceProvider(Func<Type, object?>? serviceProvider) =>
        InitializeServiceProvider(serviceProvider);

    /// <inheritdoc />
    public override bool IsValid(T? instance)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);

        return Validator.TryValidateProperty(GetValue(instance), CreateValidationContext(instance, null), null);
    }

    /// <inheritdoc />
    public override List<ValidationResult>? GetValidationResults(T? instance, string name,
        IEnumerable<string>? memberNames = null)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);

        // 获取显示名称
        var displayName = GetDisplayName(name);

        // 初始化验证结果集合
        var validationResults = new List<ValidationResult>();

        Validator.TryValidateProperty(GetValue(instance), CreateValidationContext(instance, displayName),
            validationResults);

        // 如果验证未通过且配置了自定义错误信息，则在首部添加自定义错误信息
        if (validationResults.Count > 0 && (string?)ErrorMessageString is not null)
        {
            validationResults.Insert(0,
                new ValidationResult(FormatErrorMessage(displayName), memberNames ?? [Property.Name]));
        }

        return validationResults.ToResults();
    }

    /// <inheritdoc />
    public override void Validate(T? instance, string name, IEnumerable<string>? memberNames = null)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);

        // 获取显示名称
        var displayName = GetDisplayName(name);

        try
        {
            Validator.ValidateProperty(GetValue(instance), CreateValidationContext(instance, displayName));
        }
        // 如果验证未通过且配置了自定义错误信息，则重新抛出异常
        catch (ValidationException e) when (ErrorMessageString is not null)
        {
            throw new ValidationException(
                new ValidationResult(FormatErrorMessage(displayName), memberNames ?? e.ValidationResult.MemberNames),
                e.ValidationAttribute, e.Value) { Source = e.Source };
        }
    }

    /// <inheritdoc />
    public override string? FormatErrorMessage(string name) =>
        (string?)ErrorMessageString is null ? null : base.FormatErrorMessage(name);

    /// <summary>
    ///     获取属性值
    /// </summary>
    /// <param name="instance">对象</param>
    /// <returns>
    ///     <see cref="object" />
    /// </returns>
    public object? GetValue(T instance)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);

        return _getter(instance);
    }

    /// <summary>
    ///     获取显示名称
    /// </summary>
    /// <param name="name">显示名称</param>
    /// <returns>
    ///     <see cref="string" />
    /// </returns>
    public string GetDisplayName(string? name) =>
        name ?? Property.GetCustomAttribute<DisplayAttribute>(false)?.GetName() ??
        Property.GetCustomAttribute<DisplayNameAttribute>(false)?.DisplayName ?? Property.Name;

    /// <summary>
    ///     创建 <see cref="ValidationContext" /> 实例
    /// </summary>
    /// <param name="value">对象</param>
    /// <param name="name">显示名称</param>
    /// <returns>
    ///     <see cref="ValidationContext" />
    /// </returns>
    internal ValidationContext CreateValidationContext(object value, string? name)
    {
        // 初始化 ValidationContext 实例
        var validationContext = new ValidationContext(value, null, _items) { MemberName = Property.Name };

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