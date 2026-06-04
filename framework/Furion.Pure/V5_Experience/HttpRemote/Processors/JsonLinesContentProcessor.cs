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

using Furion.Extensions;
using Furion.HttpRemote.Extensions;
using System.Net.Http.Headers;
using System.Text;

namespace Furion.HttpRemote;

/// <summary>
///     JSON Lines 内容处理器
/// </summary>
/// <remarks>参考文献：https://jsonlines.org/</remarks>
public class JsonLinesContentProcessor : HttpContentProcessorBase
{
    /// <inheritdoc />
    public override bool CanProcess(HttpContentProcessorContext context) =>
        context.ContentType.IsIn(
            ["application/x-ndjson", "application/x-jsonlines", "application/jsonlines", "application/jsonl"],
            StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override HttpContent? Process(HttpContentProcessorContext context)
    {
        // 尝试解析 HttpContent 类型
        if (TryProcess(context, out var httpContent))
        {
            return httpContent;
        }

        // 检查原始请求内容是否为枚举类型，且其元素类型为引用类型（非字符串）
        if (!context.RawContent!.GetType().IsArrayOrCollection(out var underlyingType) || !underlyingType.IsClass ||
            underlyingType == typeof(string))
        {
            throw new InvalidOperationException(
                $"Expected IEnumerable<T> where T is a class type other than string, but received type `{context.RawContent.GetType()}`.");
        }

        // 将原始请求内容转换为 IEnumerable 类型
        var enumerable = (IEnumerable)context.RawContent;

        // 初始化 StringBuilder 实例
        var stringBuilder = new StringBuilder();

        // 解析 JSON 序列化配置
        var jsonSerializerOptions = ResolveJsonSerializerOptions(context.HttpClientName);

        // 构建 JSON Lines 格式内容
        foreach (var item in enumerable)
        {
            stringBuilder.Append(item.ToJsonString(jsonSerializerOptions)).Append('\n');
        }

        // 移除末尾多余换行
        var content = stringBuilder.ToString().TrimEnd('\n');

        // 初始化 StringContent 实例
        var stringContent = new StringContent(content, context.Encoding,
            new MediaTypeHeaderValue(context.ContentType) { CharSet = context.Encoding?.WebName });

        return stringContent;
    }
}