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

using System.Globalization;
using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace Furion.Xunit;

/// <summary>
///     依赖注入单元测试类型激活器
/// </summary>
/// <remarks>用于指示单元测试类如何创建。参考文献：https://xunit.net/docs/getting-started/v3/custom-test-class-construction.html?q=ITypeActivator</remarks>
/// <param name="serviceProvider">
///     <see cref="IServiceProvider" />
/// </param>
public sealed class DependencyInjectionTypeActivator(IServiceProvider serviceProvider) : ITypeActivator
{
    /// <inheritdoc />
    public object CreateInstance(ConstructorInfo constructor, object?[]? arguments,
        Func<Type, IReadOnlyCollection<ParameterInfo>, string> missingArgumentMessageFormatter)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(constructor);
        ArgumentNullException.ThrowIfNull(missingArgumentMessageFormatter);

        // 获取构造函数所在的声明类型
        var type = constructor.ReflectedType ?? constructor.DeclaringType ??
            throw new ArgumentException("Untyped constructors are not permitted.", nameof(constructor));

        // 空检查
        if (arguments is null)
        {
            return constructor.Invoke(arguments);
        }

        // 获取构造函数定义的参数元数据
        var parameters = constructor.GetParameters();

        // 检查构造函数参数和运行时传入的参数数量是否一致
        if (parameters.Length != arguments.Length)
        {
            throw new TestPipelineException(string.Format(CultureInfo.CurrentCulture,
                "Cannot create type '{0}' due to parameter count mismatch (needed {1}, got {2})",
                type.FullName ?? type.Name, parameters.Length, arguments.Length));
        }

        // 初始化用于存储解析后构造函数初始化参数的数组
        var resolvedArguments = new object?[arguments.Length];

        // 遍历运行时传入的参数
        for (var i = 0; i < arguments.Length; i++)
        {
            // 当前参数信息
            var current = arguments[i];
            var parameter = parameters[i];

            // 检查当前运行时参数是否是 Xunit 无法提供的参数
            if (current is Missing)
            {
                // 尝试从依赖注入容器中解析服务
                resolvedArguments[i] = serviceProvider.GetService(parameter.ParameterType);

                // 检查是否解析结果为 null 且该参数没有默认值
                if (resolvedArguments[i] is null && !parameter.HasDefaultValue)
                {
                    throw new TestPipelineException(missingArgumentMessageFormatter(type, [parameter]));
                }
            }
            else
            {
                // Xunit 已提供的参数（如 ITestContextAccessor, ITestOutputHelper），直接使用原始值
                resolvedArguments[i] = current;
            }
        }

        return constructor.Invoke(resolvedArguments);
    }
}