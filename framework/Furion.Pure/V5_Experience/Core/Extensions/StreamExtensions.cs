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

namespace Furion.Extensions;

/// <summary>
///     <see cref="Stream" /> 扩展类
/// </summary>
public static class StreamExtensions
{
    /// <summary>
    ///     将字节数组写入本地文件
    /// </summary>
    /// <param name="data">字节数组</param>
    /// <param name="filePath">目标文件路径</param>
    /// <param name="cancellationToken">
    ///     <see cref="CancellationToken" />
    /// </param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static async Task SaveToFileAsync(this byte[] data, string filePath,
        CancellationToken cancellationToken = default)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        // 初始化 MemoryStream 实例
        using var stream = new MemoryStream(data, false);

        await stream.SaveToFileAsync(filePath, cancellationToken);
    }

    /// <summary>
    ///     将字节数组写入本地文件
    /// </summary>
    /// <param name="data">字节数组</param>
    /// <param name="filePath">目标文件路径</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static void SaveToFile(this byte[] data, string filePath)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        // 初始化 MemoryStream 实例
        using var stream = new MemoryStream(data, false);

        stream.SaveToFile(filePath);
    }

    /// <summary>
    ///     将流从当前位置开始的内容写入本地文件
    /// </summary>
    /// <remarks>如果希望总是从流的开头保存，可提前设置：<c>stream.Seek(0, SeekOrigin.Begin);</c>。</remarks>
    /// <param name="stream">
    ///     <see cref="Stream" />
    /// </param>
    /// <param name="filePath">目标文件路径</param>
    /// <param name="cancellationToken">
    ///     <see cref="CancellationToken" />
    /// </param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static async Task SaveToFileAsync(this Stream stream, string filePath,
        CancellationToken cancellationToken = default)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        // 解析文件路径
        var resolvedPath = ResolveFilePath(filePath);

        // 获取路径的目录路径
        var directory = Path.GetDirectoryName(resolvedPath);

        // 存在检查
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            // 创建目标文件夹
            Directory.CreateDirectory(directory);
        }

        // 初始化 FileStream 实例
        await using var fileStream = new FileStream(resolvedPath, FileMode.Create, FileAccess.Write, FileShare.None,
            4096, true);

        await stream.CopyToAsync(fileStream, cancellationToken);
    }

    /// <summary>
    ///     将流从当前位置开始的内容写入本地文件
    /// </summary>
    /// <remarks>如果希望总是从流的开头保存，可提前设置：<c>stream.Seek(0, SeekOrigin.Begin);</c>。</remarks>
    /// <param name="stream">
    ///     <see cref="Stream" />
    /// </param>
    /// <param name="filePath">目标文件路径</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static void SaveToFile(this Stream stream, string filePath)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        // 解析文件路径
        var resolvedPath = ResolveFilePath(filePath);

        // 获取路径的目录路径
        var directory = Path.GetDirectoryName(resolvedPath);

        // 存在检查
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            // 创建目标文件夹
            Directory.CreateDirectory(directory);
        }

        // 初始化 FileStream 实例
        using var fileStream = new FileStream(resolvedPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096);

        stream.CopyTo(fileStream);
    }

    /// <summary>
    ///     解析文件路径
    /// </summary>
    /// <remarks>如果已是绝对路径，直接返回。如果是相对路径，基于 <see cref="AppContext.BaseDirectory" /> 解析。</remarks>
    /// <param name="filePath">文件路径</param>
    /// <returns>
    ///     <see cref="string" />
    /// </returns>
    internal static string ResolveFilePath(string filePath)
    {
        // 空检查
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        // 移除前后空格
        filePath = filePath.Trim();

        // 处理绝对和相对路径问题
        var basePath = Path.IsPathRooted(filePath)
            ? filePath
            : Path.Combine(AppContext.BaseDirectory, filePath);

        return Path.GetFullPath(basePath);
    }
}