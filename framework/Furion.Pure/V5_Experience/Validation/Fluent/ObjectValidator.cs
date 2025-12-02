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

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Furion.Validation;

/// <summary>
///     对象验证器
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
public class ObjectValidator<T> : IObjectValidator<T>, IDisposable
    where T : class
{
    /// <inheritdoc cref="ObjectAnnotationValidator" />
    internal readonly ObjectAnnotationValidator _annotationValidator;

    /// <summary>
    ///     当前规则集上下文栈
    /// </summary>
    internal readonly Stack<string?> _ruleSetStack;

    /// <inheritdoc cref="IServiceProvider" />
    internal readonly IServiceProvider? _serviceProvider;

    /// <summary>
    ///     <inheritdoc cref="ObjectValidator{T}" />
    /// </summary>
    public ObjectValidator()
        : this(new ValidatorOptions())
    {
    }

    /// <summary>
    ///     <inheritdoc cref="ObjectValidator{T}" />
    /// </summary>
    /// <param name="serviceProvider">
    ///     <see cref="IServiceProvider" />
    /// </param>
    public ObjectValidator(IServiceProvider? serviceProvider)
        : this(new ValidatorOptions(), serviceProvider)
    {
    }

    /// <summary>
    ///     <inheritdoc cref="ObjectValidator{T}" />
    /// </summary>
    /// <param name="options">
    ///     <see cref="ValidatorOptions" />
    /// </param>
    /// <param name="serviceProvider">
    ///     <see cref="IServiceProvider" />
    /// </param>
    public ObjectValidator(ValidatorOptions options, IServiceProvider? serviceProvider = null)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(options);

        Options = options;
        _serviceProvider = serviceProvider;

        // 初始化 ObjectAnnotationValidator 实例
        _annotationValidator = new ObjectAnnotationValidator(serviceProvider, null)
        {
            ValidateAllProperties = options.ValidateAllProperties
        };

        PropertyValidators = [];
        _ruleSetStack = new Stack<string?>();

        // 订阅 ValidatorOptions 属性变更事件
        Options.PropertyChanged += OptionsOnPropertyChanged;
    }

    /// <inheritdoc cref="ValidatorOptions" />
    public ValidatorOptions Options { get; }

    /// <summary>
    ///     验证条件
    /// </summary>
    /// <remarks>当条件满足时才进行验证。</remarks>
    internal Func<T, bool>? WhenCondition { get; private set; }

    /// <summary>
    ///     逆向验证条件
    /// </summary>
    /// <remarks>当条件不满足时才进行验证。</remarks>
    internal Func<T, bool>? UnlessCondition { get; private set; }

    /// <summary>
    ///     属性验证器集合
    /// </summary>
    internal List<IObjectValidator<T>> PropertyValidators { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public bool IsValid(T? instance, params string?[]? ruleSets)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);

        // 检查是否应该对该对象执行验证
        if (!ShouldValidate(instance, ruleSets))
        {
            return true;
        }

        // 检查是否启用对象属性验证特性验证
        // 此处可能存在验证特性重复执行的问题，可通过启用 SuppressAnnotationValidation 或调用 CustomOnly() 方法解决
        if (ShouldRunAnnotationValidation() && !_annotationValidator.IsValid(instance))
        {
            return false;
        }

        return PropertyValidators.All(u => u.IsValid(instance, ruleSets));
    }

    /// <inheritdoc />
    public List<ValidationResult>? GetValidationResults(T? instance, params string?[]? ruleSets)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);

        // 检查是否应该对该对象执行验证
        if (!ShouldValidate(instance, ruleSets))
        {
            return null;
        }

        // 初始化验证结果集合
        var validationResults = new List<ValidationResult>();

        // 检查是否启用对象属性验证特性验证
        // 此处可能存在验证特性重复执行的问题，可通过启用 SuppressAnnotationValidation 或调用 CustomOnly() 方法解决
        if (ShouldRunAnnotationValidation())
        {
            validationResults.AddRange(_annotationValidator.GetValidationResults(instance, null!) ?? []);
        }

        // 获取所有属性验证器验证结果集合
        validationResults.AddRange(
            PropertyValidators.SelectMany(u => u.GetValidationResults(instance, ruleSets) ?? []));

        return validationResults.ToResults();
    }

    /// <inheritdoc />
    public void Validate(T? instance, params string?[]? ruleSets)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);

        // 检查是否应该对该对象执行验证
        if (!ShouldValidate(instance, ruleSets))
        {
            return;
        }

        // 检查是否启用对象属性验证特性验证
        // 此处可能存在验证特性重复执行的问题，可通过启用 SuppressAnnotationValidation 或调用 CustomOnly() 方法解决
        if (ShouldRunAnnotationValidation())
        {
            _annotationValidator.Validate(instance, null!);
        }

        // 遍历属性验证器集合
        foreach (var validator in PropertyValidators)
        {
            validator.Validate(instance, ruleSets);
        }
    }

    /// <summary>
    ///     配置属性验证器
    /// </summary>
    /// <param name="selector">属性选择器</param>
    /// <param name="ruleSets">规则集列表</param>
    /// <typeparam name="TProperty">属性类型</typeparam>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty?>> selector,
        params string?[]? ruleSets)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(selector);

        // 优先使用规则集上下文栈栈顶的规则集，否则使用传入的规则集列表
        var effectiveRuleSets = _ruleSetStack is { Count: > 0 }
            ? [_ruleSetStack.Peek()]
            : (ruleSets ?? []).Select(u => u?.Trim()).ToArray();

        // 初始化 PropertyValidator 实例
        var propertyValidator = new PropertyValidator<T, TProperty>(selector, this) { RuleSets = effectiveRuleSets };

        // 将实例添加到集合中
        PropertyValidators.Add(propertyValidator);

        return propertyValidator;
    }

    /// <summary>
    ///     在指定规则集上下文中配置属性验证器
    /// </summary>
    /// <param name="ruleSet">规则集</param>
    /// <param name="setAction">自定义配置委托</param>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> RuleSet(string? ruleSet, Action setAction)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(setAction);

        return RuleSet(ruleSet, _ => setAction());
    }

    /// <summary>
    ///     在指定规则集列表上下文中配置属性验证器
    /// </summary>
    /// <param name="ruleSets">规则集列表</param>
    /// <param name="setAction">自定义配置委托</param>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> RuleSet(string?[]? ruleSets, Action setAction)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(setAction);

        return RuleSet(ruleSets, _ => setAction());
    }

    /// <summary>
    ///     在指定规则集上下文中配置属性验证器
    /// </summary>
    /// <param name="ruleSet">规则集</param>
    /// <param name="setAction">自定义配置委托</param>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> RuleSet(string? ruleSet, Action<ObjectValidator<T>> setAction) =>
        RuleSet(ruleSet is null ? null : [ruleSet], setAction);

    /// <summary>
    ///     在指定规则集列表上下文中配置属性验证器
    /// </summary>
    /// <param name="ruleSets">规则集列表</param>
    /// <param name="setAction">自定义配置委托</param>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> RuleSet(string?[]? ruleSets, Action<ObjectValidator<T>> setAction)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(setAction);

        // 规范化规则集列表：去除前后空格
        var normalizeRuleSet = (ruleSets ?? []).Select(u => u?.Trim()).ToArray();

        // 空检查
        if (normalizeRuleSet is { Length: 0 })
        {
            // 调用自定义配置委托
            setAction(this);

            return this;
        }

        // 为每个规则集创建独立作用域
        foreach (var ruleSet in normalizeRuleSet)
        {
            // 将当前规则集压入上下文栈，使后续的 RuleFor() 调用能感知到该规则集
            _ruleSetStack.Push(ruleSet);

            try
            {
                // 调用自定义配置委托
                setAction(this);
            }
            // 确保即使发生异常，也能正确退出当前规则集作用域
            finally
            {
                _ruleSetStack.Pop();
            }
        }

        return this;
    }

    /// <summary>
    ///     配置 <see cref="ValidatorOptions" /> 示例
    /// </summary>
    /// <param name="configure">自定义配置委托</param>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> ConfigureOptions(Action<ValidatorOptions> configure)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(configure);

        // 调用自定义配置委托
        configure(Options);

        return this;
    }

    /// <summary>
    ///     应用对象验证器配置
    /// </summary>
    /// <typeparam name="TValidatorConfigure">
    ///     <see cref="IValidatorConfigure{T}" />
    /// </typeparam>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> ConfigureWith<TValidatorConfigure>()
        where TValidatorConfigure : IValidatorConfigure<T>, new() =>
        ConfigureWith(new TValidatorConfigure());

    /// <summary>
    ///     应用对象验证器配置
    /// </summary>
    /// <param name="configure"><see cref="IValidatorConfigure{T}" /> 实例</param>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> ConfigureWith(IValidatorConfigure<T> configure)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(configure);

        // 调用配置验证规则方法
        configure.Configure(this);

        return this;
    }

    /// <summary>
    ///     应用对象验证器配置
    /// </summary>
    /// <param name="instance">对象</param>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> ConfigureWith(T instance)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);

        // 检查是否实现 IValidatorConfigure<T> 接口
        if (instance is IValidatorConfigure<T> configure)
        {
            ConfigureWith(configure);
        }

        return this;
    }

    /// <summary>
    ///     设置验证条件
    /// </summary>
    /// <remarks>当条件满足时才验证。</remarks>
    /// <param name="condition">条件委托</param>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> When(Func<T, bool> condition)
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
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> Unless(Func<T, bool> condition)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(condition);

        UnlessCondition = condition;

        return this;
    }

    /// <summary>
    ///     配置是否启用对象属性验证特性验证
    /// </summary>
    /// <param name="enabled">是否启用</param>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> UseAnnotationValidation(bool enabled)
    {
        Options.SuppressAnnotationValidation = !enabled;

        return this;
    }

    /// <summary>
    ///     配置启用对象属性验证特性验证
    /// </summary>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> UseAnnotationValidation() => UseAnnotationValidation(true);

    /// <summary>
    ///     配置跳过对象属性验证特性验证
    /// </summary>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> SkipAnnotationValidation() => UseAnnotationValidation(false);

    /// <summary>
    ///     配置跳过对象属性验证特性验证
    /// </summary>
    /// <remarks>仅验证自定义规则。</remarks>
    /// <returns>
    ///     <see cref="ObjectValidator{T}" />
    /// </returns>
    public ObjectValidator<T> CustomOnly() => UseAnnotationValidation(false);

    /// <summary>
    ///     释放资源
    /// </summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // 移除 ValidatorOptions 属性变更事件
            Options.PropertyChanged -= OptionsOnPropertyChanged;
        }
    }

    /// <summary>
    ///     检查是否应该对该对象执行验证
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

        // 检查当前规则集列表：暂无需要实现的逻辑

        // 检查正向条件（When）
        if (WhenCondition is not null && !WhenCondition(instance))
        {
            return false;
        }

        // 检查逆向条件（Unless）
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (UnlessCondition is not null && UnlessCondition(instance))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///     检查是否启用对象属性验证特性验证
    /// </summary>
    /// <returns>
    ///     <see cref="bool" />
    /// </returns>
    internal bool ShouldRunAnnotationValidation() => !Options.SuppressAnnotationValidation;

    /// <summary>
    ///     订阅 <see cref="Options" /> 属性变更事件
    /// </summary>
    /// <param name="sender">事件源</param>
    /// <param name="arg">
    ///     <see cref="PropertyChangedEventArgs" />
    /// </param>
    internal void OptionsOnPropertyChanged(object? sender, PropertyChangedEventArgs arg)
    {
        // 同步 ValidatorOptions.ValidateAllProperties 属性值到 _annotationValidator.ValidateAllProperties
        if (arg.PropertyName == nameof(ValidatorOptions.ValidateAllProperties))
        {
            _annotationValidator.ValidateAllProperties = Options.ValidateAllProperties;
        }
    }

    // TODO: 这里还未提供解析服务的处理，是提供 GetService<T> 还是 ServiceProvider
}