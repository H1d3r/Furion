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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Xml.Serialization;

namespace Furion.HttpRemote;

/// <summary>
///     <see cref="ObjectContentConverter{TResult}" /> 默认基类
/// </summary>
public class ObjectContentConverter : IHttpContentConverter
{
    /// <inheritdoc />
    public IServiceProvider? ServiceProvider { get; set; }

    /// <inheritdoc />
    public virtual object? Read(Type resultType, HttpResponseMessage httpResponseMessage,
        CancellationToken cancellationToken = default)
    {
        // 检查 HTTP 响应的内容类型是否为 XML 媒体类型
        if (!HasXmlContentType(httpResponseMessage))
        {
            return httpResponseMessage.Content
                .ReadFromJsonAsync(resultType, GetJsonSerializerOptions(httpResponseMessage), cancellationToken)
                .GetAwaiter().GetResult();
        }

        // 将 XML 字符串反序列化为转换的目标类型
        return DeserializeXml(resultType,
            httpResponseMessage.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult());
    }

    /// <inheritdoc />
    public virtual async Task<object?> ReadAsync(Type resultType, HttpResponseMessage httpResponseMessage,
        CancellationToken cancellationToken = default)
    {
        // 检查 HTTP 响应的内容类型是否为 XML 媒体类型
        if (!HasXmlContentType(httpResponseMessage))
        {
            return await httpResponseMessage.Content.ReadFromJsonAsync(resultType,
                GetJsonSerializerOptions(httpResponseMessage), cancellationToken);
        }

        // 将 XML 字符串反序列化为转换的目标类型
        return DeserializeXml(resultType, await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken));
    }

    /// <summary>
    ///     获取 JSON 序列化选项实例
    /// </summary>
    /// <param name="httpResponseMessage">
    ///     <see cref="HttpResponseMessage" />
    /// </param>
    /// <returns>
    ///     <see cref="JsonSerializerOptions" />
    /// </returns>
    protected virtual JsonSerializerOptions GetJsonSerializerOptions(HttpResponseMessage httpResponseMessage)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(httpResponseMessage);

        // 获取 HttpClient 实例的配置名称
        if (httpResponseMessage.RequestMessage?.Options.TryGetValue(
                new HttpRequestOptionsKey<string>(Constants.HTTP_CLIENT_NAME), out var httpClientName) != true)
        {
            httpClientName = string.Empty;
        }

        // 获取 HttpClientOptions 实例
        var httpClientOptions = ServiceProvider?.GetService<IOptionsMonitor<HttpClientOptions>>()?.Get(httpClientName);

        // 优先级：指定名称的 HttpClientOptions -> HttpRemoteOptions -> 默认值
        return (httpClientOptions?.IsDefault != false ? null : httpClientOptions.JsonSerializerOptions) ??
               ServiceProvider?.GetRequiredService<IOptions<HttpRemoteOptions>>().Value.JsonSerializerOptions ??
               HttpRemoteOptions.JsonSerializerOptionsDefault;
    }

    /// <summary>
    ///     检查 HTTP 响应的内容类型是否为 XML 媒体类型
    /// </summary>
    /// <param name="httpResponseMessage">
    ///     <see cref="HttpResponseMessage" />
    /// </param>
    /// <returns>
    ///     <see cref="bool" />
    /// </returns>
    protected virtual bool HasXmlContentType(HttpResponseMessage httpResponseMessage) =>
        httpResponseMessage.Content.Headers.ContentType?.MediaType.IsIn(
            [MediaTypeNames.Application.Xml, MediaTypeNames.Application.XmlPatch, MediaTypeNames.Text.Xml],
            StringComparer.OrdinalIgnoreCase) == true;

    /// <summary>
    ///     将 XML 字符串反序列化为转换的目标类型
    /// </summary>
    /// <param name="resultType">转换的目标类型</param>
    /// <param name="xmlString">XML 字符串</param>
    /// <returns>
    ///     <see cref="object" />
    /// </returns>
    protected virtual object? DeserializeXml(Type resultType, string xmlString)
    {
        // 初始化 XmlSerializer 实例
        var xmlSerializer = new XmlSerializer(resultType);

        // 初始化 StringReader 实例
        using var stringReader = new StringReader(xmlString);

        // XML 反序列化为对象
        return xmlSerializer.Deserialize(stringReader);
    }

    /// <summary>
    ///     将 XML 字符串反序列化为 <typeparamref name="TResult" /> 类型
    /// </summary>
    /// <param name="xmlString">XML 字符串</param>
    /// <typeparam name="TResult">转换的目标类型</typeparam>
    /// <returns>
    ///     <typeparamref name="TResult" />
    /// </returns>
    protected virtual TResult? DeserializeXml<TResult>(string xmlString) =>
        (TResult?)DeserializeXml(typeof(TResult), xmlString);
}

/// <summary>
///     对象转换器
/// </summary>
/// <typeparam name="TResult">转换的目标类型</typeparam>
public class ObjectContentConverter<TResult> : ObjectContentConverter, IHttpContentConverter<TResult>
{
    /// <inheritdoc />
    public virtual TResult? Read(HttpResponseMessage httpResponseMessage,
        CancellationToken cancellationToken = default)
    {
        // 检查 HTTP 响应的内容类型是否为 XML 媒体类型
        if (!HasXmlContentType(httpResponseMessage))
        {
            return httpResponseMessage.Content
                .ReadFromJsonAsync<TResult>(GetJsonSerializerOptions(httpResponseMessage), cancellationToken)
                .GetAwaiter().GetResult();
        }

        // 将 XML 字符串反序列化为转换的目标类型
        return DeserializeXml<TResult>(httpResponseMessage.Content.ReadAsStringAsync(cancellationToken).GetAwaiter()
            .GetResult());
    }

    /// <inheritdoc />
    public virtual async Task<TResult?> ReadAsync(HttpResponseMessage httpResponseMessage,
        CancellationToken cancellationToken = default)
    {
        // 检查 HTTP 响应的内容类型是否为 XML 媒体类型
        if (!HasXmlContentType(httpResponseMessage))
        {
            return await httpResponseMessage.Content.ReadFromJsonAsync<TResult>(
                GetJsonSerializerOptions(httpResponseMessage), cancellationToken);
        }

        // 将 XML 字符串反序列化为转换的目标类型
        return DeserializeXml<TResult>(await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken));
    }
}