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

using System.Diagnostics.CodeAnalysis;

namespace Furion.Validation;

/// <summary>
///     链式条件验证器构建器
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
public class FluentConditionBuilder<T> : FluentValidatorBuilder<T, FluentConditionBuilder<T>>;

/// <summary>
///     Then 条件中间构建器
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
public sealed class ConditionThenBuilder<T>
{
    /// <summary>
    ///     条件委托
    /// </summary>
    internal readonly Func<T, bool> _condition;

    /// <inheritdoc cref="ConditionBuilder{T}" />
    internal readonly ConditionBuilder<T> _parent;

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="parent">父条件构建器</param>
    /// <param name="condition">条件委托</param>
    internal ConditionThenBuilder(ConditionBuilder<T> parent, Func<T, bool> condition)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(condition);

        _parent = parent;
        _condition = condition;
    }

    /// <summary>
    ///     配置满足条件时执行的验证规则
    /// </summary>
    /// <param name="configure">验证器配置委托</param>
    /// <returns>
    ///     <see cref="ConditionBuilder{T}" />
    /// </returns>
    public ConditionBuilder<T> Then(Action<FluentConditionBuilder<T>> configure)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(configure);

        // 构建验证器集合
        var validators = ConditionBuilder<T>.BuildValidators(configure);

        // 将条件和验证器列表添加到父条件构建器
        _parent._conditions.Add((_condition, validators));

        return _parent;
    }

    /// <summary>
    ///     配置满足条件时直接返回指定的错误消息
    /// </summary>
    /// <remarks>不执行任何验证逻辑，仅用于输出错误提示。</remarks>
    /// <param name="errorMessage">错误消息</param>
    /// <returns>
    ///     <see cref="ConditionBuilder{T}" />
    /// </returns>
    public ConditionBuilder<T> ThenMessage(string? errorMessage) => ThenErrorMessage(errorMessage);

    /// <summary>
    ///     配置满足条件时直接返回指定的错误消息
    /// </summary>
    /// <remarks>不执行任何验证逻辑，仅用于输出错误提示。</remarks>
    /// <param name="errorMessage">错误消息</param>
    /// <returns>
    ///     <see cref="ConditionBuilder{T}" />
    /// </returns>
    public ConditionBuilder<T> ThenErrorMessage(string? errorMessage)
    {
        // 初始化 FailureValidator 验证器
        var validator = new FailureValidator().WithErrorMessage(errorMessage);

        // 将条件和验证器列表添加到父条件构建器
        _parent._conditions.Add((_condition, [validator]));

        return _parent;
    }

    /// <summary>
    ///     配置满足条件时直接返回指定的错误消息
    /// </summary>
    /// <remarks>不执行任何验证逻辑，仅用于输出错误提示。</remarks>
    /// <param name="resourceType">错误信息资源类型</param>
    /// <param name="resourceName">错误信息资源名称</param>
    /// <returns>
    ///     <see cref="ConditionBuilder{T}" />
    /// </returns>
    public ConditionBuilder<T> ThenMessage(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties |
                                    DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type resourceType, string resourceName) =>
        ThenErrorMessage(resourceType, resourceName);

    /// <summary>
    ///     配置满足条件时直接返回指定的错误消息
    /// </summary>
    /// <remarks>不执行任何验证逻辑，仅用于输出错误提示。</remarks>
    /// <param name="resourceType">错误信息资源类型</param>
    /// <param name="resourceName">错误信息资源名称</param>
    /// <returns>
    ///     <see cref="ConditionBuilder{T}" />
    /// </returns>
    public ConditionBuilder<T> ThenErrorMessage(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties |
                                    DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type resourceType, string resourceName)
    {
        // 初始化 FailureValidator 验证器
        var validator = new FailureValidator().WithErrorMessage(resourceType, resourceName);

        // 将条件和验证器列表添加到父条件构建器
        _parent._conditions.Add((_condition, [validator]));

        return _parent;
    }
}

/// <summary>
///     条件验证构建器
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
public class ConditionBuilder<T>
{
    /// <summary>
    ///     条件与对应验证器列表
    /// </summary>
    internal readonly List<(Func<T, bool> Condition, IReadOnlyList<ValidatorBase> Validators)> _conditions;

    /// <summary>
    ///     默认验证器集合
    /// </summary>
    /// <remarks>当无条件匹配时使用。</remarks>
    internal IReadOnlyList<ValidatorBase>? _defaultValidators;

    /// <summary>
    ///     <inheritdoc cref="ConditionBuilder{T}" />
    /// </summary>
    internal ConditionBuilder() => _conditions = [];

    /// <summary>
    ///     定义满足指定条件时执行的验证规则
    /// </summary>
    /// <param name="condition">条件委托</param>
    /// <returns>
    ///     <see cref="ConditionThenBuilder{T}" />
    /// </returns>
    public ConditionThenBuilder<T> When(Func<T, bool> condition)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(condition);

        return new ConditionThenBuilder<T>(this, condition);
    }

    /// <summary>
    ///     定义不满足指定条件时执行的验证规则
    /// </summary>
    /// <param name="condition">条件委托</param>
    /// <returns>
    ///     <see cref="ConditionThenBuilder{T}" />
    /// </returns>
    public ConditionThenBuilder<T> Unless(Func<T, bool> condition)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(condition);

        return new ConditionThenBuilder<T>(this, u => !condition(u));
    }

    /// <summary>
    ///     配置默认验证规则
    /// </summary>
    /// <remarks>当所有条件均不满足时使用。</remarks>
    /// <param name="configure">验证器配置委托</param>
    /// <returns>
    ///     <see cref="ConditionBuilder{T}" />
    /// </returns>
    public ConditionBuilder<T> Otherwise(Action<FluentConditionBuilder<T>> configure)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(configure);

        _defaultValidators = BuildValidators(configure);

        return this;
    }

    /// <summary>
    ///     配置默认错误消息
    /// </summary>
    /// <remarks>当所有条件均不满足时直接返回该消息，不执行任何验证逻辑。</remarks>
    /// <param name="errorMessage">错误消息</param>
    /// <returns>
    ///     <see cref="ConditionBuilder{T}" />
    /// </returns>
    public ConditionBuilder<T> OtherwiseMessage(string? errorMessage) => OtherwiseErrorMessage(errorMessage);

    /// <summary>
    ///     配置默认错误消息
    /// </summary>
    /// <remarks>当所有条件均不满足时直接返回该消息，不执行任何验证逻辑。</remarks>
    /// <param name="errorMessage">错误消息</param>
    /// <returns>
    ///     <see cref="ConditionBuilder{T}" />
    /// </returns>
    public ConditionBuilder<T> OtherwiseErrorMessage(string? errorMessage)
    {
        // 初始化 FailureValidator 验证器
        var validator = new FailureValidator().WithErrorMessage(errorMessage);

        _defaultValidators = [validator];

        return this;
    }

    /// <summary>
    ///     配置默认错误消息
    /// </summary>
    /// <remarks>当所有条件均不满足时直接返回该消息，不执行任何验证逻辑。</remarks>
    /// <param name="resourceType">错误信息资源类型</param>
    /// <param name="resourceName">错误信息资源名称</param>
    /// <returns>
    ///     <see cref="ConditionBuilder{T}" />
    /// </returns>
    public ConditionBuilder<T> OtherwiseMessage([DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type resourceType, string resourceName) =>
        OtherwiseErrorMessage(resourceType, resourceName);

    /// <summary>
    ///     配置默认错误消息
    /// </summary>
    /// <remarks>当所有条件均不满足时直接返回该消息，不执行任何验证逻辑。</remarks>
    /// <param name="resourceType">错误信息资源类型</param>
    /// <param name="resourceName">错误信息资源名称</param>
    /// <returns>
    ///     <see cref="ConditionBuilder{T}" />
    /// </returns>
    public ConditionBuilder<T> OtherwiseErrorMessage([DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type resourceType, string resourceName)
    {
        // 初始化 FailureValidator 验证器
        var validator = new FailureValidator().WithErrorMessage(resourceType, resourceName);

        _defaultValidators = [validator];

        return this;
    }

    /// <summary>
    ///     构建验证器集合
    /// </summary>
    /// <param name="configure">验证器配置委托</param>
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
    ///     构建最终的条件与默认验证器集合
    /// </summary>
    /// <returns>
    ///     <see cref="Tuple{T1,T2}" />
    /// </returns>
    internal (List<(Func<T, bool> Condition, IReadOnlyList<ValidatorBase> Validators)> Conditions,
        IReadOnlyList<ValidatorBase>? DefaultValidators) Build() => (_conditions, _defaultValidators);
}