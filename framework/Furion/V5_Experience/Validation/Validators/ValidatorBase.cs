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
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Furion.Validation;

/// <summary>
///     验证器抽象基类
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
public abstract class ValidatorBase<T> : ValidatorBase
{
    /// <summary>
    ///     <inheritdoc cref="ValidatorBase{T}" />
    /// </summary>
    protected ValidatorBase()
    {
    }

    /// <summary>
    ///     <inheritdoc cref="ValidatorBase{T}" />
    /// </summary>
    /// <param name="errorMessage">错误信息</param>
    protected ValidatorBase(string errorMessage)
        : base(errorMessage)
    {
    }

    /// <summary>
    ///     <inheritdoc cref="ValidatorBase{T}" />
    /// </summary>
    /// <param name="errorMessageResourceAccessor">错误信息资源访问器</param>
    protected ValidatorBase(Func<string> errorMessageResourceAccessor)
        : base(errorMessageResourceAccessor)
    {
    }

    /// <summary>
    ///     检查对象合法性
    /// </summary>
    /// <param name="instance">对象</param>
    /// <returns>
    ///     <see cref="bool" />
    /// </returns>
    public abstract bool IsValid(T? instance);

    /// <summary>
    ///     获取对象验证结果集合
    /// </summary>
    /// <param name="instance">对象</param>
    /// <param name="name">显示名称</param>
    /// <param name="memberNames">成员名称列表</param>
    /// <returns>
    ///     <see cref="List{T}" />
    /// </returns>
    public virtual List<ValidationResult>? GetValidationResults(T? instance, string name,
        IEnumerable<string>? memberNames = null) =>
        base.GetValidationResults(instance, name, memberNames);

    /// <summary>
    ///     验证指定的对象
    /// </summary>
    /// <param name="instance">对象</param>
    /// <param name="name">显示名称</param>
    /// <param name="memberNames">成员名称列表</param>
    /// <exception cref="ValidationException"></exception>
    public virtual void Validate(T? instance, string name, IEnumerable<string>? memberNames = null) =>
        base.Validate(instance, name, memberNames);

    /// <inheritdoc />
    public sealed override bool IsValid(object? value) => IsValid(ConvertValue(value));

    /// <inheritdoc />
    public sealed override List<ValidationResult>? GetValidationResults(object? value, string name,
        IEnumerable<string>? memberNames = null) =>
        GetValidationResults(ConvertValue(value), name, memberNames);

    /// <inheritdoc />
    public sealed override void Validate(object? value, string name, IEnumerable<string>? memberNames = null) =>
        Validate(ConvertValue(value), name, memberNames);

    /// <summary>
    ///     将 <see cref="object" /> 类型对象转换为 <typeparamref name="T" /> 类型对象
    /// </summary>
    /// <param name="value">对象</param>
    /// <returns>
    ///     <typeparamref name="T" />
    /// </returns>
    internal static T? ConvertValue(object? value) => (T?)value;
}

/// <summary>
///     验证器抽象基类
/// </summary>
public abstract class ValidatorBase
{
    /// <summary>
    ///     错误信息
    /// </summary>
    internal string? _errorMessage;

    /// <summary>
    ///     错误信息资源访问器
    /// </summary>
    internal Func<string>? _errorMessageResourceAccessor;

    /// <summary>
    ///     错误信息资源名称
    /// </summary>
    internal string? _errorMessageResourceName;

    /// <summary>
    ///     错误信息资源类型
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties |
                                DynamicallyAccessedMemberTypes.NonPublicProperties)]
    internal Type? _errorMessageResourceType;

    /// <summary>
    ///     <inheritdoc cref="ValidatorBase" />
    /// </summary>
    protected ValidatorBase()
        : this(() => ValidationMessages.ValidatorBase_ValidationError)
    {
    }

    /// <summary>
    ///     <inheritdoc cref="ValidatorBase" />
    /// </summary>
    /// <param name="errorMessage">错误信息</param>
    protected ValidatorBase(string errorMessage)
        : this(() => errorMessage)
    {
    }

    /// <summary>
    ///     <inheritdoc cref="ValidatorBase" />
    /// </summary>
    /// <param name="errorMessageResourceAccessor">错误信息资源访问器</param>
    protected ValidatorBase(Func<string> errorMessageResourceAccessor) =>
        _errorMessageResourceAccessor = errorMessageResourceAccessor;

    /// <summary>
    ///     错误信息
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            _errorMessageResourceAccessor = null;
            CustomErrorMessageSet = true;

            // 触发属性变更事件
            OnPropertyChanged(value);
        }
    }

    /// <summary>
    ///     错误信息资源名称
    /// </summary>
    public string? ErrorMessageResourceName
    {
        get => _errorMessageResourceName;
        set
        {
            _errorMessageResourceName = value;
            _errorMessageResourceAccessor = null;
            CustomErrorMessageSet = true;

            // 触发属性变更事件
            OnPropertyChanged(value);
        }
    }

    /// <summary>
    ///     错误信息资源类型
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties |
                                DynamicallyAccessedMemberTypes.NonPublicProperties)]
    public Type? ErrorMessageResourceType
    {
        get => _errorMessageResourceType;
        set
        {
            _errorMessageResourceType = value;
            _errorMessageResourceAccessor = null;
            CustomErrorMessageSet = true;

            // 触发属性变更事件
            OnPropertyChanged(value);
        }
    }

    /// <summary>
    ///     错误信息资源访问器
    /// </summary>
    private protected Func<string> ErrorMessageResourceAccessor
    {
        init => _errorMessageResourceAccessor = value;
    }

    /// <summary>
    ///     错误信息字符串
    /// </summary>
    protected string ErrorMessageString
    {
        get
        {
            // 设置错误信息资源访问器
            SetupResourceAccessor();

            return _errorMessageResourceAccessor!();
        }
    }

    /// <summary>
    ///     是否设置了错误信息
    /// </summary>
    internal bool CustomErrorMessageSet { get; private set; }

    /// <summary>
    ///     属性变更事件
    /// </summary>
    protected event EventHandler<ValidationPropertyChangedEventArgs>? PropertyChanged;

    /// <summary>
    ///     检查对象合法性
    /// </summary>
    /// <param name="value">对象</param>
    /// <returns>
    ///     <see cref="bool" />
    /// </returns>
    public abstract bool IsValid(object? value);

    /// <summary>
    ///     获取对象验证结果集合
    /// </summary>
    /// <param name="value">对象</param>
    /// <param name="name">显示名称</param>
    /// <param name="memberNames">成员名称列表</param>
    /// <returns>
    ///     <see cref="List{T}" />
    /// </returns>
    public virtual List<ValidationResult>? GetValidationResults(object? value, string name,
        IEnumerable<string>? memberNames = null) =>
        IsValid(value) ? null : [new ValidationResult(FormatErrorMessage(name), memberNames)];

    /// <summary>
    ///     验证指定的对象
    /// </summary>
    /// <param name="value">对象</param>
    /// <param name="name">显示名称</param>
    /// <param name="memberNames">成员名称列表</param>
    /// <exception cref="ValidationException"></exception>
    public virtual void Validate(object? value, string name, IEnumerable<string>? memberNames = null)
    {
        // 检查对象合法性
        if (!IsValid(value))
        {
            throw new ValidationException(new ValidationResult(FormatErrorMessage(name), memberNames), null, value);
        }
    }

    /// <summary>
    ///     错误信息格式化设置
    /// </summary>
    /// <param name="name">显示名称</param>
    /// <returns>
    ///     <see cref="string" />
    /// </returns>
    public virtual string? FormatErrorMessage(string name) =>
        string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name);

    /// <summary>
    ///     触发属性变更事件
    /// </summary>
    /// <param name="propertyValue">已更改属性的值</param>
    /// <param name="propertyName">已更改属性的名称</param>
    protected void OnPropertyChanged(object? propertyValue, [CallerMemberName] string? propertyName = null)
    {
        // 空检查
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        PropertyChanged?.Invoke(this, new ValidationPropertyChangedEventArgs(propertyName, propertyValue));
    }

    /// <summary>
    ///     设置错误信息资源访问器
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    internal void SetupResourceAccessor()
    {
        // 空检查
        if (_errorMessageResourceAccessor is not null)
        {
            return;
        }

        var localErrorMessage = ErrorMessage;
        var resourceNameSet = !string.IsNullOrEmpty(_errorMessageResourceName);
        var errorMessageSet = !string.IsNullOrEmpty(_errorMessage);
        var resourceTypeSet = _errorMessageResourceType is not null;

        // 以下组合是非法的，会抛出 InvalidOperationException：
        //   1) 同时设置了 ErrorMessage 和 ErrorMessageResourceName 属性
        //   2) 或者 ErrorMessage、ErrorMessageResourceName 属性均未设置
        if ((resourceNameSet && errorMessageSet) || !(resourceNameSet || errorMessageSet))
        {
            throw new InvalidOperationException(
                "Either ErrorMessageString or ErrorMessageResourceName must be set, but not both.");
        }

        // 必须同时设置或都不设置 ErrorMessageResourceType 和 ErrorMessageResourceName 属性
        if (resourceTypeSet != resourceNameSet)
        {
            throw new InvalidOperationException(
                "Both ErrorMessageResourceType and ErrorMessageResourceName need to be set on this validator.");
        }

        // 如果设置了错误信息资源类型及其资源名称，那么就去查找该资源对应的值并设置错误信息资源访问器
        if (resourceNameSet)
        {
            SetResourceAccessorByPropertyLookup();
        }
        // 否则将错误信息设置给错误信息资源访问器
        else
        {
            _errorMessageResourceAccessor = () => localErrorMessage!;
        }
    }

    /// <summary>
    ///     通过错误信息资源查找并设置错误信息资源访问器
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    internal void SetResourceAccessorByPropertyLookup()
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(_errorMessageResourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(_errorMessageResourceName);

        // 尝试获取 ErrorMessageResourceType 类型的 ErrorMessageResourceName 属性
        var property = _errorMessageResourceType.GetProperty(_errorMessageResourceName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);

        // 检查属性是否只对同一程序集中的其他类型可见，而对该程序集以外的派生类型则不可见（顾名思义，使用 internal 声明的属性）
        // https://learn.microsoft.com/zh-cn/dotnet/api/system.reflection.methodbase.isassembly?view=net-9.0
        if (property?.GetMethod is null or { IsAssembly: false, IsPublic: false })
        {
            property = null;
        }

        // 空检查
        if (property is null)
        {
            throw new InvalidOperationException(
                $"The resource type `{_errorMessageResourceType.FullName}` does not have an accessible static property named `{_errorMessageResourceName}`.");
        }

        // 检查 ErrorMessageResourceName 属性类型是否是 string 类型
        if (property.PropertyType != typeof(string))
        {
            throw new InvalidOperationException(
                $"The property `{property.Name}` on resource type `{_errorMessageResourceType.FullName}` is not a string type.");
        }

        _errorMessageResourceAccessor = () => (string)property.GetValue(null, null)!;
    }
}