using Furion.HttpRemote;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Furion.UnitTests;

public class SampleTests : IDisposable
{
    /// <summary>
    /// 输出日志
    /// </summary>
    private readonly ITestOutputHelper Output;

    private readonly ISystemService _sysService;
    private readonly IConfiguration _configuration;
    private readonly IHttpRemoteService _httpRemoteService;

    public SampleTests(ITestOutputHelper tempOutput
        , ISystemService sysService
        , IConfiguration configuration
        , IHttpRemoteService httpRemoteService)
    {
        Output = tempOutput;
        _sysService = sysService;
        _configuration = configuration;
        _httpRemoteService = httpRemoteService;
    }

    [Fact]
    public void TestRootService()
    {
        Assert.NotNull(App.RootServices);
    }

    [Fact]
    public void Test_String_Equal()
    {
        Output.WriteLine("输出一条日志");
        Assert.NotEqual("Furion", "Fur");
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    public void Test_Numbers_Is_Odd(int value)
    {
        Assert.True(IsOdd(value));
    }

    [Fact]
    public void Test_Dependency_Injection()
    {
        Assert.Equal("Furion", _sysService.GetName());
        Console.WriteLine("测试 Console 打印");
    }

    private static bool IsOdd(int value)
    {
        return value % 2 == 1;
    }

    [Fact]
    public async Task TestBaidu()
    {
        var res = await _httpRemoteService.GetAsync("https://www.baidu.com");
        Assert.True(res.IsSuccessStatusCode);
    }

    public void Dispose()
    {
        Console.WriteLine("测试自动释放");
    }
}