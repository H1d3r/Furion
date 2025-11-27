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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Furion.Validation;

/// <inheritdoc cref="PropertyValidator{T,TProperty}" />
public sealed partial class PropertyValidator<T, TProperty> where T : class
{
    /// <summary>
    ///     设置错误信息
    /// </summary>
    /// <param name="errorMessage">错误信息</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> WithErrorMessage(string? errorMessage)
    {
        // 空检查
        if (_lastAddedValidator is null)
        {
            return this;
        }

        // 将错误消息设置给最新添加的验证器实例
        _lastAddedValidator.WithErrorMessage(errorMessage);

        // 重置最新添加的验证器实例
        _lastAddedValidator = null;

        return this;
    }

    /// <summary>
    ///     设置错误信息
    /// </summary>
    /// <param name="resourceType">错误信息资源类型</param>
    /// <param name="resourceName">错误信息资源名称</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> WithErrorMessage(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties |
                                    DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type resourceType, string resourceName)
    {
        // 空检查
        if (_lastAddedValidator is null)
        {
            return this;
        }

        // 将错误消息设置给最新添加的验证器实例
        _lastAddedValidator.WithErrorMessage(resourceType, resourceName);

        // 重置最新添加的验证器实例
        _lastAddedValidator = null;

        return this;
    }

    /// <summary>
    ///     设置显示名称
    /// </summary>
    /// <param name="displayName">显示名称</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> WithDisplayName(string? displayName)
    {
        DisplayName = displayName;

        return this;
    }

    /// <summary>
    ///     配置是否启用该属性上的验证特性验证
    /// </summary>
    /// <param name="enabled">是否启用</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> UseAnnotationValidation(bool? enabled)
    {
        SuppressAnnotationValidation = !enabled;

        return this;
    }

    /// <summary>
    ///     配置启用该属性上的验证特性验证
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> UseAnnotationValidation() => UseAnnotationValidation(true);

    /// <summary>
    ///     配置跳过该属性上的验证特性验证
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> SkipAnnotationValidation() => UseAnnotationValidation(false);

    /// <summary>
    ///     配置跳过该属性上的验证特性验证
    /// </summary>
    /// <remarks>仅验证自定义规则。</remarks>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> CustomOnly() => UseAnnotationValidation(false);

    /// <summary>
    ///     添加年龄（0-120 岁）验证器
    /// </summary>
    /// <param name="isAdultOnly">是否仅验证成年人（18 岁及以上），默认值为：<c>false</c></param>
    /// <param name="allowStringValues">允许字符串数值，默认值为：<c>false</c></param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Age(bool isAdultOnly = false, bool allowStringValues = false) =>
        AddValidator(new AgeValidator { IsAdultOnly = isAdultOnly, AllowStringValues = allowStringValues });

    /// <summary>
    ///     添加允许的值列表验证器
    /// </summary>
    /// <param name="values">允许的值列表</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> AllowedValues(params object?[] values) =>
        AddValidator(new AllowedValuesValidator(values));

    /// <summary>
    ///     添加银行卡号验证器（Luhn 算法）
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> BankCard() => AddValidator(new BankCardValidator());

    /// <summary>
    ///     添加 Base64 字符串验证器
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Base64String() => AddValidator(new Base64StringValidator());

    /// <summary>
    ///     添加中文姓名验证器
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> ChineseName() => AddValidator(new ChineseNameValidator());

    /// <summary>
    ///     添加中文/汉字验证器
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Chinese() => AddValidator(new ChineseValidator());

    /// <summary>
    ///     添加颜色值验证器
    /// </summary>
    /// <param name="fullMode">
    ///     是否启用完整模式。在完整模式下，支持的颜色格式包括：十六进制颜色、RGB、RGBA、HSL 和 HSLA。若未启用，则仅支持：十六进制颜色、RGB 和 RGBA。默认值为：<c>false</c>
    /// </param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> ColorValue(bool fullMode = false) =>
        AddValidator(new ColorValueValidator { FullMode = fullMode });

    /// <summary>
    ///     添加组合验证器
    /// </summary>
    /// <param name="validators">验证器列表</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Composite(params ValidatorBase[] validators) =>
        AddValidator(new CompositeValidator(validators));

    /// <summary>
    ///     添加组合验证器
    /// </summary>
    /// <param name="validators">验证器列表</param>
    /// <param name="mode">
    ///     <see cref="ValidationMode" />
    /// </param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Composite(ValidatorBase[] validators, ValidationMode mode) =>
        AddValidator(new CompositeValidator(validators) { Mode = mode });

    /// <summary>
    ///     添加条件验证器
    /// </summary>
    /// <param name="buildConditions">条件构建器配置委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Conditional(Action<ConditionBuilder<TProperty?>> buildConditions) =>
        AddValidator(new ConditionalValidator<TProperty?>(buildConditions));

    /// <summary>
    ///     添加条件验证器
    /// </summary>
    /// <param name="buildConditions">条件构建器配置委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Conditional(Action<T, ConditionBuilder<TProperty?>> buildConditions)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(buildConditions);

        return ValidatorProxy<ConditionalValidator<TProperty>>(instance =>
            [new Action<ConditionBuilder<TProperty?>>(u => buildConditions(instance, u))]);
    }

    /// <summary>
    ///     添加 <see cref="System.DateOnly" /> 验证器
    /// </summary>
    /// <param name="formats">允许的日期格式（如 "yyyy-MM-dd"）</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> DateOnly(params string[] formats) =>
        AddValidator(new DateOnlyValidator(formats));

    /// <summary>
    ///     添加 <see cref="System.DateOnly" /> 验证器
    /// </summary>
    /// <param name="formats">允许的日期格式（如 "yyyy-MM-dd"）</param>
    /// <param name="provider">格式提供器</param>
    /// <param name="style">日期解析样式，需与 <paramref name="provider" /> 搭配使用。默认值为：<see cref="DateTimeStyles.None" /></param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> DateOnly(string[] formats, IFormatProvider? provider,
        DateTimeStyles style = DateTimeStyles.None) =>
        AddValidator(new DateOnlyValidator(formats) { Provider = provider, Style = style });

    /// <summary>
    ///     添加 <see cref="System.DateTime" /> 验证器
    /// </summary>
    /// <param name="formats">允许的日期格式（如 "yyyy-MM-dd HH:mm:ss"）</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> DateTime(params string[] formats) =>
        AddValidator(new DateTimeValidator(formats));

    /// <summary>
    ///     添加 <see cref="System.DateTime" /> 验证器
    /// </summary>
    /// <param name="formats">允许的日期格式（如 "yyyy-MM-dd HH:mm:ss"）</param>
    /// <param name="provider">格式提供器</param>
    /// <param name="style">日期解析样式，需与 <paramref name="provider" /> 搭配使用。默认值为：<see cref="DateTimeStyles.None" /></param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> DateTime(string[] formats, IFormatProvider? provider,
        DateTimeStyles style = DateTimeStyles.None) =>
        AddValidator(new DateTimeValidator(formats) { Provider = provider, Style = style });

    /// <summary>
    ///     添加验证数值的小数位数验证器
    /// </summary>
    /// <param name="maxDecimalPlaces">允许的最大有效小数位数</param>
    /// <param name="allowStringValues">允许字符串数值，默认值为：<c>false</c></param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> DecimalPlaces(int maxDecimalPlaces, bool allowStringValues = false) =>
        AddValidator(new DecimalPlacesValidator(maxDecimalPlaces) { AllowStringValues = allowStringValues });

    /// <summary>
    ///     添加不允许的值列表验证器
    /// </summary>
    /// <param name="values">不允许的值列表</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> DeniedValues(params object?[] values) =>
        AddValidator(new DeniedValuesValidator(values));

    /// <summary>
    ///     添加域名验证器
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Domain() => AddValidator(new DomainValidator());

    /// <summary>
    ///     添加邮箱地址验证器
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> EmailAddress() => AddValidator(new EmailAddressValidator());

    /// <summary>
    ///     添加以特定字符/字符串结尾的验证器
    /// </summary>
    /// <param name="searchValue">检索的值</param>
    /// <param name="comparison"><see cref="StringComparison" />，默认值为：<see cref="StringComparison.Ordinal" /></param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> EndsWith(string searchValue,
        StringComparison comparison = StringComparison.Ordinal) =>
        AddValidator(new EndsWithValidator(searchValue) { Comparison = comparison });

    /// <summary>
    ///     添加相等验证器
    /// </summary>
    /// <param name="compareValue">比较的值</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> EqualTo(object? compareValue) =>
        AddValidator(new EqualToValidator(compareValue));

    /// <summary>
    ///     添加相等验证器
    /// </summary>
    /// <param name="compareValueAccessor">比较的值访问器</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> EqualTo(Func<T, object?> compareValueAccessor)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(compareValueAccessor);

        return ValidatorProxy<EqualToValidator>(instance => [compareValueAccessor(instance)]);
    }

    /// <summary>
    ///     添加大于等于验证器
    /// </summary>
    /// <param name="compareValue">比较的值</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> GreaterThanOrEqualTo(IComparable compareValue) =>
        AddValidator(new GreaterThanOrEqualToValidator(compareValue));

    /// <summary>
    ///     添加大于等于验证器
    /// </summary>
    /// <param name="compareValueAccessor">比较的值访问器</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> GreaterThanOrEqualTo(Func<T, IComparable> compareValueAccessor)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(compareValueAccessor);

        return ValidatorProxy<GreaterThanOrEqualToValidator>(instance => [compareValueAccessor(instance)]);
    }

    /// <summary>
    ///     添加大于验证器
    /// </summary>
    /// <param name="compareValue">比较的值</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> GreaterThan(IComparable compareValue) =>
        AddValidator(new GreaterThanValidator(compareValue));

    /// <summary>
    ///     添加大于验证器
    /// </summary>
    /// <param name="compareValueAccessor">比较的值访问器</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> GreaterThan(Func<T, IComparable> compareValueAccessor)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(compareValueAccessor);

        return ValidatorProxy<GreaterThanValidator>(instance => [compareValueAccessor(instance)]);
    }

    /// <summary>
    ///     添加身份证号验证器
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> IDCard() => AddValidator(new IDCardValidator());

    /// <summary>
    ///     添加 JSON 格式验证器
    /// </summary>
    /// <param name="allowTrailingCommas">是否允许末尾多余逗号，默认值为：<c>false</c></param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Json(bool allowTrailingCommas = false) =>
        AddValidator(new JsonValidator { AllowTrailingCommas = allowTrailingCommas });

    /// <summary>
    ///     添加长度验证器
    /// </summary>
    /// <param name="minimumLength">最小允许长度</param>
    /// <param name="maximumLength">最大允许长度</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Length(int minimumLength, int maximumLength) =>
        AddValidator(new LengthValidator(minimumLength, maximumLength));

    /// <summary>
    ///     添加小于等于验证器
    /// </summary>
    /// <param name="compareValue">比较的值</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> LessThanOrEqualTo(IComparable compareValue) =>
        AddValidator(new LessThanOrEqualToValidator(compareValue));

    /// <summary>
    ///     添加小于等于验证器
    /// </summary>
    /// <param name="compareValueAccessor">比较的值访问器</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> LessThanOrEqualTo(Func<T, IComparable> compareValueAccessor)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(compareValueAccessor);

        return ValidatorProxy<LessThanOrEqualToValidator>(instance => [compareValueAccessor(instance)]);
    }

    /// <summary>
    ///     添加小于验证器
    /// </summary>
    /// <param name="compareValue">比较的值</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> LessThan(IComparable compareValue) =>
        AddValidator(new LessThanValidator(compareValue));

    /// <summary>
    ///     添加小于验证器
    /// </summary>
    /// <param name="compareValueAccessor">比较的值访问器</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> LessThan(Func<T, IComparable> compareValueAccessor)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(compareValueAccessor);

        return ValidatorProxy<LessThanValidator>(instance => [compareValueAccessor(instance)]);
    }

    /// <summary>
    ///     添加最大长度验证器
    /// </summary>
    /// <param name="length">最大允许长度</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> MaxLength(int length) => AddValidator(new MaxLengthValidator(length));

    /// <summary>
    ///     添加最大值验证器
    /// </summary>
    /// <param name="maximum">允许的最大字段值</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Max(IComparable maximum) =>
        AddValidator(new MaxValidator(maximum));

    /// <summary>
    ///     添加 MD5 字符串验证器
    /// </summary>
    /// <param name="allowShortFormat">是否允许截断的 128 位哈希值（16 字节的十六进制字符串，共 32 字符），默认值为：<c>false</c></param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> MD5String(bool allowShortFormat = false) =>
        AddValidator(new MD5StringValidator { AllowShortFormat = allowShortFormat });

    /// <summary>
    ///     添加最小长度验证器
    /// </summary>
    /// <param name="length">最小允许长度</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> MinLength(int length) => AddValidator(new MinLengthValidator(length));

    /// <summary>
    ///     添加最小值验证器
    /// </summary>
    /// <param name="minimum">允许的最小字段值</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Min(IComparable minimum) => AddValidator(new MinValidator(minimum));

    /// <summary>
    ///     添加自定义条件不成立时委托验证器
    /// </summary>
    /// <param name="condition">条件委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> MustUnless(Func<TProperty?, bool> condition) =>
        AddValidator(new MustUnlessValidator<TProperty>(condition));

    /// <summary>
    ///     添加自定义条件不成立时委托验证器
    /// </summary>
    /// <param name="condition">条件委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> MustUnless(Func<T, TProperty?, bool> condition)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(condition);

        return ValidatorProxy<MustUnlessValidator<TProperty>>(instance =>
            [new Func<TProperty?, bool>(u => condition(instance, u))]);
    }

    /// <summary>
    ///     添加自定义条件成立时委托验证器
    /// </summary>
    /// <param name="condition">条件委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Must(Func<TProperty?, bool> condition) =>
        AddValidator(new MustValidator<TProperty>(condition));

    /// <summary>
    ///     添加自定义条件成立时委托验证器
    /// </summary>
    /// <param name="condition">条件委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Must(Func<T, TProperty?, bool> condition)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(condition);

        return ValidatorProxy<MustValidator<TProperty>>(instance =>
            [new Func<TProperty?, bool>(u => condition(instance, u))]);
    }

    /// <summary>
    ///     添加非空白字符串验证器
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> NotBlank() => AddValidator(new NotBlankValidator());

    /// <summary>
    ///     添加非空集合、数组和字符串验证器
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> NotEmpty() => AddValidator(new NotEmptyValidator());

    /// <summary>
    ///     添加不相等验证器
    /// </summary>
    /// <param name="compareValue">比较的值</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> NotEqualTo(object? compareValue) =>
        AddValidator(new NotEqualToValidator(compareValue));

    /// <summary>
    ///     添加不相等验证器
    /// </summary>
    /// <param name="compareValueAccessor">比较的值访问器</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> NotEqualTo(Func<T, object?> compareValueAccessor)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(compareValueAccessor);

        return ValidatorProxy<NotEqualToValidator>(instance => [compareValueAccessor(instance)]);
    }

    /// <summary>
    ///     添加非 null 验证器
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> NotNull() => AddValidator(new NotNullValidator());

    /// <summary>
    ///     添加密码验证器
    /// </summary>
    /// <param name="strong">是否启用强密码验证模式，默认值为：<c>false</c></param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Password(bool strong = false) =>
        AddValidator(new PasswordValidator { Strong = strong });

    /// <summary>
    ///     添加手机号（中国）验证器
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> PhoneNumber() =>
        AddValidator(new PhoneNumberValidator());

    /// <summary>
    ///     添加邮政编码（中国）验证器
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> PostalCode() =>
        AddValidator(new PostalCodeValidator());

    /// <summary>
    ///     添加自定义条件成立时委托验证器
    /// </summary>
    /// <param name="condition">条件委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Predicate(Func<TProperty?, bool> condition) =>
        AddValidator(new PredicateValidator<TProperty>(condition));

    /// <summary>
    ///     添加自定义条件成立时委托验证器
    /// </summary>
    /// <param name="condition">条件委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Predicate(Func<T, TProperty?, bool> condition)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(condition);

        return ValidatorProxy<PredicateValidator<TProperty>>(instance =>
            [new Func<TProperty?, bool>(u => condition(instance, u))]);
    }

    /// <summary>
    ///     添加指定数值范围约束验证器
    /// </summary>
    /// <param name="minimum">允许的最小字段值</param>
    /// <param name="maximum">允许的最大字段值</param>
    /// <param name="configure">自定义配置委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Range(int minimum, int maximum, Action<RangeValidator>? configure = null) =>
        AddValidator(new RangeValidator(minimum, maximum), configure);

    /// <summary>
    ///     添加指定数值范围约束验证器
    /// </summary>
    /// <param name="minimum">允许的最小字段值</param>
    /// <param name="maximum">允许的最大字段值</param>
    /// <param name="configure">自定义配置委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Range(double minimum, double maximum,
        Action<RangeValidator>? configure = null) =>
        AddValidator(new RangeValidator(minimum, maximum), configure);

    /// <summary>
    ///     添加指定数值范围约束验证器
    /// </summary>
    /// <param name="type">数据字段值的类型</param>
    /// <param name="minimum">允许的最小字段值</param>
    /// <param name="maximum">允许的最大字段值</param>
    /// <param name="configure">自定义配置委托</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Range(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type type, string minimum, string maximum, Action<RangeValidator>? configure = null) =>
        AddValidator(new RangeValidator(type, minimum, maximum), configure);

    /// <summary>
    ///     添加正则表达式验证器
    /// </summary>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="matchTimeoutInMilliseconds">用于在操作超时前执行单个匹配操作的时间量。以毫秒为单位，默认值为：2000。</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> RegularExpression(string pattern, int matchTimeoutInMilliseconds = 2000) =>
        AddValidator(
            new RegularExpressionValidator(pattern) { MatchTimeoutInMilliseconds = matchTimeoutInMilliseconds });

    /// <summary>
    ///     添加必填验证器
    /// </summary>
    /// <param name="allowEmptyStrings">是否允许空字符串。默认值为：<c>false</c>。</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Required(bool allowEmptyStrings = false) =>
        AddValidator(new RequiredValidator { AllowEmptyStrings = allowEmptyStrings });

    /// <summary>
    ///     添加单项验证器
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Single() => AddValidator(new SingleValidator());

    /// <summary>
    ///     添加以特定字符/字符串开头的验证器
    /// </summary>
    /// <param name="searchValue">检索的值</param>
    /// <param name="comparison"><see cref="StringComparison" />，默认值为：<see cref="StringComparison.Ordinal" /></param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> StartsWith(string searchValue,
        StringComparison comparison = StringComparison.Ordinal) =>
        AddValidator(new StartsWithValidator(searchValue) { Comparison = comparison });

    /// <summary>
    ///     添加包含特定字符/字符串的验证器
    /// </summary>
    /// <param name="searchValue">检索的值</param>
    /// <param name="comparison"><see cref="StringComparison" />，默认值为：<see cref="StringComparison.Ordinal" /></param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> StringContains(string searchValue,
        StringComparison comparison = StringComparison.Ordinal) =>
        AddValidator(new StringContainsValidator(searchValue) { Comparison = comparison });

    /// <summary>
    ///     添加字符串长度验证器
    /// </summary>
    /// <param name="maximumLength">最大允许长度</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> StringLength(int maximumLength) =>
        AddValidator(new StringLengthValidator(maximumLength));

    /// <summary>
    ///     添加字符串长度验证器
    /// </summary>
    /// <param name="minimumLength">最小允许长度</param>
    /// <param name="maximumLength">最大允许长度</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> StringLength(int minimumLength, int maximumLength) =>
        AddValidator(new StringLengthValidator(maximumLength) { MinimumLength = minimumLength });

    /// <summary>
    ///     添加强密码模式验证器
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> StrongPassword() => AddValidator(new StrongPasswordValidator());

    /// <summary>
    ///     添加座机（电话）验证器
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Telephone() => AddValidator(new TelephoneValidator());

    /// <summary>
    ///     添加时间格式 <see cref="System.TimeOnly" /> 验证器
    /// </summary>
    /// <param name="formats">允许的时间格式（如 "HH:mm:ss"）</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> TimeOnly(params string[] formats) =>
        AddValidator(new TimeOnlyValidator(formats));

    /// <summary>
    ///     添加时间格式 <see cref="System.TimeOnly" /> 验证器
    /// </summary>
    /// <param name="formats">允许的时间格式（如 "HH:mm:ss"）</param>
    /// <param name="provider">格式提供器</param>
    /// <param name="style">日期解析样式，需与 <paramref name="provider" /> 搭配使用。默认值为：<see cref="DateTimeStyles.None" /></param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> TimeOnly(string[] formats, IFormatProvider? provider,
        DateTimeStyles style = DateTimeStyles.None) =>
        AddValidator(new TimeOnlyValidator(formats) { Provider = provider, Style = style });

    /// <summary>
    ///     添加 URL 地址验证器
    /// </summary>
    /// <param name="supportsFtp">是否支持 FTP 协议。默认值为：<c>false</c>。</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> Url(bool supportsFtp = false) =>
        AddValidator(new UrlValidator { SupportsFtp = supportsFtp });

    /// <summary>
    ///     添加用户名验证器
    /// </summary>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> UserName() => AddValidator(new UserNameValidator());

    /// <summary>
    ///     添加验证器代理
    /// </summary>
    /// <param name="constructorArgsFactory"><typeparamref name="TValidator" /> 构造函数参数工厂</param>
    /// <param name="configure">配置验证器实例</param>
    /// <typeparam name="TValidator">
    ///     <see cref="ValidatorBase" />
    /// </typeparam>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> ValidatorProxy<TValidator>(Func<T, object?[]?>? constructorArgsFactory,
        Action<TValidator>? configure = null)
        where TValidator : ValidatorBase
    {
        // 初始化 ValidatorProxy<T, TValidator> 实例
        var validatorProxy = new ValidatorProxy<T, TValidator>(instance => GetValue(instance), constructorArgsFactory);

        // 空检查
        if (configure is not null)
        {
            validatorProxy.Configure(configure);
        }

        return AddValidator(validatorProxy);
    }

    /// <summary>
    ///     添加验证器代理
    /// </summary>
    /// <param name="constructorArgs"><typeparamref name="TValidator" /> 构造函数参数列表</param>
    /// <param name="configure">配置验证器实例</param>
    /// <typeparam name="TValidator">
    ///     <see cref="ValidatorBase" />
    /// </typeparam>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> ValidatorProxy<TValidator>(object?[]? constructorArgs,
        Action<TValidator>? configure = null)
        where TValidator : ValidatorBase
    {
        // 初始化 ValidatorProxy<TValidator> 实例
        var validatorProxy = new ValidatorProxy<TValidator>(constructorArgs);

        // 空检查
        if (configure is not null)
        {
            validatorProxy.Configure(configure);
        }

        return AddValidator(validatorProxy);
    }

    /// <summary>
    ///     添加验证特性验证器
    /// </summary>
    /// <param name="attributes">验证特性列表</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> AddAnnotations(params ValidationAttribute[] attributes) =>
        AddValidator(new ValueAnnotationValidator(attributes, _objectValidator._serviceProvider, null));

    /// <summary>
    ///     添加验证特性验证器
    /// </summary>
    /// <param name="attributes">验证特性列表</param>
    /// <param name="items">验证上下文关联的键值对字典</param>
    /// <returns>
    ///     <see cref="PropertyValidator{T,TProperty}" />
    /// </returns>
    public PropertyValidator<T, TProperty> AddAnnotations(ValidationAttribute[] attributes,
        IDictionary<object, object?>? items) =>
        AddValidator(new ValueAnnotationValidator(attributes, _objectValidator._serviceProvider, items));

    // TODO: 考虑支持解析服务的的拓展，比如先解析服务，满足某种添加再操作，另外是否考虑数据验证中，比如是否大于某个值，而这个值是通过服务解析出来的！

    // TODO: Func<T, bool> 改为 Func<ValidationContext, bool>，因为里面可能还会解析服务

    // TODO: IServiceProvider 还没用上，还有 ValidationContent 也是

    // TODO: MustUseServices：解析服务的验证器
}