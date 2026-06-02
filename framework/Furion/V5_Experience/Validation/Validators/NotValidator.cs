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
///     取反验证器
/// </summary>
/// <remarks>只要内部配置的验证规则中【有任何一个】验证失败，则整体取反验证通过。</remarks>
/// <typeparam name="T">对象类型</typeparam>
public class NotValidator<T> : ValidatorBase<T>, IValidatorInitializer, IDisposable
{
    /// <summary>
    ///     <see cref="ValidatorBase" /> 集合
    /// </summary>
    internal readonly IReadOnlyList<ValidatorBase> _validators;

    /// <summary>
    ///     <inheritdoc cref="NotValidator{T}" />
    /// </summary>
    /// <param name="validators">验证器列表</param>
    public NotValidator(ValidatorBase[] validators)
        : this(u => u.AddValidators(validators))
    {
    }

    /// <summary>
    ///     <inheritdoc cref="NotValidator{T}" />
    /// </summary>
    /// <param name="configure">验证器配置委托</param>
    public NotValidator(Action<FluentValidatorBuilder<T>> configure)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(configure);

        _validators = new FluentValidatorBuilder<T>().Build(configure);
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
    public override bool IsValid(T? instance, ValidationContext<T> validationContext) =>
        !_validators.All(u => u.IsValid(instance, validationContext));

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
        foreach (var validator in _validators)
        {
            if (validator is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <inheritdoc cref="IValidatorInitializer.InitializeServiceProvider" />
    internal void InitializeServiceProvider(Func<Type, object?>? serviceProvider)
    {
        // 遍历所有验证器并尝试同步 IServiceProvider 委托
        foreach (var validator in _validators)
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