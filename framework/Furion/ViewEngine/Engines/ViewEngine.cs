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

using Furion.DataEncryption;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Concurrent;
using System.Text;

namespace Furion.ViewEngine;

/// <summary>
/// 视图引擎实现类
/// </summary>
public class ViewEngine : IViewEngine
{
    /// <summary>
    /// Razor 引擎缓存
    /// </summary>
    private static readonly ConcurrentDictionary<string, RazorProjectEngine> _razorEngineCache = new();

    /// <summary>
    /// 编译结果缓存
    /// </summary>
    private static readonly ConcurrentDictionary<string, byte[]> _compilationCache = new();

    /// <summary>
    /// 缓存是否启用
    /// </summary>
    private static readonly bool _enableCache = Environment.GetEnvironmentVariable("FURION_VIEWENGINE_CACHE") != "false";

    /// <summary>
    /// 编译并运行
    /// </summary>
    /// <param name="content"></param>
    /// <param name="model"></param>
    /// <param name="builderAction"></param>
    /// <returns></returns>
    public string RunCompile(string content, object model = null, Action<IViewEngineOptionsBuilder> builderAction = null)
    {
        using var template = Compile(content, builderAction);
        var result = template.Run(model);
        return result;
    }

    /// <summary>
    /// 编译并运行
    /// </summary>
    /// <param name="content"></param>
    /// <param name="model"></param>
    /// <param name="builderAction"></param>
    /// <returns></returns>
    public async Task<string> RunCompileAsync(string content, object model = null, Action<IViewEngineOptionsBuilder> builderAction = null)
    {
        using var template = await CompileAsync(content, builderAction);
        var result = await template.RunAsync(model);
        return result;
    }

    /// <summary>
    /// 编译并运行
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="content"></param>
    /// <param name="model"></param>
    /// <param name="builderAction"></param>
    /// <returns></returns>
    public string RunCompile<T>(string content, T model, Action<IViewEngineOptionsBuilder> builderAction = null)
        where T : class, new()
    {
        using var template = Compile<ViewEngineModel<T>>(content, builderAction);
        var result = template.Run(u =>
        {
            u.Model = model;
        });
        return result;
    }

    /// <summary>
    /// 编译并运行
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="content"></param>
    /// <param name="model"></param>
    /// <param name="builderAction"></param>
    /// <returns></returns>
    public async Task<string> RunCompileAsync<T>(string content, T model, Action<IViewEngineOptionsBuilder> builderAction = null)
        where T : class, new()
    {
        using var template = await CompileAsync<ViewEngineModel<T>>(content, builderAction);
        var result = await template.RunAsync(u =>
        {
            u.Model = model;
        });
        return result;
    }

    /// <summary>
    /// 通过缓存解析模板
    /// </summary>
    /// <param name="content"></param>
    /// <param name="model"></param>
    /// <param name="cacheFileName"></param>
    /// <param name="builderAction"></param>
    /// <returns></returns>
    public string RunCompileFromCached(string content, object model = null, string cacheFileName = default, Action<IViewEngineOptionsBuilder> builderAction = null)
    {
        var fileName = cacheFileName ?? MD5Encryption.Encrypt(content);

        IViewEngineTemplate template = null;

        try
        {
            if (File.Exists(Penetrates.GetTemplateFileName(fileName)))
                template = ViewEngineTemplate.LoadFromFile(fileName);
            else
            {
                template = Compile(content, builderAction);
                template.SaveToFile(fileName);
            }

            var result = template.Run(model);
            return result;
        }
        finally
        {
            template?.Dispose();
        }
    }

    /// <summary>
    /// 通过缓存解析模板
    /// </summary>
    /// <param name="content"></param>
    /// <param name="model"></param>
    /// <param name="cacheFileName"></param>
    /// <param name="builderAction"></param>
    /// <returns></returns>
    public async Task<string> RunCompileFromCachedAsync(string content, object model = null, string cacheFileName = default, Action<IViewEngineOptionsBuilder> builderAction = null)
    {
        var fileName = cacheFileName ?? MD5Encryption.Encrypt(content);

        IViewEngineTemplate template = null;

        try
        {
            if (File.Exists(Penetrates.GetTemplateFileName(fileName)))
                template = await ViewEngineTemplate.LoadFromFileAsync(fileName);
            else
            {
                template = await CompileAsync(content, builderAction);
                await template.SaveToFileAsync(fileName);
            }

            var result = await template.RunAsync(model);
            return result;
        }
        finally
        {
            template?.Dispose();
        }
    }

    /// <summary>
    /// 通过缓存解析模板
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="content"></param>
    /// <param name="model"></param>
    /// <param name="cacheFileName"></param>
    /// <param name="builderAction"></param>
    /// <returns></returns>
    public string RunCompileFromCached<T>(string content, T model, string cacheFileName = default, Action<IViewEngineOptionsBuilder> builderAction = null)
        where T : class, new()
    {
        var fileName = cacheFileName ?? MD5Encryption.Encrypt(content);

        IViewEngineTemplate<ViewEngineModel<T>> template = null;

        try
        {
            if (File.Exists(Penetrates.GetTemplateFileName(fileName)))
                template = ViewEngineTemplate<ViewEngineModel<T>>.LoadFromFile(fileName);
            else
            {
                template = Compile<ViewEngineModel<T>>(content, builderAction);
                template.SaveToFile(fileName);
            }

            var result = template.Run(u =>
            {
                u.Model = model;
            });
            return result;
        }
        finally
        {
            template?.Dispose();
        }
    }

    /// <summary>
    /// 通过缓存解析模板
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="content"></param>
    /// <param name="model"></param>
    /// <param name="cacheFileName"></param>
    /// <param name="builderAction"></param>
    /// <returns></returns>
    public async Task<string> RunCompileFromCachedAsync<T>(string content, T model, string cacheFileName = default, Action<IViewEngineOptionsBuilder> builderAction = null)
        where T : class, new()
    {
        var fileName = cacheFileName ?? MD5Encryption.Encrypt(content);

        IViewEngineTemplate<ViewEngineModel<T>> template = null;

        try
        {
            if (File.Exists(Penetrates.GetTemplateFileName(fileName)))
                template = await ViewEngineTemplate<ViewEngineModel<T>>.LoadFromFileAsync(fileName);
            else
            {
                template = await CompileAsync<ViewEngineModel<T>>(content, builderAction);
                await template.SaveToFileAsync(fileName);
            }

            var result = await template.RunAsync(u =>
            {
                u.Model = model;
            });
            return result;
        }
        finally
        {
            template?.Dispose();
        }
    }

    /// <summary>
    /// 编译模板
    /// </summary>
    /// <param name="content"></param>
    /// <param name="builderAction"></param>
    /// <returns></returns>
    public IViewEngineTemplate Compile(string content, Action<IViewEngineOptionsBuilder> builderAction = null)
    {
        IViewEngineOptionsBuilder compilationOptionsBuilder = new ViewEngineOptionsBuilder();
        compilationOptionsBuilder.Inherits(typeof(ViewEngineModel));

        builderAction?.Invoke(compilationOptionsBuilder);

        // 尝试从缓存获取编译结果
        var cacheKey = _enableCache ? GenerateCacheKey(content, compilationOptionsBuilder.Options) : null;

        MemoryStream memoryStream;
        if (_enableCache && !string.IsNullOrEmpty(cacheKey) && _compilationCache.TryGetValue(cacheKey, out var cachedBytes))
        {
            memoryStream = new MemoryStream(cachedBytes);
        }
        else
        {
            memoryStream = CreateAndCompileToStream(content, compilationOptionsBuilder.Options);

            // 缓存编译结果
            if (_enableCache && !string.IsNullOrEmpty(cacheKey))
            {
                _compilationCache[cacheKey] = memoryStream.ToArray();
            }
        }

        return new ViewEngineTemplate(memoryStream);
    }

    /// <summary>
    /// 编译模板
    /// </summary>
    /// <param name="content"></param>
    /// <param name="builderAction"></param>
    /// <returns></returns>
    public async Task<IViewEngineTemplate> CompileAsync(string content, Action<IViewEngineOptionsBuilder> builderAction = null)
    {
        return await Task.Run(() => Compile(content: content, builderAction: builderAction));
    }

    /// <summary>
    /// 编译模板
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="content"></param>
    /// <param name="builderAction"></param>
    /// <returns></returns>
    public IViewEngineTemplate<T> Compile<T>(string content, Action<IViewEngineOptionsBuilder> builderAction = null)
        where T : IViewEngineModel
    {
        IViewEngineOptionsBuilder compilationOptionsBuilder = new ViewEngineOptionsBuilder();

        compilationOptionsBuilder.AddAssemblyReference(typeof(T).Assembly);
        compilationOptionsBuilder.Inherits(typeof(T));

        builderAction?.Invoke(compilationOptionsBuilder);

        // 尝试从缓存获取编译结果
        var cacheKey = _enableCache ? GenerateCacheKey(content, compilationOptionsBuilder.Options) : null;

        MemoryStream memoryStream;
        if (_enableCache && !string.IsNullOrEmpty(cacheKey) && _compilationCache.TryGetValue(cacheKey, out var cachedBytes))
        {
            memoryStream = new MemoryStream(cachedBytes);
        }
        else
        {
            memoryStream = CreateAndCompileToStream(content, compilationOptionsBuilder.Options);

            // 缓存编译结果
            if (_enableCache && !string.IsNullOrEmpty(cacheKey))
            {
                _compilationCache[cacheKey] = memoryStream.ToArray();
            }
        }

        return new ViewEngineTemplate<T>(memoryStream);
    }

    /// <summary>
    /// 编译模板
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="content"></param>
    /// <param name="builderAction"></param>
    /// <returns></returns>
    public async Task<IViewEngineTemplate<T>> CompileAsync<T>(string content, Action<IViewEngineOptionsBuilder> builderAction = null)
        where T : IViewEngineModel
    {
        return await Task.Run(() => Compile<T>(content: content, builderAction: builderAction));
    }

    /// <summary>
    /// 生成缓存键
    /// </summary>
    private static string GenerateCacheKey(string content, ViewEngineOptions options)
    {
        var hashContent = MD5Encryption.Encrypt(content);
        var hashOptions = MD5Encryption.Encrypt(string.Join("|",
            options.ReferencedAssemblies.Select(a => a.FullName),
            options.DefaultUsings.OrderBy(u => u)));
        return $"{hashContent}:{hashOptions}";
    }

    /// <summary>
    /// 将模板内容编译并输出内存流
    /// </summary>
    /// <param name="templateSource"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    protected virtual MemoryStream CreateAndCompileToStream(string templateSource, ViewEngineOptions options)
    {
        templateSource = WriteDirectives(templateSource, options);

        // 缓存 Razor 引擎，避免重复创建
        var engineKey = options.TemplateNamespace;
        var engine = _razorEngineCache.GetOrAdd(engineKey, _ => RazorProjectEngine.Create(
            RazorConfiguration.Default,
            RazorProjectFileSystem.Create(@"."),
            (builder) =>
            {
                builder.SetNamespace(options.TemplateNamespace);
            }));

        var fileName = Path.GetRandomFileName();

        var document = RazorSourceDocument.Create(templateSource, fileName);

        var codeDocument = engine.Process(
            document,
            null,
            new List<RazorSourceDocument>(),
            new List<TagHelperDescriptor>());

        var razorCSharpDocument = codeDocument.GetCSharpDocument();

        var syntaxTree = CSharpSyntaxTree.ParseText(razorCSharpDocument.GeneratedCode,
            new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.None));

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var metadataReferences = new List<MetadataReference>();

        foreach (var assembly in options.ReferencedAssemblies)
        {
            if (assembly == null || assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location))
                continue;

            if (!File.Exists(assembly.Location))
                continue;

            if (seen.Add(assembly.FullName ?? assembly.GetName().Name))
            {
                metadataReferences.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        // 添加自定义元数据引用
        metadataReferences.AddRange(options.MetadataReferences);

        var compilation = CSharpCompilation.Create(
            fileName,
            [syntaxTree],
            metadataReferences,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                nullableContextOptions: NullableContextOptions.Disable,
                warningLevel: 4,
                allowUnsafe: false,
                checkOverflow: false,
                deterministic: true
            ));

        var memoryStream = new MemoryStream();

        var emitResult = compilation.Emit(memoryStream);

        if (!emitResult.Success)
        {
            //var errors = emitResult.Diagnostics
            //    .Where(d => d.Severity == DiagnosticSeverity.Error || d.IsWarningAsError)
            //    .Select(d => d.ToString())
            //    .ToArray();

            var exception = new ViewEngineTemplateException()
            {
                Errors = emitResult.Diagnostics.ToList(),
                GeneratedCode = razorCSharpDocument.GeneratedCode
            };

            throw exception;
        }

        memoryStream.Position = 0;

        return memoryStream;
    }

    /// <summary>
    /// 写入 Razor 命令
    /// </summary>
    /// <param name="content"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    protected virtual string WriteDirectives(string content, ViewEngineOptions options)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"@inherits {options.Inherits}");

        foreach (var entry in options.DefaultUsings)
        {
            stringBuilder.AppendLine($"@using {entry}");
        }

        stringBuilder.Append(content);

        return stringBuilder.ToString();
    }
}