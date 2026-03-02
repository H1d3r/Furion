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

using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Furion.Authorization;

/// <summary>
/// 授权策略执行程序
/// </summary>
[SuppressSniffer]
public abstract class AppAuthorizeHandler : IAuthorizationHandler
{
    /// <summary>
    /// 刷新 Token 身份标识
    /// </summary>
    private readonly string[] _refreshTokenClaims = new[] { "f", "e", "s", "l", "k" };

    /// <summary>
    /// 授权验证核心方法
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        // 获取 HttpContext 上下文
        var httpContext = context.GetCurrentHttpContext();

        try
        {
            await HandleAsync(context, httpContext);
        }
        catch (Exception exception)
        {
            context.Fail();

            // 处理规范化结果
            await UnifyWrapper(httpContext, exception);

            // 终止响应体被二次写入
            await httpContext.Response.CompleteAsync();
        }
    }

    /// <summary>
    /// 授权验证核心方法（可重写）
    /// </summary>
    /// <param name="context"></param>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public virtual async Task HandleAsync(AuthorizationHandlerContext context, DefaultHttpContext httpContext)
    {
        // 判断是否授权
        var isAuthenticated = context.User.Identity.IsAuthenticated;
        if (isAuthenticated)
        {
            // 禁止使用刷新 Token 进行单独校验
            if (_refreshTokenClaims.All(k => context.User.Claims.Any(c => c.Type == k)))
            {
                context.Fail();
                return;
            }

            await AuthorizeHandleAsync(context);
        }
        else context.GetCurrentHttpContext()?.SignoutToSwagger();    // 退出 Swagger 登录
    }

    /// <summary>
    /// 验证管道
    /// </summary>
    /// <param name="context"></param>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public virtual Task<bool> PipelineAsync(AuthorizationHandlerContext context, DefaultHttpContext httpContext)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// 策略验证管道
    /// </summary>
    /// <param name="context"></param>
    /// <param name="httpContext"></param>
    /// <param name="requirement"></param>
    /// <returns></returns>
    public virtual Task<bool> PolicyPipelineAsync(AuthorizationHandlerContext context, DefaultHttpContext httpContext, IAuthorizationRequirement requirement)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// 授权处理
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    protected async Task AuthorizeHandleAsync(AuthorizationHandlerContext context)
    {
        // 获取 HttpContext 上下文
        var httpContext = context.GetCurrentHttpContext();

        // 判断是否成功刷新 Token
        var isRefreshSuccessful = httpContext.Response.Headers.ContainsKey("x-access-token") && httpContext.Response.Headers.ContainsKey("access-token");

        // 刷新成功则创建新 Context
        var effectiveContext = isRefreshSuccessful
            ? new AuthorizationHandlerContext(context.Requirements, httpContext.User, context.Resource)
            : context;

        // 调用子类管道
        var pipeline = await PipelineAsync(effectiveContext, httpContext);
        if (pipeline)
        {
            // 获取所有未成功验证的需求
            var pendingRequirements = effectiveContext.PendingRequirements;

            // 通过授权验证
            foreach (var requirement in pendingRequirements)
            {
                // 验证策略管道
                var policyPipeline = await PolicyPipelineAsync(effectiveContext, httpContext, requirement);
                if (policyPipeline) effectiveContext.Succeed(requirement);
                else
                {
                    effectiveContext.Fail();
                    break;
                }
            }
        }
        else effectiveContext.Fail();

        // 刷新成功时，同步结果回原 context
        if (isRefreshSuccessful)
        {
            SyncAuthorizationResult(effectiveContext, context);
        }
    }

    /// <summary>
    /// 将授权结果从源 Context 同步到目标 Context
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    private static void SyncAuthorizationResult(AuthorizationHandlerContext source, AuthorizationHandlerContext target)
    {
        // 找出 source 中已被 Succeed 的需求（在 Requirements 但不在 PendingRequirements 中）
        var succeededRequirements = source.Requirements.Except(source.PendingRequirements);

        foreach (var requirement in succeededRequirements)
        {
            target.Succeed(requirement);
        }

        // 同步整体失败状态
        if (source.HasFailed) target.Fail();
    }

    /// <summary>
    /// 处理规范化结果
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    private static async Task UnifyWrapper(DefaultHttpContext httpContext, Exception exception)
    {
        // 尝试解析为友好异常
        var friendlyException = exception as AppFriendlyException;

        // 处理规范化结果
        if (!UnifyContext.CheckExceptionHttpContextNonUnify(httpContext, out var unifyRes))
        {
            _ = UnifyContext.CheckVaildResult(unifyRes.OnAuthorizeException(httpContext, new ExceptionMetadata
            {
                StatusCode = friendlyException?.StatusCode ?? StatusCodes.Status500InternalServerError,
                Errors = friendlyException?.ErrorMessage ?? exception.Message,
                Data = friendlyException?.Data,
                ErrorCode = friendlyException?.ErrorCode,
                OriginErrorCode = friendlyException?.OriginErrorCode,
                Exception = exception
            }), out var data);

            // 设置响应状态码
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await httpContext.Response.WriteAsJsonAsync(data, App.GetOptions<JsonOptions>()?.JsonSerializerOptions);
        }
        else throw exception;
    }
}