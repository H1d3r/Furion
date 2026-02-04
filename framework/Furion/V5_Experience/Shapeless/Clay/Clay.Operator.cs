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

using System.Dynamic;
using System.Text.Json.Nodes;

namespace Furion.Shapeless;

/// <summary>
///     流变对象
/// </summary>
public partial class Clay
{
    /// <inheritdoc />
    public bool Equals(Clay? other)
    {
        // 检查是否是相同的实例
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // 空检查及基础类型检查
        if (other is null || Type != other.Type)
        {
            return false;
        }

        return IsObject ? AreObjectEqual(this, other) : AreArrayEqual(this, other);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || Equals(obj as Clay);

    /// <summary>
    ///     重载 == 运算符
    /// </summary>
    /// <param name="left">
    ///     <see cref="Clay" />
    /// </param>
    /// <param name="right">
    ///     <see cref="Clay" />
    /// </param>
    /// <returns>
    ///     <see cref="bool" />
    /// </returns>
    public static bool operator ==(Clay? left, Clay? right) => Equals(left, right);

    /// <summary>
    ///     重载 != 运算符
    /// </summary>
    /// <param name="left">
    ///     <see cref="Clay" />
    /// </param>
    /// <param name="right">
    ///     <see cref="Clay" />
    /// </param>
    /// <returns>
    ///     <see cref="bool" />
    /// </returns>
    public static bool operator !=(Clay? left, Clay? right) => !(left == right);

    /// <summary>
    ///     支持 <see cref="Clay" /> 类型隐式转换为 <see cref="string" />
    /// </summary>
    /// <param name="clay">
    ///     <see cref="Clay" />
    /// </param>
    /// <returns>
    ///     <see cref="string" />
    /// </returns>
    public static implicit operator string(in Clay clay) => clay.JsonCanvas.ToJsonString();

    /// <summary>
    ///     支持 <see cref="Clay" /> 类型隐式转换为 <see cref="Dictionary{TKey,TValue}" />
    /// </summary>
    /// <param name="clay">
    ///     <see cref="Clay" />
    /// </param>
    /// <returns>
    ///     <see cref="Dictionary{TKey,TValue}" />
    /// </returns>
    public static implicit operator Dictionary<string, object?>(in Clay clay) =>
        clay.As<Dictionary<string, object?>>()!;

    /// <summary>
    ///     支持 <see cref="string" /> 类型隐式转换为 <see cref="Clay" />
    /// </summary>
    /// <param name="str">
    ///     <see cref="string" />
    /// </param>
    /// <returns>
    ///     <see cref="Clay" />
    /// </returns>
    public static implicit operator Clay(string? str) => Parse(str);

    /// <summary>
    ///     支持 <see cref="Stream" /> 类型隐式转换为 <see cref="Clay" />
    /// </summary>
    /// <param name="stream">
    ///     <see cref="Stream" />
    /// </param>
    /// <returns>
    ///     <see cref="Clay" />
    /// </returns>
    public static implicit operator Clay(Stream? stream) => Parse(stream);

    /// <summary>
    ///     支持 <see cref="byte" />[] 类型隐式转换为 <see cref="Clay" />
    /// </summary>
    /// <param name="bytes">
    ///     <see cref="byte" />[]
    /// </param>
    /// <returns>
    ///     <see cref="Clay" />
    /// </returns>
    public static implicit operator Clay(byte[]? bytes) => Parse(bytes);

    /// <summary>
    ///     支持 <see cref="JsonNode" /> 类型隐式转换为 <see cref="Clay" />
    /// </summary>
    /// <param name="jsonNode">
    ///     <see cref="JsonNode" />
    /// </param>
    /// <returns>
    ///     <see cref="Clay" />
    /// </returns>
    public static implicit operator Clay(JsonNode? jsonNode) => Parse(jsonNode);

    /// <summary>
    ///     支持 <see cref="Dictionary{TKey,TValue}" /> 类型隐式转换为 <see cref="Clay" />
    /// </summary>
    /// <param name="dic">
    ///     <see cref="Dictionary{TKey,TValue}" />
    /// </param>
    /// <returns>
    ///     <see cref="Clay" />
    /// </returns>
    public static implicit operator Clay(Dictionary<string, object?>? dic) => Parse(dic);

    /// <summary>
    ///     支持 <see cref="ExpandoObject" /> 类型隐式转换为 <see cref="Clay" />
    /// </summary>
    /// <param name="expandoObject">
    ///     <see cref="ExpandoObject" />
    /// </param>
    /// <returns>
    ///     <see cref="Clay" />
    /// </returns>
    public static implicit operator Clay(ExpandoObject? expandoObject) => Parse(expandoObject);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // 初始化 HashCode 实例
        var hash = new HashCode();

        if (IsObject)
        {
            // 预处理键值对（排序）
            var sortedEntries = AsEnumerateObject().OrderBy(kvp => kvp.Key, StringComparer.Ordinal);

            // 遍历键值对集合
            foreach (var (key, value) in sortedEntries)
            {
                // 递归计算键和值的哈希码
                hash.Add(key?.GetHashCode() ?? 0);
                hash.Add(value?.GetHashCode() ?? 0);
            }
        }
        else
        {
            // 遍历集合或数组集合
            foreach (var value in AsEnumerateArray())
            {
                // 递归计算元素的哈希码
                hash.Add(value?.GetHashCode() ?? 0);
            }
        }

        return hash.ToHashCode();
    }

    /// <summary>
    ///     检查两个单一对象实例是否相等
    /// </summary>
    /// <param name="clay1">
    ///     <see cref="Clay" />
    /// </param>
    /// <param name="clay2">
    ///     <see cref="Clay" />
    /// </param>
    /// <returns>
    ///     <see cref="bool" />
    /// </returns>
    internal static bool AreObjectEqual(Clay clay1, Clay clay2) =>
        clay1.Count == clay2.Count && clay1.All((dynamic? item) =>
            clay2.HasProperty(item?.Key) && object.Equals(item?.Value, clay2[item?.Key]));

    /// <summary>
    ///     检查两个集合或数组实例是否相等
    /// </summary>
    /// <param name="clay1">
    ///     <see cref="Clay" />
    /// </param>
    /// <param name="clay2">
    ///     <see cref="Clay" />
    /// </param>
    /// <returns>
    ///     <see cref="bool" />
    /// </returns>
    internal static bool AreArrayEqual(Clay clay1, Clay clay2)
    {
        // 检查集合或数组长度是否相等
        if (clay1.Count != clay2.Count)
        {
            return false;
        }

        // 遍历检查每一项是否相等
        for (var i = 0; i < clay1.Count; i++)
        {
            if (!Equals(clay1[i], clay2[i]))
            {
                return false;
            }
        }

        return true;
    }
}