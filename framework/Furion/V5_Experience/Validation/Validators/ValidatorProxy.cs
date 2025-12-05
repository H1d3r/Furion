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
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Furion.Validation;

/// <summary>
///     验证器代理
/// </summary>
/// <typeparam name="TValidator">
///     <see cref="ValidatorBase" />
/// </typeparam>
public class ValidatorProxy<TValidator> : ValidatorBase, IDisposable, IValidatorInitializer
    where TValidator : ValidatorBase
{
    /// <summary>
    ///     <inheritdoc cref="ValidatorProxy{TValidator}" />
    /// </summary>
    /// <param name="constructorArgs"><typeparamref name="TValidator" /> 构造函数参数列表</param>
    public ValidatorProxy(params object?[]? constructorArgs)
    {
        ErrorMessageResourceAccessor = () => null!;
        Validator = (TValidator)Activator.CreateInstance(typeof(TValidator), constructorArgs)!;

        // 订阅属性变更事件
        PropertyChanged += OnPropertyChanged;
    }

    /// <summary>
    ///     <typeparamref name="TValidator" /> 实例
    /// </summary>
    protected TValidator Validator { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public void InitializeServiceProvider(Func<Type, object?>? serviceProvider)
    {
        // 检查验证器是否实现 IValidatorInitializer 接口
        if (Validator is IValidatorInitializer initializer)
        {
            // 同步 IServiceProvider 委托
            initializer.InitializeServiceProvider(serviceProvider);
        }
    }

    /// <summary>
    ///     配置验证器实例
    /// </summary>
    /// <param name="predicate">自定义配置委托</param>
    /// <returns>
    ///     <see cref="ValidatorProxy{TValidator}" />
    /// </returns>
    public ValidatorProxy<TValidator> Configure(Action<TValidator> predicate)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(predicate);

        predicate(Validator);

        return this;
    }

    /// <inheritdoc />
    public override bool IsValid(object? value) => Validator.IsValid(value);

    /// <inheritdoc />
    public override List<ValidationResult>? GetValidationResults(object? value, string name) =>
        Validator.GetValidationResults(value, name);

    /// <inheritdoc />
    public override void Validate(object? value, string name) => Validator.Validate(value, name);

    /// <inheritdoc />
    public override string? FormatErrorMessage(string name) => Validator.FormatErrorMessage(name);

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
        // 根据变更的属性名查找对应的验证器实例属性
        var validatorProperty =
            typeof(TValidator).GetProperty(eventArgs.PropertyName!, BindingFlags.Instance | BindingFlags.Public);

        // 检查验证器实例属性是否可写
        if (validatorProperty is not { CanWrite: true })
        {
            return;
        }

        // 设置验证器实例属性值
        validatorProperty.SetValue(Validator, eventArgs.PropertyValue);
    }
}

/// <summary>
///     验证器代理
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
/// <typeparam name="TValidator">
///     <see cref="ValidatorBase" />
/// </typeparam>
public class ValidatorProxy<T, TValidator> : ValidatorBase<T>, IDisposable
    where TValidator : ValidatorBase
{
    /// <summary>
    ///     <typeparamref name="TValidator" /> 构造函数参数工厂
    /// </summary>
    internal readonly Func<T, object?[]?>? _constructorArgsFactory;

    /// <summary>
    ///     属性变更字典
    /// </summary>
    internal readonly ConcurrentDictionary<string, object?> _propertyChanges = new();

    /// <summary>
    ///     验证器实例缓存字典
    /// </summary>
    internal readonly ConcurrentDictionary<int, TValidator> _validatorCache = new();

    /// <summary>
    ///     验证器实例配置委托集合
    /// </summary>
    internal readonly List<Action<TValidator>> _validatorConfigurations = [];

    /// <summary>
    ///     验证前值转换器
    /// </summary>
    internal readonly Func<T, object?> _valueTransformer;

    /// <summary>
    ///     <inheritdoc cref="ValidatorProxy{TValidator}" />
    /// </summary>
    /// <param name="valueTransformer">验证前值转换器</param>
    /// <param name="constructorArgsFactory"><typeparamref name="TValidator" /> 构造函数参数工厂</param>
    public ValidatorProxy(Func<T, object?> valueTransformer, Func<T, object?[]?>? constructorArgsFactory = null)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(valueTransformer);

        _valueTransformer = valueTransformer;
        _constructorArgsFactory = constructorArgsFactory;
        ErrorMessageResourceAccessor = () => null!;

        // 订阅属性变更事件
        PropertyChanged += OnPropertyChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     配置验证器实例
    /// </summary>
    /// <param name="predicate">自定义配置委托</param>
    /// <returns>
    ///     <see cref="ValidatorProxy{TValidator}" />
    /// </returns>
    public ValidatorProxy<T, TValidator> Configure(Action<TValidator> predicate)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(predicate);

        _validatorConfigurations.Add(predicate);

        // 清除缓存以确保新实例获取最新属性
        _validatorCache.Clear();

        return this;
    }

    /// <inheritdoc />
    public override bool IsValid(T? instance) => GetValidator(instance).IsValid(GetValidationValue(instance));

    /// <inheritdoc />
    public override List<ValidationResult>? GetValidationResults(T? instance, string name) =>
        GetValidator(instance).GetValidationResults(GetValidationValue(instance), name);

    /// <inheritdoc />
    public override void Validate(T? instance, string name) =>
        GetValidator(instance).Validate(GetValidationValue(instance), name);

    /// <inheritdoc />
    /// <exception cref="NotSupportedException"></exception>
    public sealed override string FormatErrorMessage(string name) =>
        throw new NotSupportedException("Use FormatErrorMessage(string name, T? instance) instead.");

    /// <summary>
    ///     错误信息格式化设置
    /// </summary>
    /// <param name="name">显示名称</param>
    /// <param name="instance">对象</param>
    /// <returns>
    ///     <see cref="string" />
    /// </returns>
    public virtual string? FormatErrorMessage(string name, T? instance) =>
        GetValidator(instance).FormatErrorMessage(name);

    /// <summary>
    ///     获取或创建被代理的验证器实例
    /// </summary>
    /// <param name="instance">对象</param>
    /// <returns>
    ///     <typeparamref name="TValidator" />
    /// </returns>
    protected TValidator GetValidator(T? instance)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);

        return _validatorCache.GetOrAdd(RuntimeHelpers.GetHashCode(instance), _ =>
        {
            // 反射创建验证器实例
            var validator = _constructorArgsFactory is null
                ? Activator.CreateInstance<TValidator>()
                : (TValidator)Activator.CreateInstance(typeof(TValidator), _constructorArgsFactory.Invoke(instance))!;

            // 应用属性变更到验证器
            ApplyPropertyChanges(validator);

            // 应用验证器实例配置
            foreach (var configuredValidator in _validatorConfigurations)
            {
                configuredValidator.Invoke(validator);
            }

            return validator;
        });
    }

    /// <summary>
    ///     获取用于验证的值
    /// </summary>
    /// <param name="instance">对象</param>
    /// <returns>
    ///     <see cref="object" />
    /// </returns>
    protected object? GetValidationValue(T? instance)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(instance);

        return _valueTransformer.Invoke(instance);
    }

    /// <summary>
    ///     应用属性变更到验证器
    /// </summary>
    /// <param name="validator">
    ///     <typeparamref name="TValidator" />
    /// </param>
    internal void ApplyPropertyChanges(TValidator validator)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(validator);

        // 遍历所有变更的属性
        foreach (var (propertyName, propertyValue) in _propertyChanges)
        {
            // 根据变更的属性名查找对应的验证器实例属性
            var validatorProperty =
                typeof(TValidator).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

            // 检查验证器实例属性是否可写
            if (validatorProperty is not { CanWrite: true })
            {
                continue;
            }

            validatorProperty.SetValue(validator, propertyValue);
        }
    }

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
        _propertyChanges.AddOrUpdate(eventArgs.PropertyName!, eventArgs.PropertyValue);

        // 清除缓存以确保新实例获取最新属性
        _validatorCache.Clear();
    }
}