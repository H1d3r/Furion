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
///     组合验证器
/// </summary>
public class CompositeValidator : ValidatorBase, IValidatorInitializer, IDisposable
{
    /// <summary>
    ///     <see cref="ValidatorBase" /> 集合
    /// </summary>
    internal readonly List<ValidatorBase> _validators;

    /// <summary>
    ///     高优先级验证器区域的结束索引（同时也是普通验证器区域的起始索引）
    /// </summary>
    /// <remarks>
    ///     该索引将验证器列表划分为两个区域：<c>[0, _highPriorityEndIndex)</c> 为高优先级验证器区域（按 <see cref="IHighPriorityValidator.Priority" />
    ///     升序排列），<c>[_highPriorityEndIndex, Count)</c>
    ///     为普通验证器区域。当添加新验证器时，高优先级验证器会插入到指定位置以维持顺序，普通验证器则直接追加到列表末尾，此索引值会相应更新以维护区域边界。
    /// </remarks>
    internal int _highPriorityEndIndex;

    /// <summary>
    ///     <inheritdoc cref="CompositeValidator" />
    /// </summary>
    /// <param name="validators">验证器列表</param>
    public CompositeValidator(params ValidatorBase[] validators)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(validators);

        // 遍历验证器列表并添加
        _validators = [];
        foreach (var validator in validators)
        {
            AddValidator(validator);
        }

        Validators = _validators;
        ErrorMessageResourceAccessor = () => null!;
    }

    /// <summary>
    ///     验证器列表
    /// </summary>
    public IReadOnlyList<ValidatorBase> Validators { get; }

    /// <summary>
    ///     <inheritdoc cref="ValidationMode" />
    /// </summary>
    /// <remarks>默认值为：<see cref="ValidationMode.ValidateAll" />。</remarks>
    public ValidationMode Mode { get; set; } = ValidationMode.ValidateAll;

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
    public override bool IsValid(object? value) =>
        Mode switch
        {
            ValidationMode.ValidateAll or ValidationMode.BreakOnFirstError => Validators.All(u => u.IsValid(value)),
            ValidationMode.BreakOnFirstSuccess => Validators.Any(u => u.IsValid(value)),
            _ => throw new NotSupportedException()
        };

    /// <inheritdoc />
    public override List<ValidationResult>? GetValidationResults(object? value, string name,
        IEnumerable<string>? memberNames = null)
    {
        // 初始化验证结果集合和成员名称列表
        var validationResults = new List<ValidationResult>();
        var memberNameList = memberNames?.ToList();

        // 遍历验证器集合
        foreach (var validator in Validators)
        {
            // 获取对象验证结果集合
            if (validator.GetValidationResults(value, name, memberNameList) is { Count: > 0 } results)
            {
                // 追加验证结果集合
                validationResults.AddRange(results);

                // 检查验证器模式是否是首个验证失败则停止验证
                if (Mode is ValidationMode.BreakOnFirstError)
                {
                    break;
                }
            }
            // 检查验证器模式是否是首个验证成功则视为通过
            else if (Mode is ValidationMode.BreakOnFirstSuccess)
            {
                // 清空验证结果集合
                validationResults.Clear();

                break;
            }
        }

        // 如果验证未通过且配置了自定义错误信息，则在首部添加自定义错误信息
        if (validationResults.Count > 0 && (string?)ErrorMessageString is not null)
        {
            validationResults.Insert(0, new ValidationResult(FormatErrorMessage(name), memberNameList));
        }

        return validationResults.ToResults();
    }

    /// <inheritdoc />
    public override void Validate(object? value, string name, IEnumerable<string>? memberNames = null)
    {
        // 初始化首个验证无效的验证器和成员名称列表
        ValidatorBase? firstFailedValidator = null;
        var memberNameList = memberNames?.ToList();

        // 遍历验证器集合
        foreach (var validator in Validators)
        {
            // 检查对象合法性
            if (!validator.IsValid(value))
            {
                // 缓存首个验证无效的验证器
                firstFailedValidator ??= validator;

                // 检查验证器模式是否是验证所有或首个验证失败则停止验证
                if (Mode is ValidationMode.ValidateAll or ValidationMode.BreakOnFirstError)
                {
                    ThrowValidationException(value, name, validator, memberNameList);
                }
            }
            // 检查验证器模式是否是首个验证成功则视为通过
            else if (Mode is ValidationMode.BreakOnFirstSuccess)
            {
                return;
            }
        }

        // 空检查
        if (firstFailedValidator is not null)
        {
            ThrowValidationException(value, name, firstFailedValidator, memberNameList);
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
        foreach (var validator in Validators)
        {
            if (validator is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <summary>
    ///     添加验证器
    /// </summary>
    /// <param name="validator">
    ///     <see cref="ValidatorBase" />
    /// </param>
    internal void AddValidator(ValidatorBase validator)
    {
        // 空检查 
        ArgumentNullException.ThrowIfNull(validator);

        // 检查是否是高优先级验证器
        if (validator is IHighPriorityValidator highPriorityValidator)
        {
            // 只在 [0, _highPriorityEndIndex) 范围内查找插入位置（保持 Priority 升序）
            var insertIndex = _highPriorityEndIndex;
            for (var i = 0; i < _highPriorityEndIndex; i++)
            {
                // ReSharper disable once InvertIf
                if (_validators[i] is IHighPriorityValidator existing &&
                    existing.Priority > highPriorityValidator.Priority)
                {
                    insertIndex = i;
                    break;
                }
            }

            _validators.Insert(insertIndex, validator);

            // 高优先级区域扩大
            _highPriorityEndIndex++;
        }
        else
        {
            _validators.Add(validator);
        }
    }

    /// <inheritdoc cref="IValidatorInitializer.InitializeServiceProvider" />
    internal void InitializeServiceProvider(Func<Type, object?>? serviceProvider)
    {
        // 遍历所有验证器并尝试同步 IServiceProvider 委托
        foreach (var validator in Validators)
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