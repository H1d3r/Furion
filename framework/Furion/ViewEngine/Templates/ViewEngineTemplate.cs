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

using Furion.Extensions;

namespace Furion.ViewEngine;

/// <summary>
/// 视图引擎模板（编译后）
/// </summary>
public class ViewEngineTemplate : IViewEngineTemplate
{
    /// <summary>
    /// 程序集字节码
    /// </summary>
    private readonly byte[] _assemblyBytes;

    /// <summary>
    /// 模板类型
    /// </summary>
    private readonly Type _templateType;

    /// <summary>
    /// 是否已释放
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="assemblyBytes">程序集字节数组</param>
    /// <param name="templateType">模板类型</param>
    internal ViewEngineTemplate(byte[] assemblyBytes, Type templateType)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(assemblyBytes);
        ArgumentNullException.ThrowIfNull(templateType);

        _assemblyBytes = assemblyBytes;
        _templateType = templateType;
    }

    /// <summary>
    /// 保存到流中
    /// </summary>
    /// <param name="stream"></param>
    public void SaveToStream(Stream stream)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ViewEngineTemplate));

        stream.Write(_assemblyBytes, 0, _assemblyBytes.Length);
    }

    /// <summary>
    /// 保存到流中
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public Task SaveToStreamAsync(Stream stream)
    {
        SaveToStream(stream);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 保存到文件
    /// </summary>
    /// <param name="fullName"></param>
    public void SaveToFile(string fullName)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ViewEngineTemplate));

        File.WriteAllBytes(fullName, _assemblyBytes);
    }

    /// <summary>
    /// 保存到文件
    /// </summary>
    /// <param name="fullName"></param>
    /// <returns></returns>
    public Task SaveToFileAsync(string fullName)
    {
        SaveToFile(fullName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 执行编译
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public string Run(object model = null)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ViewEngineTemplate));

        if (model != null && model.IsAnonymous())
        {
            model = new AnonymousTypeWrapper(model);
        }

        var instance = (IViewEngineModel)Activator.CreateInstance(_templateType);
        instance.Model = model;

        instance.Execute();
        return instance.Result();
    }

    /// <summary>
    /// 执行编译
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<string> RunAsync(object model = null)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ViewEngineTemplate));

        if (model != null && model.IsAnonymous())
        {
            model = new AnonymousTypeWrapper(model);
        }

        var instance = (IViewEngineModel)Activator.CreateInstance(_templateType);
        instance.Model = model;

        await instance.ExecuteAsync();
        return await instance.ResultAsync();
    }

    /// <summary>
    /// 从文件中加载模板
    /// </summary>
    /// <param name="fullName"></param>
    /// <returns></returns>
    public static IViewEngineTemplate LoadFromFile(string fullName)
    {
        var bytes = File.ReadAllBytes(fullName);
        var type = Penetrates.LoadTemplateType(bytes);
        return new ViewEngineTemplate(bytes, type);
    }

    /// <summary>
    /// 从文件中加载模板
    /// </summary>
    /// <param name="fullName"></param>
    /// <returns></returns>
    public static async Task<IViewEngineTemplate> LoadFromFileAsync(string fullName)
    {
        var bytes = await File.ReadAllBytesAsync(fullName);
        var type = Penetrates.LoadTemplateType(bytes);
        return new ViewEngineTemplate(bytes, type);
    }

    /// <summary>
    /// 从流中加载模板
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static IViewEngineTemplate LoadFromStream(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();
        var type = Penetrates.LoadTemplateType(bytes);
        return new ViewEngineTemplate(bytes, type);
    }

    /// <summary>
    /// 从流中加载模板
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static async Task<IViewEngineTemplate> LoadFromStreamAsync(Stream stream)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var bytes = ms.ToArray();
        var type = Penetrates.LoadTemplateType(bytes);
        return new ViewEngineTemplate(bytes, type);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _disposed = true;
    }
}

/// <summary>
/// 视图引擎模板（编译后）
/// </summary>
/// <typeparam name="T"></typeparam>
public class ViewEngineTemplate<T> : IViewEngineTemplate<T>
    where T : IViewEngineModel
{
    /// <summary>
    /// 程序集字节码
    /// </summary>
    private readonly byte[] _assemblyBytes;

    /// <summary>
    /// 模板类型
    /// </summary>
    private readonly Type _templateType;

    /// <summary>
    /// 是否已释放
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="assemblyBytes">程序集字节数组</param>
    /// <param name="templateType">模板类型</param>
    internal ViewEngineTemplate(byte[] assemblyBytes, Type templateType)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(assemblyBytes);
        ArgumentNullException.ThrowIfNull(templateType);

        _assemblyBytes = assemblyBytes;
        _templateType = templateType;
    }

    /// <summary>
    /// 保存到流中
    /// </summary>
    /// <param name="stream"></param>
    public void SaveToStream(Stream stream)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ViewEngineTemplate<T>));

        stream.Write(_assemblyBytes, 0, _assemblyBytes.Length);
    }

    /// <summary>
    /// 保存到流中
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public Task SaveToStreamAsync(Stream stream)
    {
        SaveToStream(stream);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 保存到文件中
    /// </summary>
    /// <param name="fullName"></param>
    public void SaveToFile(string fullName)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ViewEngineTemplate<T>));

        File.WriteAllBytes(fullName, _assemblyBytes);
    }

    /// <summary>
    /// 保存到文件中
    /// </summary>
    /// <param name="fullName"></param>
    /// <returns></returns>
    public Task SaveToFileAsync(string fullName)
    {
        SaveToFile(fullName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 执行编译
    /// </summary>
    /// <param name="initializer"></param>
    /// <returns></returns>
    public string Run(Action<T> initializer)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ViewEngineTemplate<T>));

        var instance = (T)Activator.CreateInstance(_templateType);
        initializer(instance);

        instance.Execute();
        return instance.Result();
    }

    /// <summary>
    /// 执行编译
    /// </summary>
    /// <param name="initializer"></param>
    /// <returns></returns>
    public async Task<string> RunAsync(Action<T> initializer)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ViewEngineTemplate<T>));

        var instance = (T)Activator.CreateInstance(_templateType);
        initializer(instance);

        await instance.ExecuteAsync();
        return await instance.ResultAsync();
    }

    /// <summary>
    /// 从文件中加载模板
    /// </summary>
    /// <param name="fullName"></param>
    /// <returns></returns>
    public static IViewEngineTemplate<T> LoadFromFile(string fullName)
    {
        var bytes = File.ReadAllBytes(fullName);
        var type = Penetrates.LoadTemplateType(bytes);
        return new ViewEngineTemplate<T>(bytes, type);
    }

    /// <summary>
    /// 从文件中加载模板
    /// </summary>
    /// <param name="fullName"></param>
    /// <returns></returns>
    public static async Task<IViewEngineTemplate<T>> LoadFromFileAsync(string fullName)
    {
        var bytes = await File.ReadAllBytesAsync(fullName);
        var type = Penetrates.LoadTemplateType(bytes);
        return new ViewEngineTemplate<T>(bytes, type);
    }

    /// <summary>
    /// 从流中加载模板
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static IViewEngineTemplate<T> LoadFromStream(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();
        var type = Penetrates.LoadTemplateType(bytes);
        return new ViewEngineTemplate<T>(bytes, type);
    }

    /// <summary>
    /// 从流中加载模板
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static async Task<IViewEngineTemplate<T>> LoadFromStreamAsync(Stream stream)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var bytes = ms.ToArray();
        var type = Penetrates.LoadTemplateType(bytes);
        return new ViewEngineTemplate<T>(bytes, type);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _disposed = true;
    }
}