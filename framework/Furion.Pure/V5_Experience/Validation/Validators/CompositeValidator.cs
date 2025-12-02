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
public class CompositeValidator : ValidatorBase
{
    /// <summary>
    ///     高优先级验证器列表
    /// </summary>
    internal readonly ValidatorBase[] _highPriorityValidators;

    /// <summary>
    ///     <inheritdoc cref="CompositeValidator" />
    /// </summary>
    /// <param name="validators">验证器列表</param>
    /// <exception cref="ArgumentException"></exception>
    public CompositeValidator(params ValidatorBase[] validators)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(validators);

        // 确保数组元素不存在 null 值
        if (validators.Any(u => (ValidatorBase?)u is null))
        {
            // ReSharper disable once LocalizableElement
            throw new ArgumentException("Validators cannot contain null elements.", nameof(validators));
        }

        Validators = validators;
        ErrorMessageResourceAccessor = () => null!;

        // 初始化高优先级验证器列表
        _highPriorityValidators = validators.OfType<IHighPriorityValidator>().OrderBy(u => u.Priority)
            .Cast<ValidatorBase>().ToArray();
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
    public override bool IsValid(object? value)
    {
        // 初始化将要被验证的验证器集合
        var validatorsToCheck =
            value is null && _highPriorityValidators.Length > 0 ? _highPriorityValidators : Validators;

        return Mode switch
        {
            ValidationMode.ValidateAll or ValidationMode.BreakOnFirstError => validatorsToCheck.All(u =>
                u.IsValid(value)),
            ValidationMode.BreakOnFirstSuccess => validatorsToCheck.Any(u => u.IsValid(value)),
            _ => throw new NotSupportedException()
        };
    }

    /// <inheritdoc />
    public override List<ValidationResult>? GetValidationResults(object? value, string name)
    {
        // 初始化将要被验证的验证器集合和验证结果集合
        var validatorsToCheck =
            value is null && _highPriorityValidators.Length > 0 ? _highPriorityValidators : Validators;
        var validationResults = new List<ValidationResult>();

        // 遍历验证器集合
        foreach (var validator in validatorsToCheck)
        {
            // 获取对象验证结果集合
            if (validator.GetValidationResults(value, name) is { Count: > 0 } results)
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
            validationResults.Insert(0, new ValidationResult(FormatErrorMessage(name), [name]));
        }

        return validationResults.ToResults();
    }

    /// <inheritdoc />
    public override void Validate(object? value, string name)
    {
        // 初始化将要被验证的验证器集合
        var validatorsToCheck =
            value is null && _highPriorityValidators.Length > 0 ? _highPriorityValidators : Validators;

        // 初始化首个验证无效的验证器
        ValidatorBase? firstFailedValidator = null;

        // 遍历验证器集合
        foreach (var validator in validatorsToCheck)
        {
            // 检查对象合法性
            if (!validator.IsValid(value))
            {
                // 缓存首个验证无效的验证器
                firstFailedValidator ??= validator;

                // 检查验证器模式是否是验证所有或首个验证失败则停止验证
                if (Mode is ValidationMode.ValidateAll or ValidationMode.BreakOnFirstError)
                {
                    ThrowValidationException(value, name, validator);
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
            ThrowValidationException(value, name, firstFailedValidator);
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
    /// <exception cref="ValidationException"></exception>
    internal void ThrowValidationException(object? value, string name, ValidatorBase validator)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(validator);

        // 检查是否配置了自定义错误信息
        if ((string?)ErrorMessageString is null)
        {
            validator.Validate(value, name);
        }
        else
        {
            throw new ValidationException(new ValidationResult(FormatErrorMessage(name), [name]), null, value);
        }
    }
}