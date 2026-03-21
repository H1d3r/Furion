using Furion.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

[assembly: TestPipelineStartup(typeof(TestPipelineStartup))]
[assembly: CaptureConsole]  // 支持使用 Console 静态类打印
[assembly: CaptureTrace]    // 支持使用 Debug/Trace 静态类打印

/// <summary>
///     测试管道启动类
/// </summary>
public sealed class TestPipelineStartup : ITestPipelineStartup
{
    private IHost _host;

    /// <inheritdoc />
    public async ValueTask StartAsync(IMessageSink diagnosticMessageSink)
    {
        // 初始化 Furion
        _host = await Serve.RunNativeAsync(services =>
        {
            services.AddHttpRemote();
        }, urls: Serve.IdleHost.Urls);

        // 配置单元测试类型依赖注入激活器
        TypeActivator.Current = new DependencyInjectionTypeActivator(_host.Services);
    }

    /// <inheritdoc />
    public async ValueTask StopAsync() => await _host.StopAsync();
}