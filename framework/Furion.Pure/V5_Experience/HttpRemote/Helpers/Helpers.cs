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

using Furion.Utilities;
using Microsoft.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace Furion.HttpRemote;

/// <summary>
///     HTTP 远程请求模块帮助类
/// </summary>
internal static partial class Helpers
{
    /// <summary>
    ///     从互联网 URL 地址中加载流
    /// </summary>
    /// <param name="requestUri">互联网 URL 地址</param>
    /// <param name="maxResponseContentBufferSize">响应内容的最大缓存大小。默认值为：<c>100MB</c>。</param>
    /// <returns>
    ///     <see cref="Tuple{T1, T2}" />
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    internal static Tuple<Stream, long> GetStreamFromRemote(string requestUri,
        long maxResponseContentBufferSize = 104857600L)
    {
        // 空检查
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri);

        // 检查 URL 地址是否是互联网地址
        if (!NetworkUtility.IsWebUrl(requestUri))
        {
            throw new ArgumentException($"Invalid internet address: `{requestUri}`.", nameof(requestUri));
        }

        // 初始化 HttpClient 实例
        using var httpClient = new HttpClient();

        // 限制流大小
        httpClient.MaxResponseContentBufferSize = maxResponseContentBufferSize;

        // 设置默认 User-Agent
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(HeaderNames.UserAgent,
            Constants.USER_AGENT_OF_BROWSER);

        try
        {
            // 发送 HTTP 远程请求
            var httpResponseMessage = httpClient.Send(new HttpRequestMessage(HttpMethod.Get, requestUri),
                HttpCompletionOption.ResponseHeadersRead);

            // 确保请求成功
            httpResponseMessage.EnsureSuccessStatusCode();

            // 读取流和长度
            var stream = httpResponseMessage.Content.ReadAsStream();
            var length = httpResponseMessage.Content.Headers.ContentLength ?? stream.Length;

            return Tuple.Create(stream, length);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Failed to load stream from internet address: `{requestUri}`.", e);
        }
    }

    /// <summary>
    ///     从 <see cref="Uri" /> 中解析文件的名称
    /// </summary>
    /// <param name="uri">
    ///     <see cref="Uri" />
    /// </param>
    /// <returns>
    ///     <see cref="string" />
    /// </returns>
    internal static string? GetFileNameFromUri(Uri? uri)
    {
        // 空检查
        if (uri is null)
        {
            return null;
        }

        // 获取 URL 的绝对路径
        var path = uri.AbsolutePath;

        // 使用 / 分割路径，并获取最后一个部分作为潜在的文件的名称
        var parts = path.Split('/');
        var fileName = parts.Length > 0 ? parts[^1] : string.Empty;

        // 检查文件的名称是否为空或仅由点组成
        if (string.IsNullOrEmpty(fileName) || fileName.Trim('.').Length == 0)
        {
            return string.Empty;
        }

        // 查找文件的名称中的查询字符串开始位置。如果存在查询字符串，则去除它
        var queryStartIndex = fileName.IndexOf('?');
        if (queryStartIndex != -1)
        {
            fileName = fileName[..queryStartIndex];
        }

        // 检查文件的名称是否包含有效的扩展名
        var lastDotIndex = fileName.LastIndexOf('.');
        if (lastDotIndex == -1 || lastDotIndex == fileName.Length - 1)
        {
            return string.Empty;
        }

        // 使用 UTF-8 解码文件的名称
        return Uri.UnescapeDataString(fileName);
    }

    /// <summary>
    ///     解析 HTTP 谓词
    /// </summary>
    /// <param name="httpMethod">HTTP 谓词</param>
    /// <returns>
    ///     <see cref="HttpMethod" />
    /// </returns>
    internal static HttpMethod ParseHttpMethod(string? httpMethod)
    {
        // 空检查
        ArgumentException.ThrowIfNullOrWhiteSpace(httpMethod);

        return HttpMethod.Parse(httpMethod);
    }

    /// <summary>
    ///     验证字符串是否是 <c>application/x-www-form-urlencoded</c> 格式
    /// </summary>
    /// <param name="output">字符串</param>
    /// <returns>
    ///     <see cref="bool" />
    /// </returns>
    internal static bool IsFormUrlEncodedFormat(string output)
    {
        // 空检查
        ArgumentException.ThrowIfNullOrWhiteSpace(output);

        return FormUrlEncodedFormatRegex().IsMatch(output);
    }

    /// <summary>
    ///     <c>application/x-www-form-urlencoded</c> 格式正则表达式
    /// </summary>
    /// <returns>
    ///     <see cref="Regex" />
    /// </returns>
    [GeneratedRegex(
        @"^(?:(?:[a-zA-Z0-9-._~]+|%(?:[0-9A-Fa-f]{2}))+=(?:[a-zA-Z0-9-._~]*|%(?:[0-9A-Fa-f]{2}))+)(?:&(?:[a-zA-Z0-9-._~]+|%(?:[0-9A-Fa-f]{2}))+=(?:[a-zA-Z0-9-._~]*|%(?:[0-9A-Fa-f]{2}))+)*$")]
    private static partial Regex FormUrlEncodedFormatRegex();
}