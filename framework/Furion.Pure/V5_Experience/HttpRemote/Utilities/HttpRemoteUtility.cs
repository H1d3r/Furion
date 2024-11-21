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

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Furion.HttpRemote;

/// <summary>
///     提供 HTTP 远程请求实用方法
/// </summary>
public static class HttpRemoteUtility
{
    /// <summary>
    ///     获取所有支持的 SslProtocols
    /// </summary>
#pragma warning disable SYSLIB0039
#pragma warning disable CS0618 // 类型或成员已过时
    public static SslProtocols AllSslProtocols => SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Ssl2 |
                                                  SslProtocols.Ssl3 | SslProtocols.Tls12 | SslProtocols.Tls13 |
                                                  SslProtocols.None;
#pragma warning restore CS0618 // 类型或成员已过时
#pragma warning restore SYSLIB0039

    /// <summary>
    ///     忽略 SSL 证书验证
    /// </summary>
    public static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> IgnoreSslErrors =>
        (message, cert, chain, errors) => true;

    /// <summary>
    ///     获取使用 IPv4 连接到服务器的回调
    /// </summary>
    /// <param name="context">
    ///     <see cref="SocketsHttpConnectionContext" />
    /// </param>
    /// <param name="cancellationToken">
    ///     <see cref="CancellationToken" />
    /// </param>
    /// <returns>
    ///     <see cref="Stream" />
    /// </returns>
    public static ValueTask<Stream> IPv4ConnectCallback(SocketsHttpConnectionContext context,
        CancellationToken cancellationToken) =>
        IPAddressConnectCallback(AddressFamily.InterNetwork, context, cancellationToken);

    /// <summary>
    ///     获取使用 IPv6 连接到服务器的回调
    /// </summary>
    /// <param name="context">
    ///     <see cref="SocketsHttpConnectionContext" />
    /// </param>
    /// <param name="cancellationToken">
    ///     <see cref="CancellationToken" />
    /// </param>
    /// <returns>
    ///     <see cref="Stream" />
    /// </returns>
    public static ValueTask<Stream> IPv6ConnectCallback(SocketsHttpConnectionContext context,
        CancellationToken cancellationToken) =>
        IPAddressConnectCallback(AddressFamily.InterNetworkV6, context, cancellationToken);

    /// <summary>
    ///     获取使用 IPv4 或 IPv6 连接到服务器的回调
    /// </summary>
    /// <param name="context">
    ///     <see cref="SocketsHttpConnectionContext" />
    /// </param>
    /// <param name="cancellationToken">
    ///     <see cref="CancellationToken" />
    /// </param>
    /// <returns>
    ///     <see cref="Stream" />
    /// </returns>
    public static ValueTask<Stream> UnspecifiedConnectCallback(SocketsHttpConnectionContext context,
        CancellationToken cancellationToken) =>
        IPAddressConnectCallback(AddressFamily.Unspecified, context, cancellationToken);

    /// <summary>
    ///     获取使用指定 IP 地址类型连接到服务器的回调
    /// </summary>
    /// <param name="addressFamily">
    ///     <see cref="AddressFamily" />
    /// </param>
    /// <param name="context">
    ///     <see cref="SocketsHttpConnectionContext" />
    /// </param>
    /// <param name="cancellationToken">
    ///     <see cref="CancellationToken" />
    /// </param>
    /// <returns>
    ///     <see cref="Stream" />
    /// </returns>
    internal static async ValueTask<Stream> IPAddressConnectCallback(AddressFamily addressFamily,
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        // 参考文献：
        // - https://www.meziantou.net/forcing-httpclient-to-use-ipv4-or-ipv6-addresses.htm
        // - https://learn.microsoft.com/en-us/dotnet/core/runtime-config/#runtimeconfigjson

        // 使用 DNS 查找目标主机的 IP 地址：
        // - IPv4: AddressFamily.InterNetwork
        // - IPv6: AddressFamily.InterNetworkV6
        // - IPv4 或 IPv6: AddressFamily.Unspecified
        // 注意：当主机没有 IP 地址时，此方法会抛出一个 SocketException 异常
        var entry = await Dns.GetHostEntryAsync(context.DnsEndPoint.Host, addressFamily, cancellationToken);

        // 打开与目标主机/端口的连接
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

        // 关闭 Nagle 算法，因为这在大多数 HttpClient 场景中会降低性能。
        socket.NoDelay = true;

        try
        {
            await socket.ConnectAsync(entry.AddressList, context.DnsEndPoint.Port, cancellationToken);

            // 如果你想选择特定的 IP 地址来连接服务器
            // await socket.ConnectAsync(
            //    entry.AddressList[Random.Shared.Next(0, entry.AddressList.Length)],
            //    context.DnsEndPoint.Port, cancellationToken);

            // 返回 NetworkStream 给调用者
            return new NetworkStream(socket, true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}