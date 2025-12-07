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

using System.Linq.Expressions;

namespace Furion.Validation;

/// <inheritdoc cref="PropertyValidator{T,TProperty}" />
public partial class PropertyValidator<T, TProperty>
{
    /// <summary>
    ///     配置是否启用该属性上的验证特性验证
    /// </summary>
    /// <param name="enabled">是否启用</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> UseAnnotationValidation(bool? enabled)
    {
        SuppressAnnotationValidation = !enabled;

        return this;
    }

    /// <summary>
    ///     配置启用该属性上的验证特性验证
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> UseAnnotationValidation() => UseAnnotationValidation(true);

    /// <summary>
    ///     配置跳过该属性上的验证特性验证
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> SkipAnnotationValidation() => UseAnnotationValidation(false);

    /// <summary>
    ///     配置跳过该属性上的验证特性验证
    /// </summary>
    /// <remarks>仅验证自定义规则。</remarks>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> CustomOnly() => UseAnnotationValidation(false);

    /// <summary>
    ///     添加条件验证器
    /// </summary>
    /// <param name="buildConditions">条件构建器配置委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Conditional(
        Action<ConditionBuilder<TProperty>, ValidationContext<T>> buildConditions)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(buildConditions);

        return ValidatorProxy<ConditionalValidator<TProperty>>(context =>
            [new Action<ConditionBuilder<TProperty>>(u => buildConditions(u, context))]);
    }

    /// <summary>
    ///     添加相等验证器
    /// </summary>
    /// <param name="compareValueAccessor">比较的值访问器</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> EqualTo(Func<ValidationContext<T>, object?> compareValueAccessor)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(compareValueAccessor);

        return ValidatorProxy<EqualToValidator>(context => [compareValueAccessor(context)]);
    }

    /// <summary>
    ///     添加大于等于验证器
    /// </summary>
    /// <param name="compareValueAccessor">比较的值访问器</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> GreaterThanOrEqualTo(
        Func<ValidationContext<T>, IComparable> compareValueAccessor)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(compareValueAccessor);

        return ValidatorProxy<GreaterThanOrEqualToValidator>(context => [compareValueAccessor(context)]);
    }

    /// <summary>
    ///     添加大于验证器
    /// </summary>
    /// <param name="compareValueAccessor">比较的值访问器</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> GreaterThan(Func<ValidationContext<T>, IComparable> compareValueAccessor)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(compareValueAccessor);

        return ValidatorProxy<GreaterThanValidator>(context => [compareValueAccessor(context)]);
    }

    /// <summary>
    ///     添加小于等于验证器
    /// </summary>
    /// <param name="compareValueAccessor">比较的值访问器</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> LessThanOrEqualTo(
        Func<ValidationContext<T>, IComparable> compareValueAccessor)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(compareValueAccessor);

        return ValidatorProxy<LessThanOrEqualToValidator>(context => [compareValueAccessor(context)]);
    }

    /// <summary>
    ///     添加小于验证器
    /// </summary>
    /// <param name="compareValueAccessor">比较的值访问器</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> LessThan(Func<ValidationContext<T>, IComparable> compareValueAccessor)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(compareValueAccessor);

        return ValidatorProxy<LessThanValidator>(context => [compareValueAccessor(context)]);
    }

    /// <summary>
    ///     添加自定义条件不成立时委托验证器
    /// </summary>
    /// <param name="condition">条件委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> MustUnless(Func<TProperty, ValidationContext<T>, bool> condition)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(condition);

        return ValidatorProxy<MustUnlessValidator<TProperty>>(context =>
            [new Func<TProperty, bool>(u => condition(u, context))]);
    }

    /// <summary>
    ///     添加自定义条件成立时委托验证器
    /// </summary>
    /// <param name="condition">条件委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Must(Func<TProperty, ValidationContext<T>, bool> condition)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(condition);

        return ValidatorProxy<MustValidator<TProperty>>(context =>
            [new Func<TProperty, bool>(u => condition(u, context))]);
    }

    /// <summary>
    ///     添加不相等验证器
    /// </summary>
    /// <param name="compareValueAccessor">比较的值访问器</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> NotEqualTo(Func<ValidationContext<T>, object?> compareValueAccessor)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(compareValueAccessor);

        return ValidatorProxy<NotEqualToValidator>(context => [compareValueAccessor(context)]);
    }

    /// <summary>
    ///     添加自定义条件成立时委托验证器
    /// </summary>
    /// <param name="condition">条件委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Predicate(Func<TProperty, ValidationContext<T>, bool> condition)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(condition);

        return ValidatorProxy<PredicateValidator<TProperty>>(context =>
            [new Func<TProperty, bool>(u => condition(u, context))]);
    }

    /// <summary>
    ///     添加验证器代理
    /// </summary>
    /// <param name="constructorArgsFactory"><typeparamref name="TValidator" /> 构造函数参数工厂</param>
    /// <param name="configure">配置验证器实例</param>
    /// <typeparam name="TValidator">
    ///     <see cref="ValidatorBase" />
    /// </typeparam>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> ValidatorProxy<TValidator>(
        Func<ValidationContext<T>, object?[]?>? constructorArgsFactory, Action<TValidator>? configure = null)
        where TValidator : ValidatorBase
    {
        // 初始化 ValidatorProxy<T, TValidator> 实例
        var validatorProxy = new ValidatorProxy<T, TValidator>(instance => GetValue(instance),
            constructorArgsFactory is null
                ? null
                : instance => constructorArgsFactory(CreateValidationContext(instance)));

        // 空检查
        if (configure is not null)
        {
            validatorProxy.Configure(configure);
        }

        return AddValidator(validatorProxy);
    }

    /// <summary>
    ///     结束当前属性验证器的配置，返回到对象验证器以继续链式操作
    /// </summary>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> And() => _objectValidator;

    /// <summary>
    ///     结束当前属性验证器的配置，返回到对象验证器以继续链式操作
    /// </summary>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> Then() => _objectValidator;

    /// <summary>
    ///     结束当前属性验证器的配置，返回到对象验证器以继续链式操作
    /// </summary>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> End() => _objectValidator;

    /// <summary>
    ///     为指定属性配置验证规则
    /// </summary>
    /// <param name="selector">属性选择器</param>
    /// <param name="ruleSets">规则集列表</param>
    /// <typeparam name="TOtherProperty">属性类型</typeparam>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TOtherProperty> RuleFor<TOtherProperty>(Expression<Func<T, TOtherProperty>> selector,
        params string?[]? ruleSets) => _objectValidator.RuleFor(selector, ruleSets);

    /// <summary>
    ///     为集合类型属性中的每一个元素配置验证规则
    /// </summary>
    /// <param name="selector">属性选择器</param>
    /// <param name="ruleSets">规则集列表</param>
    /// <typeparam name="TOtherElement">元素类型</typeparam>
    /// <returns>
    ///     <see cref="CollectionPropertyValidator{T,TElement}" />
    /// </returns>
    public CollectionPropertyValidator<T, TOtherElement> RuleForEach<TOtherElement>(
        Expression<Func<T, IEnumerable<TOtherElement>?>> selector, params string?[]? ruleSets)
        where TOtherElement : class =>
        _objectValidator.RuleForEach(selector, ruleSets);

    /// <summary>
    ///     在指定规则集上下文中为指定属性配置验证规则
    /// </summary>
    /// <param name="ruleSet">规则集</param>
    /// <param name="setAction">自定义配置委托</param>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> RuleSet(string? ruleSet, Action setAction) =>
        _objectValidator.RuleSet(ruleSet, setAction);

    /// <summary>
    ///     在指定规则集列表上下文中为指定属性配置验证规则
    /// </summary>
    /// <param name="ruleSets">规则集列表</param>
    /// <param name="setAction">自定义配置委托</param>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> RuleSet(string?[]? ruleSets, Action setAction) =>
        _objectValidator.RuleSet(ruleSets, setAction);

    /// <summary>
    ///     在指定规则集上下文中为指定属性配置验证规则
    /// </summary>
    /// <param name="ruleSet">规则集</param>
    /// <param name="setAction">自定义配置委托</param>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> RuleSet(string? ruleSet, Action<ObjectValidator<T>> setAction) =>
        _objectValidator.RuleSet(ruleSet, setAction);

    /// <summary>
    ///     在指定规则集列表上下文中为指定属性配置验证规则
    /// </summary>
    /// <param name="ruleSets">规则集列表</param>
    /// <param name="setAction">自定义配置委托</param>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> RuleSet(string?[]? ruleSets, Action<ObjectValidator<T>> setAction) =>
        _objectValidator.RuleSet(ruleSets, setAction);
}