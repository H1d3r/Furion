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
///     敏感词清理器配置选项
/// </summary>
public sealed record SensitiveWordOptions
{
    /// <summary>
    ///     默认选项
    /// </summary>
    /// <remarks>忽略大小写、忽略符号、忽略全角/半角差异、忽略 Unicode 字母变体。</remarks>
    public static readonly SensitiveWordOptions Default = new();

    /// <summary>
    ///     是否忽略大小写
    /// </summary>
    /// <remarks>默认值为：<c>true</c>。</remarks>
    public bool IgnoreCase { get; set; } = true;

    /// <summary>
    ///     是否跳过标点/空格/符号进行匹配
    /// </summary>
    /// <remarks>默认值为：<c>true</c>。</remarks>
    public bool IgnoreSymbol { get; set; } = true;

    /// <summary>
    ///     是否忽略全角与半角字符的差异
    /// </summary>
    /// <remarks>
    ///     <para>启用后，全角字母、数字、符号将视为与对应半角字符相同。例如 <c>ｆｕｃｋ</c> 等同于 <c>fuck</c>，<c>１２３</c> 等同于 <c>123</c>。</para>
    ///     <para>默认值为：<c>true</c>。</para>
    /// </remarks>
    public bool IgnoreFullwidth { get; set; } = true;

    /// <summary>
    ///     是否忽略 Unicode 字母变体
    /// </summary>
    /// <remarks>
    ///     <para>启用后，（带圈、带括号、数学粗体等），统一视为普通字母。例如 <c>Ⓕⓤc⒦</c>、<c>𝐟𝐮𝐜𝐤</c> 等变体均等同于 <c>fuck</c>。</para>
    ///     <para>默认值为：<c>true</c>。</para>
    /// </remarks>
    public bool IgnoreUnicodeVariants { get; set; } = true;
}