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

namespace Furion.Utilities;

/// <summary>
///     多线程安全的节流器
/// </summary>
/// <remarks>控制操作执行的最小时间间隔。</remarks>
public sealed class Throttler
{
    /// <summary>
    ///     记录上一次允许执行的时间戳
    /// </summary>
    /// <remarks>单位为毫秒。</remarks>
    internal long _intervalMs;

    /// <summary>
    ///     节流间隔时间
    /// </summary>
    /// <remarks>单位为毫秒。</remarks>
    internal long _lastTick;

    /// <summary>
    ///     <inheritdoc cref="Throttler" />
    /// </summary>
    /// <param name="interval">两次允许执行的最小时间间隔</param>
    public Throttler(TimeSpan interval) => _intervalMs = (long)interval.TotalMilliseconds;

    /// <summary>
    ///     当前节流间隔
    /// </summary>
    public TimeSpan Interval
    {
        get => TimeSpan.FromMilliseconds(Volatile.Read(ref _intervalMs));
        set => Volatile.Write(ref _intervalMs, (long)value.TotalMilliseconds);
    }

    /// <summary>
    ///     判断当前是否允许执行操作
    /// </summary>
    /// <returns>
    ///     <see cref="bool" />
    /// </returns>
    public bool TryEnter()
    {
        // 获取当前配置的间隔时间（毫秒）
        var intervalMs = Volatile.Read(ref _intervalMs);

        // 检查是否不限制频率
        if (intervalMs <= 0)
        {
            return true;
        }

        // 获取当前系统运行时间戳
        var now = Environment.TickCount64;

        // 获取最近一次允许执行的时间戳
        // Volatile.Read 确保读取到的是其他线程写入的最新值（内存可见性）
        var last = Volatile.Read(ref _lastTick);

        // 检查是否是首次调用或间隔时间已到
        // ReSharper disable once InvertIf
        if (last == 0 || now - last >= intervalMs)
        {
            // CAS 原子操作，更新最近一次允许执行的时间戳
            // 返回值 == last 表示当前线程成功抢占，获得本次执行权限
            if (Interlocked.CompareExchange(ref _lastTick, now, last) == last)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     重置节流状态
    /// </summary>
    /// <remarks>下次调用立即允许执行。</remarks>
    public void Reset() => Interlocked.Exchange(ref _lastTick, 0);

    /// <summary>
    ///     获取距离下次允许执行还需等待的毫秒数
    /// </summary>
    /// <returns>
    ///     <see cref="long" />
    /// </returns>
    public long GetRemainingMilliseconds()
    {
        // 获取当前配置的间隔时间（毫秒）
        var intervalMs = Volatile.Read(ref _intervalMs);

        // 检查是否不限制频率
        if (intervalMs <= 0)
        {
            return 0;
        }

        // 获取当前系统运行时间戳
        var now = Environment.TickCount64;

        // 获取最近一次允许执行的时间戳
        // Volatile.Read 确保读取到的是其他线程写入的最新值（内存可见性）
        var last = Volatile.Read(ref _lastTick);

        // 如果是首次调用，立即可执行
        if (last == 0)
        {
            return 0;
        }

        // 计算已过去的时间
        var elapsed = now - last;

        // 如果已到间隔，返回 0；否则返回剩余等待时间
        return elapsed >= intervalMs ? 0 : intervalMs - elapsed;
    }
}