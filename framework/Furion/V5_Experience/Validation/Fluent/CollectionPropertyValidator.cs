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
///     集合类型属性验证器
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
/// <typeparam name="TElement">元素类型</typeparam>
public sealed class CollectionPropertyValidator<T, TElement> : PropertyValidator<T, IEnumerable<TElement>?>
    where TElement : class
{
    /// <inheritdoc cref="IObjectValidator{T}" />
    /// <remarks>集合元素级别的对象验证器。</remarks>
    internal ObjectValidator<TElement>? _elementValidator;

    /// <inheritdoc />
    internal CollectionPropertyValidator(Expression<Func<T, IEnumerable<TElement>?>> selector,
        ObjectValidator<T> objectValidator) : base(selector, objectValidator)
    {
    }

    /// <summary>
    ///     过滤条件
    /// </summary>
    /// <remarks>当条件满足时才进行验证。</remarks>
    internal Func<TElement, ValidationContext<T>, bool>? WhereCondition { get; private set; }

    /// <inheritdoc />
    public override bool IsValid(T? instance, params string?[]? ruleSets)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);

        // 调用基类 IsValid 方法
        if (!base.IsValid(instance, ruleSets))
        {
            return false;
        }

        // 获取属性值
        var propertyValue = GetValue(instance);

        // 检查是否设置了集合元素级别对象验证器
        if (propertyValue is not null && _elementValidator is not null)
        {
            return GetValidatedElements(propertyValue, instance)
                .All(element => _elementValidator.IsValid(element, ruleSets));
        }

        return true;
    }

    /// <inheritdoc />
    public override List<ValidationResult>? GetValidationResults(T? instance, params string?[]? ruleSets)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);

        // 初始化验证结果集合
        var validationResults = new List<ValidationResult>();

        // 调用基类 GetValidationResults 方法
        validationResults.AddRange(base.GetValidationResults(instance, ruleSets) ?? []);

        // 获取属性值
        var propertyValue = GetValue(instance);

        // 检查是否设置了集合元素级别对象验证器
        if (propertyValue is not null && _elementValidator is not null)
        {
            validationResults.AddRange(GetValidatedElements(propertyValue, instance)
                .SelectMany(element => _elementValidator.GetValidationResults(element, ruleSets) ?? []));
        }

        return validationResults.ToResults();
    }

    /// <inheritdoc />
    public override void Validate(T? instance, params string?[]? ruleSets)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);

        // 调用基类 Validate 方法
        base.Validate(instance, ruleSets);

        // 获取属性值
        var propertyValue = GetValue(instance);

        // 检查是否设置了集合元素级别对象验证器
        if (propertyValue is null || _elementValidator is null)
        {
            return;
        }

        // 遍历验证的集合元素
        foreach (var element in GetValidatedElements(propertyValue, instance))
        {
            _elementValidator.Validate(element, ruleSets);
        }
    }

    /// <summary>
    ///     设置过滤条件
    /// </summary>
    /// <remarks>当条件满足时才验证。</remarks>
    /// <param name="condition">条件委托</param>
    /// <returns>
    ///     <see cref="CollectionPropertyValidator{T,TElement}" />
    /// </returns>
    public CollectionPropertyValidator<T, TElement> Where(Func<TElement, bool> condition)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(condition);

        WhereCondition = (e, _) => condition(e);

        return this;
    }

    /// <summary>
    ///     设置过滤条件
    /// </summary>
    /// <remarks>当条件满足时才验证。</remarks>
    /// <param name="condition">条件委托</param>
    /// <returns>
    ///     <see cref="CollectionPropertyValidator{T,TElement}" />
    /// </returns>
    public CollectionPropertyValidator<T, TElement> Where(Func<TElement, ValidationContext<T>, bool> condition)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(condition);

        WhereCondition = condition;

        return this;
    }

    /// <summary>
    ///     设置集合元素级别对象验证器
    /// </summary>
    /// <param name="validator">
    ///     <see cref="ValidatorBase" />
    /// </param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    /// <exception cref="InvalidOperationException"></exception>
    public PropertyValidator<T, IEnumerable<TElement>?> SetValidator(ObjectValidator<TElement>? validator)
    {
        // 空检查（重复调用检查）
        if (_elementValidator is not null && validator is not null)
        {
            throw new InvalidOperationException(
                $"An object validator has already been assigned to this element. Only one object validator is allowed per element. To define nested rules, use `{nameof(ChildRules)}` within a single validator.");
        }

        _elementValidator = validator;

        // 同步 IServiceProvider 委托
        _elementValidator?.InitializeServiceProvider(_serviceProvider);

        return this;
    }


    /// <summary>
    ///     为集合元素继续配置规则
    /// </summary>
    /// <param name="configure">自定义配置委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    /// <exception cref="InvalidOperationException"></exception>
    public PropertyValidator<T, IEnumerable<TElement>?> ChildRules(Action<ObjectValidator<TElement>> configure)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(configure);

        // 空检查（重复调用检查）
        if (_elementValidator is not null)
        {
            throw new InvalidOperationException(
                $"An object validator has already been assigned to this element. `{nameof(ChildRules)}` cannot be applied after `{nameof(SetValidator)}` or another `{nameof(ChildRules)}` call.");
        }

        // 初始化集合元素级别对象验证器实例
        var elementValidator = new ObjectValidator<TElement>();

        // 调用自定义配置委托
        configure(elementValidator);

        return SetValidator(elementValidator);
    }

    /// <inheritdoc />
    public override void InitializeServiceProvider(Func<Type, object?>? serviceProvider)
    {
        // 同步基类 IServiceProvider 委托
        base.InitializeServiceProvider(serviceProvider);

        // 同步 _elementValidator 实例 IServiceProvider 委托
        _elementValidator?.InitializeServiceProvider(serviceProvider);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        // 调用基类 Dispose 方法
        base.Dispose(disposing);

        // 释放集合元素级别对象验证器资源
        if (_elementValidator is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    ///     获取验证的集合元素
    /// </summary>
    /// <param name="elements">
    ///     <see cref="IEnumerable{T}" />
    /// </param>
    /// <param name="instance">对象</param>
    /// <returns>
    ///     <see cref="IEnumerable{T}" />
    /// </returns>
    internal IEnumerable<TElement> GetValidatedElements(IEnumerable<TElement> elements, T instance)
    {
        // 空检查
        if (WhereCondition is null)
        {
            return elements;
        }

        // 创建 ValidationContext<T> 实例
        var context = CreateValidationContext(instance);

        return elements.Where(element => WhereCondition(element, context));
    }
}