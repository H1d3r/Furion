using Furion.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

[assembly: TestPipelineStartup(typeof(TestPipelineStartup))]
[assembly: CaptureConsole]
[assembly: CaptureTrace]

/// <summary>
///     测试管道启动类
/// </summary>
public sealed class TestPipelineStartup : ITestPipelineStartup
{
    private IHost _host;

    /// <inheritdoc />
    public async ValueTask StartAsync(IMessageSink diagnosticMessageSink)
    {
        _host = await Serve.RunNativeAsync(services =>
        {
            services.AddHttpRemote();
        });

        TypeActivator.Current = new DependencyInjectionTypeActivator(_host.Services);
    }

    /// <inheritdoc />
    public async ValueTask StopAsync() => await _host.StopAsync();
}