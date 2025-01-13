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

namespace Furion.Shapeless;

/// <summary>
///     流变对象
/// </summary>
public partial class Clay
{
    /// <summary>
    ///     元素数量
    /// </summary>
    public int Count => IsObject ? JsonCanvas.AsObject().Count : JsonCanvas.AsArray().Count;

    /// <summary>
    ///     元素数量
    /// </summary>
    public int Length => Count;

    /// <summary>
    ///     是否为空元素
    /// </summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    ///     获取键或索引集合
    /// </summary>
    public IEnumerable<object> Indexes => AsEnumerable().Select(u => u.Key);

    /// <summary>
    ///     获取键或索引集合
    /// </summary>
    public IEnumerable<object> Keys => Indexes;

    /// <summary>
    ///     获取值集合
    /// </summary>
    public IEnumerable<dynamic?> Values => AsEnumerable().Select(u => u.Value);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    ///     返回循环访问元素的枚举数
    /// </summary>
    /// <returns>
    ///     <see cref="IEnumerator{T}" />
    /// </returns>
    public IEnumerator<KeyValuePair<object, dynamic?>> GetEnumerator() => AsEnumerable().GetEnumerator();

    /// <summary>
    ///     返回类型化为 <see cref="IEnumerable{T}" /> 的输入
    /// </summary>
    /// <returns>
    ///     <see cref="IEnumerable{T}" />
    /// </returns>
    public IEnumerable<KeyValuePair<object, dynamic?>> AsEnumerable() => IsObject
        ? AsEnumerableObject().Select(u => new KeyValuePair<object, dynamic?>(u.Key, u.Value))
        : AsEnumerableArray().Select(u => new KeyValuePair<object, dynamic?>(u.Key, u.Value));

    /// <summary>
    ///     枚举 <see cref="JsonCanvas" /> 作为对象时的键值对
    /// </summary>
    /// <returns>
    ///     <see cref="IEnumerable{T}" />
    /// </returns>
    public IEnumerable<KeyValuePair<string, dynamic?>> AsEnumerableObject()
    {
        // 检查是否是集合（数组）实例调用
        ThrowIfMethodCalledOnArrayCollection(nameof(AsEnumerableObject));

        // 获取循环访问 JsonObject 的枚举数
        using var enumerator = JsonCanvas.AsObject().GetEnumerator();

        // 遍历 JsonObject 每个键值对
        while (enumerator.MoveNext())
        {
            // 获取当前的键值对
            var current = enumerator.Current;

            yield return new KeyValuePair<string, dynamic?>(current.Key, DeserializeNode(current.Value, Options));
        }
    }

    /// <summary>
    ///     枚举 <see cref="JsonCanvas" /> 作为数组时的元素
    /// </summary>
    /// <returns>
    ///     <see cref="IEnumerable{T}" />
    /// </returns>
    public IEnumerable<KeyValuePair<int, dynamic?>> AsEnumerableArray()
    {
        // 检查是否是单一对象实例调用
        ThrowIfMethodCalledOnSingleObject(nameof(AsEnumerableArray));

        // 获取循环访问 JsonArray 的枚举数
        using var enumerator = JsonCanvas.AsArray().GetEnumerator();

        // 定义索引变量用于记录数组中元素的位置
        var index = 0;

        // 遍历 JsonArray 每个元素
        while (enumerator.MoveNext())
        {
            // 获取当前的元素
            var current = enumerator.Current;

            yield return new KeyValuePair<int, dynamic?>(index++, DeserializeNode(current, Options));
        }
    }
}