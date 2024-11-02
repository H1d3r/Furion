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
using System.Reflection;

namespace Furion.HttpRemote;

/// <summary>
///     HTTP 声明式远程请求构建器
/// </summary>
/// <remarks>使用 <c>HttpRequestBuilder.Declarative(method, args)</c> 静态方法创建。</remarks>
public sealed class HttpDeclarativeBuilder
{
    /// <summary>
    ///     HTTP 声明式 <see cref="IHttpDeclarativeExtractor" /> 提取器集合
    /// </summary>
    internal static readonly Dictionary<Type, IHttpDeclarativeExtractor> _extractors = new()
    {
        { typeof(ValidationDeclarativeExtractor), new ValidationDeclarativeExtractor() },
        { typeof(HttpClientNameDeclarativeExtractor), new HttpClientNameDeclarativeExtractor() },
        { typeof(TraceIdentifierDeclarativeExtractor), new TraceIdentifierDeclarativeExtractor() },
        { typeof(ProfilerDeclarativeExtractor), new ProfilerDeclarativeExtractor() },
        { typeof(SimulateBrowserDeclarativeExtractor), new SimulateBrowserDeclarativeExtractor() },
        { typeof(DisableCacheDeclarativeExtractor), new DisableCacheDeclarativeExtractor() },
        { typeof(EnsureSuccessStatusCodeDeclarativeExtractor), new EnsureSuccessStatusCodeDeclarativeExtractor() },
        { typeof(TimeoutDeclarativeExtractor), new TimeoutDeclarativeExtractor() },
        { typeof(QueryDeclarativeExtractor), new QueryDeclarativeExtractor() },
        { typeof(PathDeclarativeExtractor), new PathDeclarativeExtractor() },
        { typeof(CookieDeclarativeExtractor), new CookieDeclarativeExtractor() },
        { typeof(HeaderDeclarativeExtractor), new HeaderDeclarativeExtractor() },
        { typeof(BodyDeclarativeExtractor), new BodyDeclarativeExtractor() },
        { typeof(MultipartBodyDeclarativeExtractor), new MultipartBodyDeclarativeExtractor() },
        { typeof(HttpRequestBuilderDeclarativeExtractor), new HttpRequestBuilderDeclarativeExtractor() }
    };

    /// <summary>
    ///     标识是否已加载自定义 HTTP 声明式提取器
    /// </summary>
    internal bool _hasLoadedExtractors;

    /// <summary>
    ///     <inheritdoc cref="HttpDeclarativeBuilder" />
    /// </summary>
    /// <param name="method">被调用方法</param>
    /// <param name="args">被调用方法的参数值数组</param>
    internal HttpDeclarativeBuilder(MethodInfo method, object?[] args)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(args);

        Method = method;
        Args = args;
    }

    /// <summary>
    ///     被调用方法
    /// </summary>
    public MethodInfo Method { get; }

    /// <summary>
    ///     被调用方法的参数值数组
    /// </summary>
    public object?[] Args { get; }

    /// <summary>
    ///     构建 <see cref="HttpRequestBuilder" /> 实例
    /// </summary>
    /// <param name="httpRemoteOptions">
    ///     <see cref="HttpRemoteOptions" />
    /// </param>
    /// <returns>
    ///     <see cref="HttpRequestBuilder" />
    /// </returns>
    /// <exception cref="InvalidOperationException"></exception>
    internal HttpRequestBuilder Build(HttpRemoteOptions httpRemoteOptions)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(httpRemoteOptions);

        // 检查被调用方法是否贴有 [HttpMethod] 特性
        if (!Method.IsDefined(typeof(HttpMethodAttribute), true))
        {
            throw new InvalidOperationException($"No `[HttpMethod]` annotation was found in method `{Method.Name}`.");
        }

        // 获取 HttpMethodAttribute 实例
        var httpMethodAttribute = Method.GetCustomAttribute<HttpMethodAttribute>(true)!;

        // 初始化 HttpRequestBuilder 实例
        var httpRequestBuilder =
            HttpRequestBuilder.Create(httpMethodAttribute.Method, httpMethodAttribute.RequestUri);

        // 初始化 HttpDeclarativeExtractorContext 实例
        var httpDeclarativeExtractorContext = new HttpDeclarativeExtractorContext(Method, Args);

        // 检查是否已加载自定义 HTTP 声明式提取器
        if (!_hasLoadedExtractors)
        {
            _hasLoadedExtractors = true;

            // 添加自定义 IHttpDeclarativeExtractor 数组
            _extractors.TryAdd(httpRemoteOptions.HttpDeclarativeExtractors?.SelectMany(u => u.Invoke()).ToArray(),
                value => value.GetType());
        }

        // 遍历 HTTP 声明式提取器集合
        foreach (var extractor in _extractors.Values)
        {
            // 提取方法信息构建 HttpRequestBuilder 实例
            extractor.Extract(httpRequestBuilder, httpDeclarativeExtractorContext);
        }

        return httpRequestBuilder;
    }
}