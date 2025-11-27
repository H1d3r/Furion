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

using Furion.Validation.Resources;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Furion.Validation;

/// <summary>
///     指定数值范围约束验证器
/// </summary>
public class RangeValidator : ValidatorBase, IDisposable
{
    /// <summary>
    ///     需要监听属性变更的属性名集合
    /// </summary>
    internal readonly string[] _observedPropertyNames =
    [
        nameof(MinimumIsExclusive), nameof(MaximumIsExclusive), nameof(ParseLimitsInInvariantCulture),
        nameof(ConvertValueInInvariantCulture)
    ];

    /// <summary>
    ///     <inheritdoc cref="ValueAnnotationValidator" />
    /// </summary>
    internal readonly ValueAnnotationValidator _validator;

    /// <summary>
    ///     <inheritdoc cref="RangeValidator" />
    /// </summary>
    /// <param name="minimum">允许的最小字段值</param>
    /// <param name="maximum">允许的最大字段值</param>
    public RangeValidator(int minimum, int maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
        OperandType = typeof(int);
        ErrorMessageResourceAccessor = GetErrorMessage;

        _validator = new ValueAnnotationValidator(new RangeAttribute(minimum, maximum));

        // 订阅属性变更事件
        PropertyChanged += OnPropertyChanged;
    }

    /// <summary>
    ///     <inheritdoc cref="RangeValidator" />
    /// </summary>
    /// <param name="minimum">允许的最小字段值</param>
    /// <param name="maximum">允许的最大字段值</param>
    public RangeValidator(double minimum, double maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
        OperandType = typeof(double);
        ErrorMessageResourceAccessor = GetErrorMessage;

        _validator = new ValueAnnotationValidator(new RangeAttribute(minimum, maximum));

        // 订阅属性变更事件
        PropertyChanged += OnPropertyChanged;
    }

    /// <summary>
    ///     <inheritdoc cref="RangeValidator" />
    /// </summary>
    /// <param name="type">数据字段值的类型</param>
    /// <param name="minimum">允许的最小字段值</param>
    /// <param name="maximum">允许的最大字段值</param>
    public RangeValidator([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, string minimum,
        string maximum)
    {
        OperandType = type;
        Minimum = minimum;
        Maximum = maximum;
        ErrorMessageResourceAccessor = GetErrorMessage;

        _validator = new ValueAnnotationValidator(new RangeAttribute(type, minimum, maximum));

        // 订阅属性变更事件
        PropertyChanged += OnPropertyChanged;
    }

    /// <summary>
    ///     允许的最小字段值
    /// </summary>
    public object Minimum { get; }

    /// <summary>
    ///     允许的最大字段值
    /// </summary>
    public object Maximum { get; }

    /// <summary>
    ///     是否当值等于 <see cref="Minimum" /> 时验证失败
    /// </summary>
    /// <remarks>默认值为：<c>false</c>。</remarks>
    public bool MinimumIsExclusive
    {
        get;
        set
        {
            field = value;

            // 触发属性变更事件
            OnPropertyChanged(value);
        }
    }

    /// <summary>
    ///     是否当值等于 <see cref="Maximum" /> 时验证失败
    /// </summary>
    /// <remarks>默认值为：<c>false</c>。</remarks>
    public bool MaximumIsExclusive
    {
        get;
        set
        {
            field = value;

            // 触发属性变更事件
            OnPropertyChanged(value);
        }
    }

    /// <summary>
    ///     数据字段值的类型
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type OperandType { get; }

    /// <summary>
    ///     判断 <see cref="Minimum" /> 和 <see cref="Maximum" /> 的字符串值是否依据固定区域性而非当前区域性进行解析
    /// </summary>
    /// <remarks>默认值为：<c>false</c>。</remarks>
    public bool ParseLimitsInInvariantCulture
    {
        get;
        set
        {
            field = value;

            // 触发属性变更事件
            OnPropertyChanged(value);
        }
    }

    /// <summary>
    ///     验证由构造函数参数 <c>RangeValidator(Type, String, String)</c> 设置的 <c>type</c> 的 <see cref="OperandType" />
    ///     值在进行任何转换时，是否采用的是固定区域性而非当前区域性
    /// </summary>
    /// <remarks>默认值为：<c>false</c>。</remarks>
    public bool ConvertValueInInvariantCulture
    {
        get;
        set
        {
            field = value;

            // 触发属性变更事件
            OnPropertyChanged(value);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public override bool IsValid(object? value) => _validator.IsValid(value);

    /// <inheritdoc />
    public override string FormatErrorMessage(string name) =>
        string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, Minimum, Maximum);

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
    ///     获取错误信息
    /// </summary>
    /// <returns>
    ///     <see cref="string" />
    /// </returns>
    internal string GetErrorMessage() =>
        MinimumIsExclusive switch
        {
            true when MaximumIsExclusive => ValidationMessages.RangeValidator_ValidationError_MinExclusive_MaxExclusive,
            true => ValidationMessages.RangeValidator_ValidationError_MinExclusive,
            _ => MaximumIsExclusive
                ? ValidationMessages.RangeValidator_ValidationError_MaxExclusive
                : ValidationMessages.RangeValidator_ValidationError
        };

    /// <summary>
    ///     订阅属性变更事件
    /// </summary>
    /// <param name="sender">事件源</param>
    /// <param name="eventArgs">
    ///     <see cref="ValidationPropertyChangedEventArgs" />
    /// </param>
    internal void OnPropertyChanged(object? sender, ValidationPropertyChangedEventArgs eventArgs)
    {
        // 检查是否是需要同步的属性名
        if (!_observedPropertyNames.Contains(eventArgs.PropertyName))
        {
            return;
        }

        // 应用属性变更到 RangeAttribute 对应的属性中
        typeof(RangeAttribute).GetProperty(eventArgs.PropertyName!)
            ?.SetValue(_validator.Attributes[0], eventArgs.PropertyValue);
    }
}