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

namespace Furion.Schedule;

/// <summary>
/// 毫秒周期（间隔）作业触发器
/// </summary>
public class PeriodTrigger : Trigger
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="interval">间隔（毫秒）</param>
    public PeriodTrigger(long interval)
    {
        // 最低运行毫秒数为 100ms
        if (interval < 100) throw new InvalidOperationException($"The interval cannot be less than 100ms, but the value is <{interval}ms>.");

        Interval = interval;
    }

    /// <summary>
    /// 间隔（毫秒）
    /// </summary>
    protected long Interval { get; }

    /// <summary>
    /// 计算下一个触发时间
    /// </summary>
    /// <param name="startAt">起始时间</param>
    /// <returns><see cref="DateTime"/></returns>
    public override DateTime? GetNextOccurrence(DateTime startAt)
    {
        // 获取间隔触发器周期计算基准时间
        var baseTime = StartTime == null ? startAt : StartTime.Value;

        // 处理基准时间大于当前时间
        if (baseTime > startAt)
        {
            return baseTime;
        }

        // 获取从基准时间开始到现在经过了多少个完整周期
        var elapsedMilliseconds = (startAt - baseTime).Ticks / TimeSpan.TicksPerMillisecond;
        var fullPeriods = elapsedMilliseconds / Interval;

        // 获取下一次执行时间
        var nextRunTime = baseTime.AddMilliseconds(fullPeriods * Interval);

        // 确保下一次执行时间是在当前时间之后
        if (startAt >= nextRunTime)
        {
            nextRunTime = nextRunTime.AddMilliseconds(Interval);
        }

        return nextRunTime;
    }

    /// <summary>
    /// 作业触发器转字符串输出
    /// </summary>
    /// <returns><see cref="string"/></returns>
    public override string ToString()
    {
        return $"<{JobId} {TriggerId}> {FormatDuration(Interval)}{(string.IsNullOrWhiteSpace(Description) ? string.Empty : $" {Description.GetMaxLengthString()}")} {NumberOfRuns}ts";
    }

    /// <summary>
    /// 将毫秒数格式化为更直观的时间单位字符串（如 ms, s, m, h, d, y）
    /// </summary>
    /// <param name="ms">毫秒</param>
    /// <returns><see cref="string"/></returns>
    private static string FormatDuration(long ms)
    {
        if (ms < 0) return "-" + FormatDuration(-ms);
        if (ms < 1000) return $"{ms}ms";

        var (value, unit) = ms switch
        {
            < 60_000 => (ms / 1000.0, "s"),
            < 3_600_000 => (ms / 60_000.0, "m"),
            < 86_400_000 => (ms / 3_600_000.0, "h"),
            < 31_536_000_000L => (ms / 86_400_000.0, "d"),
            _ => (ms / 31_536_000_000.0, "y")
        };

        return FormatValue(value, unit);
    }

    /// <summary>
    /// 格式化数值为指定单位的字符串表示
    /// </summary>
    /// <param name="value">数值</param>
    /// <param name="unit">单位</param>
    /// <returns><see cref="string"/></returns>
    private static string FormatValue(double value, string unit)
    {
        var rounded = Math.Round(value, 1);
        var isInteger = Math.Abs(rounded % 1) < 0.0001;
        return isInteger ? $"{rounded:F0}{unit}" : $"{rounded:F1}{unit}";
    }
}