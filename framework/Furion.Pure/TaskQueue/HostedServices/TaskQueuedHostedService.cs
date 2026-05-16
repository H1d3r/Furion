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

using Furion.FriendlyException;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Logging;

namespace Furion.TaskQueue;

/// <summary>
/// 任务队列后台主机服务
/// </summary>
/// <remarks>用于长时间监听任务项入队后进行出队调用</remarks>
internal sealed class TaskQueueHostedService : BackgroundService
{
    /// <summary>
    /// 避免由 CLR 的终结器捕获该异常从而终止应用程序，让所有未觉察异常被觉察
    /// </summary>
    internal event EventHandler<UnobservedTaskExceptionEventArgs> UnobservedTaskException;

    /// <summary>
    /// 日志对象
    /// </summary>
    private readonly ILogger<TaskQueueService> _logger;

    /// <summary>
    /// 服务提供器
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 后台任务队列
    /// </summary>
    private readonly ITaskQueue _taskQueue;

    /// <summary>
    /// 是否采用并行执行
    /// </summary>
    private readonly bool _concurrent;

    /// <summary>
    /// 重试次数
    /// </summary>
    private readonly int _numRetries;

    /// <summary>
    /// 重试间隔
    /// </summary>
    private readonly int _retryTimeout;

    /// <summary>
    /// 追踪当前正在运行的并行任务
    /// </summary>
    private readonly ConcurrentBag<Task> _runningTasks = [];

    /// <summary>
    /// 同步任务队列（线程安全）
    /// </summary>
    private readonly BlockingCollection<TaskWrapper> _syncTaskWrapperQueue = new(12000);

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志对象</param>
    /// <param name="serviceProvider">服务提供器</param>
    /// <param name="taskQueue">后台任务队列</param>
    /// <param name="concurrent">是否采用并行执行</param>
    /// <param name="numRetries">重试次数</param>
    /// <param name="retryTimeout">重试间隔</param>
    public TaskQueueHostedService(ILogger<TaskQueueService> logger
        , IServiceProvider serviceProvider
        , ITaskQueue taskQueue
        , bool concurrent
        , int numRetries
        , int retryTimeout)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _taskQueue = taskQueue;
        _concurrent = concurrent;
        _numRetries = numRetries;
        _retryTimeout = retryTimeout;
    }

    /// <summary>
    /// 执行后台任务
    /// </summary>
    /// <param name="stoppingToken">后台主机服务停止时取消任务 Token</param>
    /// <returns>Task</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TaskQueue hosted service is running.");

        // 注册后台主机服务停止监听
        stoppingToken.Register(() => _logger.LogDebug($"TaskQueue hosted service is stopping."));

        // 创建关联取消 Token
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var cancellationToken = linkedCts.Token;

        // 启动一个独立的任务来处理同步队列中的任务
        var serialQueueTask = Task.Run(async () => await ProcessQueueAsync(cancellationToken), cancellationToken);

        try
        {
            // 监听服务是否取消
            while (!cancellationToken.IsCancellationRequested)
            {
                // 执行具体任务
                await BackgroundProcessing(cancellationToken);
            }
        }
        finally
        {
            // 确保串行队列任务被取消
            _syncTaskWrapperQueue.CompleteAdding();

            // 取消内部令牌通知串行队列任务
            linkedCts.Cancel();

            // 等待串行队列任务完成（最多 1.5 秒）
            try
            {
                await Task.WhenAny(serialQueueTask, Task.Delay(TimeSpan.FromMilliseconds(1500), cancellationToken));
            }
            catch { }
        }

        _logger.LogWarning($"TaskQueue hosted service is stopped.");
    }

    /// <summary>
    /// 后台调用处理程序
    /// </summary>
    /// <param name="stoppingToken">后台主机服务停止时取消任务 Token</param>
    /// <returns><see cref="Task"/></returns>
    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        // 出队
        var taskWrapper = await _taskQueue.DequeueAsync(stoppingToken);

        // 空检查
        if (taskWrapper is null)
        {
            return;
        }

        // 获取任务执行策略
        var concurrent = taskWrapper.Concurrent == null
            ? _concurrent
            : taskWrapper.Concurrent.Value;

        // 并行执行
        if (concurrent)
        {
            // 添加待执行的任务程序
            var task = DequeueHandleAsync(taskWrapper, stoppingToken);
            _runningTasks.Add(task);

            // 任务完成后自动从集合中移除
            _ = task.ContinueWith(t => _runningTasks.TryTake(out _), TaskContinuationOptions.ExecuteSynchronously);
        }
        else
        {
            // 只有队列可持续入队才写入
            if (!_syncTaskWrapperQueue.IsAddingCompleted)
            {
                try
                {
                    _syncTaskWrapperQueue.TryAdd(taskWrapper, 0, stoppingToken);
                }
                catch (InvalidOperationException) { }
                catch { }
            }
        }
    }

    /// <summary>
    /// 同步队列出队
    /// </summary>
    /// <param name="stoppingToken">后台主机服务停止时取消任务 Token</param>
    /// <returns><see cref="Task"/></returns>
    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        foreach (var taskWrapper in _syncTaskWrapperQueue.GetConsumingEnumerable(stoppingToken))
        {
            await DequeueHandleAsync(taskWrapper, stoppingToken);
        }
    }

    /// <summary>
    /// 出队调用处理程序
    /// </summary>
    /// <param name="taskWrapper">任务包装器</param>
    /// <param name="stoppingToken">后台主机服务停止时取消任务 Token</param>
    /// <returns><see cref="Task"/></returns>
    private async Task DequeueHandleAsync(TaskWrapper taskWrapper, CancellationToken stoppingToken)
    {
        try
        {
            // 判断是否禁用重试（解决全局配置问题）
            if (!taskWrapper.DisableRetry)
            {
                // 调用任务处理程序并配置出错执行重试
                await Retry.InvokeAsync(async () =>
                {
                    // 调用任务处理委托
                    await taskWrapper.Handler(_serviceProvider, stoppingToken);
                }
                , _numRetries
                , _retryTimeout
                , retryAction: (total, times) =>
                {
                    // 输出重试日志
                    _logger.LogWarning("Retrying {times}/{total} times for {TaskHandler}", times, total, taskWrapper.Handler?.ToString());
                });
            }
            else
            {
                // 调用任务处理委托
                await taskWrapper.Handler(_serviceProvider, stoppingToken);
            }

            // 触发任务队列事件
            _taskQueue.InvokeEvents(new(taskWrapper.TaskId, taskWrapper.Channel, true));
        }
        catch (Exception ex)
        {
            // 输出异常日志
            _logger.LogError(ex, "Error occurred executing in {TaskHandler}.", taskWrapper.Handler?.ToString());

            // 捕获 Task 任务异常信息并统计所有异常
            if (UnobservedTaskException != default)
            {
                var args = new UnobservedTaskExceptionEventArgs(
                    ex as AggregateException ?? new AggregateException(ex));

                UnobservedTaskException.Invoke(this, args);
            }

            // 触发任务队列事件
            _taskQueue.InvokeEvents(new(taskWrapper.TaskId, taskWrapper.Channel, false)
            {
                Exception = ex
            });
        }
        finally
        {
            taskWrapper = null;
        }
    }

    /// <summary>
    /// 监听任务队列服务服务停止
    /// </summary>
    /// <param name="cancellationToken">后台主机服务停止时取消任务 Token</param>
    /// <returns><see cref="Task"/></returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TaskQueue hosted service is stopping...");

        // 等待正在运行的并行任务完成
        if (!_runningTasks.IsEmpty)
        {
            var tasks = _runningTasks.ToArray();
            _logger.LogInformation("Waiting for {Count} running tasks to complete before shutdown...", tasks.Length);

            // 最多等待 1.5 秒
            var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(1500), cancellationToken);
            var whenAllTask = Task.WhenAll(tasks);

            var completedTask = await Task.WhenAny(whenAllTask, timeoutTask);
            if (completedTask == timeoutTask)
            {
                _logger.LogWarning("Shutdown timeout reached. Some tasks may be terminated abruptly.");
            }
            else
            {
                _logger.LogInformation("All running tasks completed.");
            }
        }

        await base.StopAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        base.Dispose();

        // 标记同步任务队列停止写入
        _syncTaskWrapperQueue.CompleteAdding();
    }
}