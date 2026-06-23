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

using System.Reflection;

namespace Furion.EventBus;

/// <summary>
/// 事件处理程序执行前上下文
/// </summary>
public class EventHandlerExecutingContext : EventHandlerContext
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventSource">事件源（事件承载对象）</param>
    /// <param name="properties">共享上下文数据</param>
    /// <param name="handlerMethod">触发的方法</param>
    /// <param name="attribute">订阅特性</param>
    /// <param name="runId">事件运行的唯一标识</param>
    internal EventHandlerExecutingContext(IEventSource eventSource
        , IDictionary<object, object> properties
        , MethodInfo handlerMethod
        , EventSubscribeAttribute attribute
        , string runId)
        : base(eventSource, properties, handlerMethod, attribute, runId)
    {
    }

    /// <summary>
    /// 执行前时间
    /// </summary>
    public DateTime ExecutingTime { get; internal set; }

    /// <summary>
    /// 执行结果
    /// </summary>
    internal object Result { get; set; }

    /// <summary>
    /// 设置执行结果
    /// </summary>
    /// <param name="result"></param>
    public void SetResult(object result)
    {
        Result = result;
    }
}

/// <summary>
/// 泛型事件处理程序执行前上下文
/// </summary>
/// <typeparam name="T">事件承载（携带）数据类型</typeparam>
public sealed class EventHandlerExecutingContext<T> : EventHandlerExecutingContext
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventSource">事件源（事件承载对象）</param>
    /// <param name="properties">共享上下文数据</param>
    /// <param name="handlerMethod">触发的方法</param>
    /// <param name="attribute">订阅特性</param>
    /// <param name="runId">事件运行的唯一标识</param>
    internal EventHandlerExecutingContext(IEventSource eventSource
        , IDictionary<object, object> properties
        , MethodInfo handlerMethod
        , EventSubscribeAttribute attribute
        , string runId)
        : base(eventSource, properties, handlerMethod, attribute, runId)
    {
    }

    /// <summary>
    /// 强类型事件承载（携带）数据
    /// </summary>
    public T Payload => GetPayload<T>();

    /// <summary>
    /// 将泛型事件处理委托包装为双参数委托
    /// </summary>
    /// <param name="typedHandler">事件订阅委托</param>
    /// <returns></returns>
    internal static Func<EventHandlerExecutingContext, CancellationToken, Task> Wrap(Func<EventHandlerExecutingContext<T>, CancellationToken, Task> typedHandler)
    {
        return async (context, cancellationToken) =>
        {
            var typedContext = new EventHandlerExecutingContext<T>(
                context.Source,
                context.Properties,
                context.HandlerMethod,
                context.Attribute,
                context.RunId)
            {
                ExecutingTime = context.ExecutingTime,
                Result = context.Result
            };

            await typedHandler(typedContext, cancellationToken);

            context.Result = typedContext.Result;
        };
    }

    /// <summary>
    /// 将泛型事件处理委托包装为双参数委托
    /// </summary>
    /// <param name="typedHandler">事件订阅委托</param>
    /// <returns></returns>
    internal static Func<EventHandlerExecutingContext, CancellationToken, Task> WrapSingle(Func<EventHandlerExecutingContext<T>, Task> typedHandler)
    {
        return Wrap((ctx, _) => typedHandler(ctx));
    }
}