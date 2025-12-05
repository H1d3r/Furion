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
using System.Linq.Expressions;

namespace Furion.Validation;

/// <summary>
///     属性验证器
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
/// <typeparam name="TProperty">属性类型</typeparam>
public sealed partial class
    PropertyValidator<T, TProperty> : FluentValidatorBuilder<TProperty, PropertyValidator<T, TProperty>>,
    IObjectValidator<T>
    where T : class
{
    /// <inheritdoc cref="PropertyAnnotationValidator{T,TProperty}" />
    internal readonly PropertyAnnotationValidator<T, TProperty> _annotationValidator;

    /// <inheritdoc cref="ObjectValidator{T}" />
    internal readonly ObjectValidator<T> _objectValidator;

    /// <inheritdoc cref="IObjectValidator{T}" />
    internal IObjectValidator<TProperty>? _propertyValidator;

    /// <summary>
    ///     <inheritdoc cref="PropertyValidator{T,TProperty}" />
    /// </summary>
    /// <param name="selector">属性选择器</param>
    /// <param name="objectValidator">
    ///     <see cref="ObjectValidator{T}" />
    /// </param>
    internal PropertyValidator(Expression<Func<T, TProperty?>> selector, ObjectValidator<T> objectValidator)
        : base(null, (objectValidator ?? throw new ArgumentNullException(nameof(objectValidator)))._items)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(objectValidator);

        _objectValidator = objectValidator;

        // 初始化 PropertyAnnotationValidator 实例
        _annotationValidator = new PropertyAnnotationValidator<T, TProperty>(selector, null, objectValidator._items);

        // 同步 IServiceProvider 委托
        InitializeServiceProvider(objectValidator._serviceProvider);
    }

    /// <summary>
    ///     规则集列表
    /// </summary>
    internal string?[]? RuleSets { get; init; }

    /// <summary>
    ///     是否禁用属性验证特性验证
    /// </summary>
    /// <remarks>
    ///     默认值为：<c>null</c>。当此值为 <c>null</c> 时，是否启用注解验证将由 <see cref="ObjectValidator{T}.Options" /> 中的
    ///     <see cref="ValidatorOptions.SuppressAnnotationValidation" /> 配置项决定。
    /// </remarks>
    internal bool? SuppressAnnotationValidation { get; private set; }

    /// <summary>
    ///     显示名称
    /// </summary>
    internal string? DisplayName { get; private set; }

    /// <summary>
    ///     验证条件
    /// </summary>
    /// <remarks>当条件满足时才进行验证。</remarks>
    internal Func<T, TProperty?, bool>? WhenCondition { get; private set; }

    /// <summary>
    ///     逆向验证条件
    /// </summary>
    /// <remarks>当条件不满足时才进行验证。</remarks>
    internal Func<T, TProperty?, bool>? UnlessCondition { get; private set; }

    /// <inheritdoc />
    public bool IsValid(T? instance, params string?[]? ruleSets)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);

        // 检查是否应该对该属性执行验证
        if (!ShouldValidate(instance, ruleSets))
        {
            return true;
        }

        // 检查是否启用属性验证特性验证
        if (ShouldRunAnnotationValidation() && !_annotationValidator.IsValid(instance))
        {
            return false;
        }

        // 获取属性值
        var propertyValue = GetValue(instance);

        // 检查是否设置了属性级别的对象验证器
        if (propertyValue is not null && _propertyValidator is not null &&
            !_propertyValidator.IsValid(propertyValue, ruleSets))
        {
            return false;
        }

        return Validators.All(u => u.IsValid(GetValidationValue(instance, u, propertyValue)));
    }

    /// <inheritdoc />
    public List<ValidationResult>? GetValidationResults(T? instance, params string?[]? ruleSets)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);

        // 检查是否应该对该属性执行验证
        if (!ShouldValidate(instance, ruleSets))
        {
            return null;
        }

        // 获取显示名称和初始化验证结果集合
        var displayName = GetDisplayName();
        var validationResults = new List<ValidationResult>();

        // 检查是否启用属性验证特性验证
        if (ShouldRunAnnotationValidation())
        {
            validationResults.AddRange(_annotationValidator.GetValidationResults(instance, displayName) ?? []);
        }

        // 获取属性值
        var propertyValue = GetValue(instance);

        // 检查是否设置了属性级别的对象验证器
        if (propertyValue is not null && _propertyValidator is not null)
        {
            validationResults.AddRange(_propertyValidator.GetValidationResults(propertyValue, ruleSets) ?? []);
        }

        // 获取所有验证器验证结果集合
        validationResults.AddRange(Validators.SelectMany(u =>
            u.GetValidationResults(GetValidationValue(instance, u, propertyValue), displayName) ?? []));

        return validationResults.ToResults();
    }

    /// <inheritdoc />
    public void Validate(T? instance, params string?[]? ruleSets)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);

        // 检查是否应该对该属性执行验证
        if (!ShouldValidate(instance, ruleSets))
        {
            return;
        }

        // 获取显示名称
        var displayName = GetDisplayName();

        // 检查是否启用属性验证特性验证
        if (ShouldRunAnnotationValidation())
        {
            _annotationValidator.Validate(instance, displayName);
        }

        // 获取属性值
        var propertyValue = GetValue(instance);

        // 检查是否设置了属性级别的对象验证器
        if (propertyValue is not null && _propertyValidator is not null)
        {
            _propertyValidator.Validate(propertyValue, ruleSets);
        }

        // 遍历验证器集合
        foreach (var validator in Validators)
        {
            validator.Validate(GetValidationValue(instance, validator, propertyValue), displayName);
        }
    }

    /// <inheritdoc />
    public override void InitializeServiceProvider(Func<Type, object?>? serviceProvider)
    {
        // 同步基类 IServiceProvider 委托
        base.InitializeServiceProvider(serviceProvider);

        // 同步 _annotationValidator 实例 IServiceProvider 委托
        _annotationValidator.InitializeServiceProvider(serviceProvider);
    }

    /// <summary>
    ///     设置对象验证器
    /// </summary>
    /// <remarks>属性级别的对象验证器。</remarks>
    /// <param name="validator">
    ///     <see cref="ValidatorBase" />
    /// </param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> SetValidator(IObjectValidator<TProperty>? validator)
    {
        _propertyValidator = validator;

        // 同步 IServiceProvider 委托
        _propertyValidator?.InitializeServiceProvider(_serviceProvider);

        return this;
    }

    /// <summary>
    ///     设置验证条件
    /// </summary>
    /// <remarks>当条件满足时才验证。</remarks>
    /// <param name="condition">条件委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> When(Func<TProperty?, bool> condition)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(condition);

        WhenCondition = (_, p) => condition(p);

        return this;
    }

    /// <summary>
    ///     设置逆向验证条件
    /// </summary>
    /// <remarks>当条件不满足时才验证。</remarks>
    /// <param name="condition">条件委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Unless(Func<TProperty?, bool> condition)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(condition);

        UnlessCondition = (_, p) => condition(p);

        return this;
    }

    /// <summary>
    ///     设置验证条件
    /// </summary>
    /// <remarks>当条件满足时才验证。</remarks>
    /// <param name="condition">条件委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> When(Func<T, TProperty?, bool> condition)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(condition);

        WhenCondition = condition;

        return this;
    }

    /// <summary>
    ///     设置逆向验证条件
    /// </summary>
    /// <remarks>当条件不满足时才验证。</remarks>
    /// <param name="condition">条件委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Unless(Func<T, TProperty?, bool> condition)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(condition);

        UnlessCondition = condition;

        return this;
    }

    /// <summary>
    ///     设置显示名称
    /// </summary>
    /// <param name="displayName">显示名称</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> WithDisplayName(string? displayName)
    {
        DisplayName = displayName;

        return this;
    }

    /// <summary>
    ///     检查是否应该对该属性执行验证
    /// </summary>
    /// <param name="instance">对象</param>
    /// <param name="ruleSets">规则集列表</param>
    /// <returns>
    ///     <see cref="bool" />
    /// </returns>
    internal bool ShouldValidate(T instance, params string?[]? ruleSets)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);

        // 检查传入的规则集列表是否与指定的规则集列表匹配
        if (!MatchesRuleSet(ruleSets))
        {
            return false;
        }

        // 检查正向条件（When）
        if (WhenCondition is not null && !WhenCondition(instance, GetValue(instance)))
        {
            return false;
        }

        // 检查逆向条件（Unless）
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (UnlessCondition is not null && UnlessCondition(instance, GetValue(instance)))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///     检查是否启用属性验证特性验证
    /// </summary>
    /// <returns>
    ///     <see cref="bool" />
    /// </returns>
    internal bool ShouldRunAnnotationValidation()
    {
        // 属性级配置优先
        if (SuppressAnnotationValidation.HasValue)
        {
            return !SuppressAnnotationValidation.Value;
        }

        return !_objectValidator.Options.SuppressAnnotationValidation;
    }

    /// <summary>
    ///     检查传入的规则集列表是否与指定的规则集列表匹配
    /// </summary>
    /// <param name="ruleSets">规则集列表</param>
    /// <returns>
    ///     <see cref="bool" />
    /// </returns>
    internal bool MatchesRuleSet(params string?[]? ruleSets)
    {
        // 规范化规则集列表
        var current = (RuleSets ?? []).Select(NormalizeRuleSet).ToArray();
        var input = (ruleSets ?? []).Select(NormalizeRuleSet).ToArray();

        // 当前实例未定义规则集列表时
        if (current is { Length: 0 })
        {
            return input is { Length: 0 } || input.Contains("*") || input.Contains(null);
        }

        // 当前实例有规则集列表但无传入规则集列表时
        if (input is { Length: 0 })
        {
            return current.Contains("*") || current.Contains(null);
        }

        // 当双方均有规则集列表时
        return input.Contains("*") || current.Contains("*") || current.Intersect(input).Any();

        // 规范化规则集：去除前后空格
        static string? NormalizeRuleSet(string? ruleSet) => ruleSet?.Trim();
    }

    /// <summary>
    ///     获取用于验证的值
    /// </summary>
    /// <param name="instance">对象</param>
    /// <param name="validator">
    ///     <see cref="ValidatorBase" />
    /// </param>
    /// <param name="propertyValue">属性值</param>
    /// <returns>
    ///     <see cref="bool" />
    /// </returns>
    internal static object? GetValidationValue(T instance, ValidatorBase validator, TProperty? propertyValue)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(validator);

        // 获取验证器类型
        var validatorType = validator.GetType();

        // 检查验证器类型是否是验证器代理类型
        if (validatorType.IsGenericType && validatorType.GetGenericTypeDefinition() == typeof(ValidatorProxy<,>))
        {
            return instance;
        }

        return propertyValue;
    }

    /// <summary>
    ///     获取属性值
    /// </summary>
    /// <param name="instance">对象</param>
    /// <returns>
    ///     <typeparamref name="TProperty" />
    /// </returns>
    internal TProperty? GetValue(T instance) => _annotationValidator.GetValue(instance);

    /// <summary>
    ///     获取显示名称
    /// </summary>
    /// <returns>
    ///     <see cref="string" />
    /// </returns>
    internal string GetDisplayName() => _annotationValidator.GetDisplayName(DisplayName);

    /// <summary>
    ///     创建 <see cref="ValidationContext{T}" /> 实例
    /// </summary>
    /// <param name="value">对象</param>
    /// <returns>
    ///     <see cref="ValidationContext{T}" />
    /// </returns>
    internal ValidationContext<T> CreateValidationContext(T value)
    {
        // 初始化 ValidationContext 实例
        var validationContext = new ValidationContext<T>(value, null, _items?.AsReadOnly());

        // 同步 IServiceProvider 委托
        validationContext.InitializeServiceProvider(_serviceProvider);

        return validationContext;
    }
}