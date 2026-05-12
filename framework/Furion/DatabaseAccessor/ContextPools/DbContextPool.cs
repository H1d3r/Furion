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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Transactions;

namespace Furion.DatabaseAccessor;

/// <summary>
/// 数据库上下文池
/// </summary>
public class DbContextPool : IDbContextPool, IDisposable, IAsyncDisposable
{
    /// <summary>
    /// 线程安全的数据库上下文集合
    /// </summary>
    private readonly ConcurrentDictionary<Guid, DbContext> _dbContexts;

    /// <summary>
    /// 登记错误的数据库上下文
    /// </summary>
    private readonly ConcurrentDictionary<Guid, DbContext> _failedDbContexts;

    /// <summary>
    /// 服务提供器
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 事件处理程序引用
    /// </summary>
    private readonly ConcurrentDictionary<Guid, EventHandler<SaveChangesFailedEventArgs>> _eventHandlers;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider"></param>
    public DbContextPool(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _dbContexts = new ConcurrentDictionary<Guid, DbContext>();
        _failedDbContexts = new ConcurrentDictionary<Guid, DbContext>();
        _eventHandlers = new ConcurrentDictionary<Guid, EventHandler<SaveChangesFailedEventArgs>>();
    }

    /// <summary>
    /// 数据库上下文事务
    /// </summary>
    public IDbContextTransaction DbContextTransaction { get; private set; }

    /// <summary>
    /// 获取所有数据库上下文
    /// </summary>
    /// <returns></returns>
    public ConcurrentDictionary<Guid, DbContext> GetDbContexts()
    {
        return _dbContexts;
    }

    /// <summary>
    /// 保存数据库上下文
    /// </summary>
    /// <param name="dbContext"></param>
    public void AddToPool(DbContext dbContext)
    {
        // 跳过非关系型数据库
        if (!dbContext.Database.IsRelational()) return;

        var instanceId = dbContext.ContextId.InstanceId;
        if (!_dbContexts.TryAdd(instanceId, dbContext)) return;

        // 订阅数据库上下文操作失败事件
        EventHandler<SaveChangesFailedEventArgs> handler = (s, e) =>
        {
            // 排除已经存在的数据库上下文
            if (!_failedDbContexts.TryAdd(instanceId, dbContext)) return;

            // 当前事务
            dynamic context = s as DbContext;
            var database = context.Database as DatabaseFacade;
            var currentTransaction = database?.CurrentTransaction;

            // 只有事务不等于空且支持自动回滚
            if (!(currentTransaction != null && context.FailedAutoRollback == true)) return;

            // 回滚事务
            currentTransaction?.Rollback();
        };

        // 订阅事件并缓存处理程序引用
        dbContext.SaveChangesFailed += handler;
        _eventHandlers.TryAdd(instanceId, handler);
    }

    /// <summary>
    /// 保存数据库上下文
    /// </summary>
    /// <param name="dbContext"></param>
    public Task AddToPoolAsync(DbContext dbContext)
    {
        AddToPool(dbContext);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 保存数据库上下文池中所有已更改的数据库上下文
    /// </summary>
    /// <returns></returns>
    public int SavePoolNow()
    {
        // 查找所有已改变的数据库上下文并保存更改
        return _dbContexts
            .Where(u => u.Value != null && !GetDisposedRef(u.Value) && u.Value.ChangeTracker.HasChanges() && !_failedDbContexts.ContainsKey(u.Key))
            .Select(u => u.Value.SaveChanges()).Sum();
    }

    /// <summary>
    /// 保存数据库上下文池中所有已更改的数据库上下文
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"></param>
    /// <returns></returns>
    public int SavePoolNow(bool acceptAllChangesOnSuccess)
    {
        // 查找所有已改变的数据库上下文并保存更改
        return _dbContexts
            .Where(u => u.Value != null && !GetDisposedRef(u.Value) && u.Value.ChangeTracker.HasChanges() && !_failedDbContexts.ContainsKey(u.Key))
            .Select(u => u.Value.SaveChanges(acceptAllChangesOnSuccess)).Sum();
    }

    /// <summary>
    /// 保存数据库上下文池中所有已更改的数据库上下文
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<int> SavePoolNowAsync(CancellationToken cancellationToken = default)
    {
        // 查找所有已改变的数据库上下文并保存更改
        var tasks = _dbContexts
            .Where(u => u.Value != null && !GetDisposedRef(u.Value) && u.Value.ChangeTracker.HasChanges() && !_failedDbContexts.ContainsKey(u.Key))
            .Select(u => u.Value.SaveChangesAsync(cancellationToken));

        // 等待所有异步完成
        var results = await Task.WhenAll(tasks);
        return results.Sum();
    }

    /// <summary>
    /// 保存数据库上下文池中所有已更改的数据库上下文（异步）
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<int> SavePoolNowAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        // 查找所有已改变的数据库上下文并保存更改
        var tasks = _dbContexts
            .Where(u => u.Value != null && !GetDisposedRef(u.Value) && u.Value.ChangeTracker.HasChanges() && !_failedDbContexts.ContainsKey(u.Key))
            .Select(u => u.Value.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken));

        // 等待所有异步完成
        var results = await Task.WhenAll(tasks);
        return results.Sum();
    }

    /// <summary>
    /// 打开事务
    /// </summary>
    /// <param name="ensureTransaction">是否确保事务强制可用</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task BeginTransactionAsync(bool ensureTransaction = false, CancellationToken cancellationToken = default)
    {
        // 判断是否启用了分布式环境事务，如果是，则跳过
        if (Transaction.Current != null) return;

        // 判断 dbContextPool 中是否包含DbContext，如果是，则使用第一个数据库上下文开启事务，并应用于其他数据库上下文
    EnsureTransaction: if (!_dbContexts.IsEmpty)
        {
            // 如果共享事务不为空，则直接共享
            if (DbContextTransaction != null) goto ShareTransaction;

            // 先判断是否已经有上下文开启了事务
            var transactionDbContext = _dbContexts.FirstOrDefault(u => u.Value.Database.CurrentTransaction != null);

            DbContextTransaction = transactionDbContext.Value != null
                   ? transactionDbContext.Value.Database.CurrentTransaction
                   // 如果没有任何上下文有事务，则将第一个开启事务
                   : await _dbContexts.First().Value.Database.BeginTransactionAsync(cancellationToken);

            // 共享事务
        ShareTransaction: await ShareTransactionAsync(DbContextTransaction.GetDbTransaction(), cancellationToken);
        }
        else
        {
            // 判断是否确保事务强制可用（此处是无奈之举）
            if (!ensureTransaction) return;

            var defaultDbContextLocator = Penetrates.DbContextDescriptors.LastOrDefault();
            if (defaultDbContextLocator.Key == null) return;

            // 创建一个新的上下文
            var newDbContext = Db.GetDbContext(defaultDbContextLocator.Key, _serviceProvider);
            DbContextTransaction = await newDbContext.Database.BeginTransactionAsync(cancellationToken);
            goto EnsureTransaction;
        }
    }

    /// <summary>
    /// 提交事务
    /// </summary>
    /// <param name="withCloseAll">是否自动关闭所有连接</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task CommitTransactionAsync(bool withCloseAll = false, CancellationToken cancellationToken = default)
    {
        // 判断是否启用了分布式环境事务，如果是，则跳过
        if (Transaction.Current != null) return;

        try
        {
            // 将所有数据库上下文修改 SaveChangesAsync();，这里另外判断是否需要手动提交
            var hasChangesCount = await SavePoolNowAsync(cancellationToken);

            // 如果事务为空，则执行完毕后关闭连接
            if (DbContextTransaction == null)
            {
                if (withCloseAll) await CloseAllAsync();
                return;
            }

            // 提交共享事务
            if (DbContextTransaction != null)
            {
                await DbContextTransaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            // 回滚事务
            if (DbContextTransaction?.GetDbTransaction()?.Connection != null)
            {
                await DbContextTransaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
        finally
        {
            if (DbContextTransaction?.GetDbTransaction()?.Connection != null)
            {
                await DbContextTransaction.DisposeAsync();
                DbContextTransaction = null;
            }
        }

        // 关闭所有连接
        if (withCloseAll) await CloseAllAsync();
    }

    /// <summary>
    /// 回滚事务
    /// </summary>
    /// <param name="withCloseAll">是否自动关闭所有连接</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task RollbackTransactionAsync(bool withCloseAll = false, CancellationToken cancellationToken = default)
    {
        // 判断是否启用了分布式环境事务，如果是，则跳过
        if (Transaction.Current != null) return;

        // 回滚事务
        if (DbContextTransaction?.GetDbTransaction()?.Connection != null)
        {
            await DbContextTransaction.RollbackAsync(cancellationToken);
        }

        if (DbContextTransaction != null)
        {
            await DbContextTransaction.DisposeAsync();
            DbContextTransaction = null;
        }

        // 关闭所有连接
        if (withCloseAll) await CloseAllAsync();
    }

    /// <summary>
    /// 释放所有数据库上下文
    /// </summary>
    public async Task CloseAllAsync()
    {
        if (_dbContexts.IsEmpty) return;

        var tasks = new List<Task>();

        foreach (var item in _dbContexts)
        {
            if (GetDisposedRef(item.Value)) continue;

            var conn = item.Value.Database.GetDbConnection();
            if (conn == null || conn.State != ConnectionState.Open) continue;

            tasks.Add(conn.CloseAsync());
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// 设置数据库上下文共享事务
    /// </summary>
    /// <param name="transaction">数据库事务</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    private async Task ShareTransactionAsync(DbTransaction transaction, CancellationToken cancellationToken = default)
    {
        // 跳过第一个数据库上下文并设置共享事务
        var tasks = _dbContexts
            .Where(u => u.Value != null && !GetDisposedRef(u.Value) && ((dynamic)u.Value).UseUnitOfWork == true && u.Value.Database.CurrentTransaction == null)
            .Select(u => u.Value.Database.UseTransactionAsync(transaction, cancellationToken));

        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// 释放所有上下文
    /// </summary>
    public void Dispose()
    {
        // 取消所有事件订阅
        foreach (var kvp in _eventHandlers)
        {
            if (_dbContexts.TryGetValue(kvp.Key, out var dbContext) && dbContext != null)
            {
                dbContext.SaveChangesFailed -= kvp.Value;
            }
        }
        _eventHandlers.Clear();

        // 释放所有数据库上下文
        foreach (var item in _dbContexts)
        {
            try
            {
                item.Value?.Dispose();
            }
            catch { }
        }
        _dbContexts.Clear();
        _failedDbContexts.Clear();

        // 释放事务资源
        if (DbContextTransaction != null)
        {
            try
            {
                DbContextTransaction.Dispose();
            }
            catch { }
            DbContextTransaction = null;
        }
    }

    /// <summary>
    /// 释放所有上下文
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        // 取消所有事件订阅
        foreach (var kvp in _eventHandlers)
        {
            if (_dbContexts.TryGetValue(kvp.Key, out var dbContext) && dbContext != null)
            {
                dbContext.SaveChangesFailed -= kvp.Value;
            }
        }
        _eventHandlers.Clear();

        // 释放所有数据库上下文
        var disposeTasks = new List<ValueTask>();
        foreach (var item in _dbContexts)
        {
            try
            {
                if (item.Value is IAsyncDisposable asyncDisposable)
                {
                    disposeTasks.Add(asyncDisposable.DisposeAsync());
                }
                else
                {
                    item.Value?.Dispose();
                }
            }
            catch { }
        }

        if (disposeTasks.Count > 0)
        {
            await Task.WhenAll(disposeTasks.Select(t => t.AsTask()));
        }

        _dbContexts.Clear();
        _failedDbContexts.Clear();

        // 释放事务资源
        if (DbContextTransaction != null)
        {
            try
            {
                await DbContextTransaction.DisposeAsync();
            }
            catch { }
            DbContextTransaction = null;
        }
    }

    /// <summary>
    /// 判断数据库上下文是否释放
    /// </summary>
    /// <param name="dbContext"></param>
    /// <returns></returns>
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_disposed")]
    private static extern ref bool GetDisposedRef(DbContext dbContext);
}