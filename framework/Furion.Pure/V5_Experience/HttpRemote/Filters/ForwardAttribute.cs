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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Furion.HttpRemote;

/// <summary>
///     <see cref="HttpContext" /> 转发操作筛选器
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ForwardAttribute : ActionFilterAttribute
{
    /// <summary>
    ///     <inheritdoc cref="ForwardAttribute" />
    /// </summary>
    /// <param name="requestUri">转发地址。若为空则尝试从请求标头 <c>X-Forward-To</c> 中获取目标地址。</param>
    public ForwardAttribute(string? requestUri) => RequestUri = requestUri;

    /// <summary>
    ///     <inheritdoc cref="ForwardAttribute" />
    /// </summary>
    /// <param name="requestUri">转发地址。若为空则尝试从请求标头 <c>X-Forward-To</c> 中获取目标地址。</param>
    /// <param name="httpMethod">转发方式</param>
    public ForwardAttribute(string? requestUri, HttpMethod httpMethod)
        : this(requestUri) =>
        Method = httpMethod;

    /// <summary>
    ///     转发地址
    /// </summary>
    public string? RequestUri { get; set; }

    /// <summary>
    ///     转发方式
    /// </summary>
    /// <remarks>若未设置，则自动采用当前请求方式作为转发方式。</remarks>
    public HttpMethod? Method { get; set; }

    /// <summary>
    ///     <see cref="HttpClient" /> 实例的配置名称
    /// </summary>
    /// <remarks>
    ///     <para>此属性用于指定 <see cref="IHttpClientFactory" /> 创建 <see cref="HttpClient" /> 实例时传递的名称。</para>
    ///     <para>该名称用于标识在服务容器中与特定 <see cref="HttpClient" /> 实例相关的配置。</para>
    /// </remarks>
    public string? HttpClientName { get; set; }

    /// <inheritdoc />
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 获取方法返回值类型
        var returnType = ((ControllerActionDescriptor)context.ActionDescriptor).MethodInfo.ReturnType;

        // 初始化最终返回值类型
        var finalReturnType = returnType == typeof(void) || returnType == typeof(Task)
            ? typeof(VoidContent)
            : typeof(Task<>).IsDefinitionEqual(returnType)
                ? returnType.GenericTypeArguments[0]
                : returnType;

        // 转发并获取结果
        var result = await context.HttpContext.ForwardAsAsync(finalReturnType,
            Method ?? Helpers.ParseHttpMethod(context.HttpContext.Request.Method), RequestUri,
            builder => builder.SetHttpClientName(HttpClientName ?? string.Empty));

        // 设置转发内容
        context.Result = result as IActionResult ?? new ObjectResult(result);
    }
}