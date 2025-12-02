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

namespace Furion.Validation;

/// <summary>
///     链式条件构建器
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
public class FluentConditionBuilder<T> : FluentValidatorBuilder<T, FluentConditionBuilder<T>>;

/// <summary>
///     条件构建器
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
public class ConditionBuilder<T>
{
    /// <summary>
    ///     条件和对应的验证器列表
    /// </summary>
    internal readonly List<(Func<T, bool> Condition, IReadOnlyList<ValidatorBase> Validators)> _conditions;

    /// <summary>
    ///     缺省验证器集合
    /// </summary>
    internal IReadOnlyList<ValidatorBase>? _defaultValidators;

    /// <summary>
    ///     <inheritdoc cref="ConditionBuilder{T}" />
    /// </summary>
    internal ConditionBuilder() => _conditions = [];

    /// <summary>
    ///     添加条件和对应的验证器集合
    /// </summary>
    /// <param name="condition">条件委托</param>
    /// <param name="configure">自定义配置委托</param>
    /// <returns>
    ///     <see cref="ConditionBuilder{T}" />
    /// </returns>
    public ConditionBuilder<T> AddCondition(Func<T, bool> condition, Action<FluentConditionBuilder<T>> configure)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(condition);

        _conditions.Add((condition, BuildValidators(configure)));

        return this;
    }

    /// <summary>
    ///     添加条件成立时对应的验证器集合
    /// </summary>
    /// <param name="condition">条件委托</param>
    /// <param name="configure">自定义配置委托</param>
    /// <returns>
    ///     <see cref="ConditionBuilder{T}" />
    /// </returns>
    public ConditionBuilder<T> When(Func<T, bool> condition, Action<FluentConditionBuilder<T>> configure) =>
        AddCondition(condition, configure);

    /// <summary>
    ///     添加条件不成立时对应的验证器集合
    /// </summary>
    /// <param name="condition">条件委托</param>
    /// <param name="configure">自定义配置委托</param>
    /// <returns>
    ///     <see cref="ConditionBuilder{T}" />
    /// </returns>
    public ConditionBuilder<T> Unless(Func<T, bool> condition, Action<FluentConditionBuilder<T>> configure)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(condition);

        return AddCondition(u => !condition(u), configure);
    }

    /// <summary>
    ///     设置默认验证器集合
    /// </summary>
    /// <remarks>当没有条件匹配时使用。</remarks>
    /// <param name="configure">自定义配置委托</param>
    /// <returns>
    ///     <see cref="ConditionBuilder{T}" />
    /// </returns>
    public ConditionBuilder<T> Otherwise(Action<FluentConditionBuilder<T>> configure)
    {
        _defaultValidators = BuildValidators(configure);

        return this;
    }

    /// <summary>
    ///     构建验证器集合
    /// </summary>
    /// <param name="configure">自定义配置委托</param>
    /// <returns>
    ///     <see cref="IReadOnlyList{T}" />
    /// </returns>
    internal static IReadOnlyList<ValidatorBase> BuildValidators(Action<FluentConditionBuilder<T>> configure)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(configure);

        // 初始化 FluentConditionBuilder<T> 实例
        var fluentConditionBuilder = new FluentConditionBuilder<T>();

        // 调用自定义配置委托
        configure.Invoke(fluentConditionBuilder);

        return fluentConditionBuilder.Build();
    }

    /// <summary>
    ///     构建条件和默认验证器集合
    /// </summary>
    /// <returns>
    ///     <see cref="Tuple{T1,T2}" />
    /// </returns>
    internal (List<(Func<T, bool> Condition, IReadOnlyList<ValidatorBase> Validators)> Conditions,
        IReadOnlyList<ValidatorBase>? DefaultValidators) Build() => (_conditions, _defaultValidators);
}