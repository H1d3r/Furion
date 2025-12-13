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
///     条件验证器
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
public class ConditionalValidator<T> : ValidatorBase<T>, IValidatorInitializer, IDisposable
{
    /// <summary>
    ///     最终的条件与默认验证器集合
    /// </summary>
    internal readonly List<(Func<T, bool> Condition, IReadOnlyList<ValidatorBase> Validators)> _conditions;

    /// <summary>
    ///     默认验证器集合
    /// </summary>
    /// <remarks>当无条件匹配时使用。</remarks>
    internal IReadOnlyList<ValidatorBase>? _defaultValidators;

    /// <summary>
    ///     <inheritdoc cref="ConditionalValidator{T}" />
    /// </summary>
    /// <param name="buildConditions">条件验证构建器配置委托</param>
    public ConditionalValidator(Action<ConditionBuilder<T>> buildConditions)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(buildConditions);

        // 初始化 ConditionBuilder<T> 实例
        var conditionBuilder = new ConditionBuilder<T>();

        // 调用条件验证构建器配置委托
        buildConditions(conditionBuilder);

        // 构建条件和默认验证器集合
        (_conditions, _defaultValidators) = conditionBuilder.Build();

        ErrorMessageResourceAccessor = () => null!;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    void IValidatorInitializer.InitializeServiceProvider(Func<Type, object?>? serviceProvider) =>
        InitializeServiceProvider(serviceProvider);

    /// <inheritdoc />
    public override bool IsValid(T? instance)
    {
        // 获取匹配到的验证器集合
        var matchedValidators = GetMatchedValidators(instance);

        return matchedValidators is null or { Count: 0 } || matchedValidators.All(u => u.IsValid(instance));
    }

    /// <inheritdoc />
    public override List<ValidationResult>? GetValidationResults(T? instance, string name,
        IEnumerable<string>? memberNames = null)
    {
        // 获取匹配到的验证器集合和成员名称列表
        var matchedValidators = GetMatchedValidators(instance);

        // 空检查
        if (matchedValidators is null or { Count: 0 })
        {
            return null;
        }

        // 获取成员名称列表
        var memberNameList = memberNames?.ToList();

        // 获取验证结果集合
        var validationResults = matchedValidators
            .SelectMany(u => u.GetValidationResults(instance, name, memberNameList) ?? []).ToList();

        // 如果验证未通过且配置了自定义错误信息，则在首部添加自定义错误信息
        if (validationResults.Count > 0 && (string?)ErrorMessageString is not null)
        {
            validationResults.Insert(0, new ValidationResult(FormatErrorMessage(name), memberNameList));
        }

        return validationResults.ToResults();
    }

    /// <inheritdoc />
    public override void Validate(T? instance, string name, IEnumerable<string>? memberNames = null)
    {
        // 获取匹配到的验证器集合
        var matchedValidators = GetMatchedValidators(instance);

        // 空检查
        if (matchedValidators is null or { Count: 0 })
        {
            return;
        }

        // 获取成员名称列表
        var memberNameList = memberNames?.ToList();

        // 遍历验证器集合
        foreach (var validator in matchedValidators)
        {
            // 检查对象合法性
            if (!validator.IsValid(instance))
            {
                ThrowValidationException(instance, name, validator, memberNameList);
            }
        }
    }

    /// <inheritdoc />
    public override string? FormatErrorMessage(string name) =>
        (string?)ErrorMessageString is null ? null : base.FormatErrorMessage(name);

    /// <summary>
    ///     抛出验证异常
    /// </summary>
    /// <param name="value">对象</param>
    /// <param name="name">显示名称</param>
    /// <param name="validator">
    ///     <see cref="ValidatorBase" />
    /// </param>
    /// <param name="memberNames">成员名称列表</param>
    /// <exception cref="ValidationException"></exception>
    internal void ThrowValidationException(object? value, string name, ValidatorBase validator,
        IEnumerable<string>? memberNames = null)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(validator);

        // 检查是否配置了自定义错误信息
        if ((string?)ErrorMessageString is null)
        {
            validator.Validate(value, name, memberNames);
        }
        else
        {
            throw new ValidationException(new ValidationResult(FormatErrorMessage(name), memberNames), null,
                value);
        }
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        // 释放所有验证器资源
        foreach (var validator in (_defaultValidators ?? []).Concat(_conditions.SelectMany(u => u.Validators)))
        {
            if (validator is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <summary>
    ///     获取匹配到的验证器集合
    /// </summary>
    /// <param name="instance">对象</param>
    /// <returns>
    ///     <see cref="IReadOnlyList{T}" />
    /// </returns>
    internal IReadOnlyList<ValidatorBase>? GetMatchedValidators(T? instance)
    {
        // 初始化匹配到的验证器集合
        IReadOnlyList<ValidatorBase>? matchedValidators = null;

        // 遍历并查找第一个条件匹配的验证器集合
        foreach (var (condition, validators) in _conditions)
        {
            // ReSharper disable once InvertIf
            if (condition(instance!))
            {
                matchedValidators = validators;
                break;
            }
        }

        // 没有匹配条件时使用默认验证器集合
        matchedValidators ??= _defaultValidators;

        return matchedValidators;
    }

    /// <inheritdoc cref="IValidatorInitializer.InitializeServiceProvider" />
    internal void InitializeServiceProvider(Func<Type, object?>? serviceProvider)
    {
        // 遍历所有验证器并尝试同步 IServiceProvider 委托
        foreach (var validator in (_defaultValidators ?? []).Concat(_conditions.SelectMany(u => u.Validators)))
        {
            // 检查验证器是否实现 IValidatorInitializer 接口
            if (validator is IValidatorInitializer initializer)
            {
                // 同步 IServiceProvider 委托
                initializer.InitializeServiceProvider(serviceProvider);
            }
        }
    }
}