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

using Furion;
using Furion.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using System.Reflection;

namespace System;

/// <summary>
/// 用于原生应用（WinForm/WPF）创建窗口
/// </summary>
public static class Native
{
    /// <summary>
    /// 创建原生应用（WinForm/WPF）窗口
    /// </summary>
    /// <typeparam name="TWindow"></typeparam>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static TWindow CreateInstance<TWindow>(params object[] parameters)
        where TWindow : class
    {
        return CreateInstance(typeof(TWindow), parameters) as TWindow;
    }

    /// <summary>
    /// 创建原生应用（WinForm/WPF）组件窗口
    /// </summary>
    /// <param name="windowType"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static object CreateInstance(Type windowType, params object[] parameters)
    {
        // 获取构造函数
        var constructors = windowType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        // 如果构造函数为空，则直接创建返回
        if (constructors.Length == 0) return Activator.CreateInstance(windowType);

        // 检查是否包含多个公开构造函数
        if (constructors.Length > 1) throw new InvalidOperationException($"Multiple constructors accepting all given argument types have been found in type '{windowType.Namespace}.{windowType.Name}'. There should only be one applicable constructor.");

        // 获取唯一构造函数参数
        var parameterInfos = constructors[0].GetParameters();

        // 准备构造函数参数
        var ctorParameters = new List<object>();

        // 创建服务作用域
        var serviceScope = App.RootServices.CreateScope();

        // 遍历构造函数参数
        for (var i = 0; i < parameterInfos.Length; i++)
        {
            var parameterInfo = parameterInfos[i];

            var serviceType = parameterInfo.ParameterType;
            object serviceInstance;

            // 获取服务注册生命周期
            var serviceLifetime = App.GetServiceLifetime(serviceType);

            // 如果构造函数不是服务类型，则直接跳出
            if (serviceLifetime == null) break;

            // 如果是单例，直接从根服务解析
            if (serviceLifetime == ServiceLifetime.Singleton)
            {
                serviceInstance = App.RootServices.GetService(serviceType);
            }
            // 否则通过作用域解析
            else
            {
                serviceInstance = serviceScope.ServiceProvider.GetService(serviceType);
            }

            ctorParameters.Add(serviceInstance);
        }

        // 创建窗口实例
        var windowInstance = Activator.CreateInstance(windowType, ctorParameters.Concat(parameters).ToArray());

        // 确保作用域在窗口关闭时释放
        AttachScopeDisposeOnClose(windowInstance, serviceScope);

        return windowInstance;
    }

    /// <summary>
    /// 在窗口关闭时释放服务作用域
    /// </summary>
    private static void AttachScopeDisposeOnClose(object window, IDisposable scope)
    {
        var type = window.GetType();

        // 尝试 WinForms 的 FormClosed 事件
        var closeEvent = type.GetEvent("FormClosed", BindingFlags.Instance | BindingFlags.Public);

        // 若没有则尝试 WPF 的 Closed 事件
        if (closeEvent == null)
        {
            closeEvent = type.GetEvent("Closed", BindingFlags.Instance | BindingFlags.Public);
        }

        if (closeEvent != null)
        {
            // 获取事件真实的委托类型
            var handlerType = closeEvent.EventHandlerType;

            // 构造闭包对象
            var closure = new CloseEventHandlerClosure
            {
                Scope = scope,
                Event = closeEvent,
                Window = window
            };

            // 使用表达式树动态生成一个关闭事件处理委托
            var handler = CreateCloseHandler(handlerType, closure);

            // 将生成的委托存入闭包
            closure.Handler = handler;

            // 绑定事件
            closeEvent.AddEventHandler(window, handler);
        }
        else
        {
            // 若窗口没有可用的关闭事件，则立即释放作用域
            scope.Dispose();
        }
    }

    /// <summary>
    /// 使用表达式树动态生成一个关闭事件处理委托
    /// </summary>
    /// <param name="handlerType">事件委托类型（如 FormClosedEventHandler 或 EventHandler）</param>
    /// <param name="closure">捕获的上下文对象</param>
    /// <returns><see cref="Delegate"/></returns>
    private static Delegate CreateCloseHandler(Type handlerType, CloseEventHandlerClosure closure)
    {
        // 获取委托 Invoke 方法的参数类型
        var invokeMethod = handlerType.GetMethod("Invoke");
        var parameters = invokeMethod.GetParameters();

        // 定义事件处理方法的参数表达式：(object sender, TEventArgs e)
        var senderParam = Expression.Parameter(parameters[0].ParameterType, "sender");
        var eParam = Expression.Parameter(parameters[1].ParameterType, "e");

        // 将闭包对象转为常量表达式
        var closureExpr = Expression.Constant(closure);

        // 生成 closure.Scope.Dispose() 调用
        var disposeCall = Expression.Call(
            Expression.PropertyOrField(closureExpr, nameof(CloseEventHandlerClosure.Scope)),
            typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose)));

        // 生成 closure.Event.RemoveEventHandler(closure.Window, closure.Handler) 调用
        var removeHandlerMethod = typeof(EventInfo).GetMethod(nameof(EventInfo.RemoveEventHandler), [typeof(object), typeof(Delegate)]);
        var removeCall = Expression.Call(
            Expression.PropertyOrField(closureExpr, nameof(CloseEventHandlerClosure.Event)),
            removeHandlerMethod,
            Expression.PropertyOrField(closureExpr, nameof(CloseEventHandlerClosure.Window)),
            Expression.PropertyOrField(closureExpr, nameof(CloseEventHandlerClosure.Handler)));

        var body = Expression.Block(disposeCall, removeCall);

        var lambda = Expression.Lambda(handlerType, body, senderParam, eParam);
        return lambda.Compile();
    }

    /// <summary>
    /// 获取一个空闲端口
    /// </summary>
    /// <returns></returns>
    public static int GetIdlePort()
    {
        return NetworkUtility.FindAvailableTcpPort();
    }


    /// <summary>
    /// 内部闭包类
    /// </summary>
    private sealed class CloseEventHandlerClosure
    {
        /// <summary>
        /// 服务作用域
        /// </summary>
        public IDisposable Scope;

        /// <summary>
        /// 关闭事件信息
        /// </summary>
        public EventInfo Event;

        /// <summary>
        /// 窗口实例
        /// </summary>
        public object Window;

        /// <summary>
        /// 动态生成的委托，用于事件移除
        /// </summary>
        public Delegate Handler;
    }
}