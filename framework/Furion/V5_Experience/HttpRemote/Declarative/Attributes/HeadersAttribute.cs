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

namespace Furion.HttpRemote;

/// <summary>
///     请求标头特性
/// </summary>
/// <remarks>支持指定多次。</remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Parameter,
    AllowMultiple = true)]
public sealed class HeadersAttribute : Attribute
{
    /// <summary>
    ///     <see cref="Values" /> 私有字段
    /// </summary>
    private object? _values;

    /// <summary>
    ///     <inheritdoc cref="HeadersAttribute" />
    /// </summary>
    /// <remarks>作用于参数</remarks>
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    public HeadersAttribute()
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    {
    }

    /// <summary>
    ///     <inheritdoc cref="HeadersAttribute" />
    /// </summary>
    /// <remarks>当用于方法或接口时，则进行移除指定标头操作。</remarks>
    /// <param name="name">标头</param>
    public HeadersAttribute(string name)
    {
        // 空检查
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
    }

    /// <summary>
    ///     <inheritdoc cref="HeadersAttribute" />
    /// </summary>
    /// <param name="name">标头</param>
    /// <param name="value">标头的值</param>
    public HeadersAttribute(string name, object? value)
        : this(name) =>
        Values = value;

    /// <summary>
    ///     标头
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     标头的值
    /// </summary>
    public object? Values
    {
        get => _values;
        set
        {
            _values = value;
            HasSetValues = true;
        }
    }

    /// <summary>
    ///     别名
    /// </summary>
    /// <remarks>当用于参数时有效。</remarks>
    public string? AliasAs { get; set; }

    /// <summary>
    ///     是否设置了标头的值
    /// </summary>
    internal bool HasSetValues { get; private set; }
}