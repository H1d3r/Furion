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

using Furion.Reflection;
using Furion.Templates.Extensions;
using System.Text;

namespace Furion.SensitiveDetection;

/// <summary>
/// 脱敏词汇（脱敏）提供器（默认实现）
/// </summary>
/// <remarks>
/// 构造函数
/// </remarks>
/// <param name="embedFileName">脱敏词汇数据文件名</param>
public class SensitiveDetectionProvider(string embedFileName) : ISensitiveDetectionProvider
{
    /// <summary>
    /// 本地缓存的脱敏词汇集合
    /// </summary>
    private IEnumerable<string>? _cachedWords;

    /// <summary>
    /// 异步加载锁
    /// </summary>
    private readonly SemaphoreSlim _asyncCacheLock = new(1, 1);

    /// <summary>
    /// 同步加载锁
    /// </summary>
    private readonly object _syncCacheLock = new();

    /// <summary>
    /// 返回所有脱敏词汇
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<string>> GetWordsAsync()
    {
        // 确保线程安全
        if (_cachedWords != null) return _cachedWords;

        await _asyncCacheLock.WaitAsync().ConfigureAwait(false);

        try
        {
            // 双重检查
            if (_cachedWords != null) return _cachedWords;

            // 从嵌入式资源加载脱敏词汇
            var wordsOfCached = await LoadWordsFromEmbeddedResourceAsync();

            // 仅分割一次
            _cachedWords = NormalizeAndSplitWords(wordsOfCached).ToList();

            return _cachedWords;
        }
        finally
        {
            _asyncCacheLock.Release();
        }
    }

    /// <summary>
    /// 返回所有脱敏词汇
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetWords()
    {
        // 确保线程安全
        if (_cachedWords != null) return _cachedWords;

        lock (_syncCacheLock)
        {
            // 双重检查
            if (_cachedWords != null) return _cachedWords;

            // 从嵌入式资源加载脱敏词汇
            var wordsOfCached = LoadWordsFromEmbeddedResource();

            // 仅分割一次
            _cachedWords = NormalizeAndSplitWords(wordsOfCached).ToList();

            return _cachedWords;
        }
    }

    /// <summary>
    /// 判断脱敏词汇是否有效（支持自定义算法）
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public async Task<bool> IsValidAsync(string text)
    {
        // 空字符串和空白字符不验证
        if (string.IsNullOrWhiteSpace(text)) return true;

        // 查找脱敏词汇出现次数和位置
        var foundSets = await FoundSensitiveWordsAsync(text);

        return foundSets.Count == 0;
    }

    /// <summary>
    /// 判断脱敏词汇是否有效（支持自定义算法）（同步版本）
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public bool IsValid(string text)
    {
        // 空字符串和空白字符不验证
        if (string.IsNullOrWhiteSpace(text)) return true;

        // 查找脱敏词汇出现次数和位置
        var foundSets = FoundSensitiveWords(text);

        return foundSets.Count == 0;
    }

    /// <summary>
    /// 替换敏感词汇
    /// </summary>
    /// <param name="text"></param>
    /// <param name="transfer"></param>
    /// <returns></returns>
    public async Task<string> ReplaceAsync(string text, char transfer = '*')
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        // 查找脱敏词汇出现次数和位置
        var foundSets = await FoundSensitiveWordsAsync(text);

        return ReplaceWordsCore(text, foundSets, transfer);
    }

    /// <summary>
    /// 替换敏感词汇（同步版本）
    /// </summary>
    /// <param name="text"></param>
    /// <param name="transfer"></param>
    /// <returns></returns>
    public string Replace(string text, char transfer = '*')
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        // 查找脱敏词汇出现次数和位置
        var foundSets = FoundSensitiveWords(text);

        return ReplaceWordsCore(text, foundSets, transfer);
    }

    /// <summary>
    /// 查找脱敏词汇
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public async Task<Dictionary<string, List<int>>> FoundSensitiveWordsAsync(string text)
    {
        // 支持读取配置渲染
        var realText = text.Render();

        // 获取脱敏词库
        var sensitiveWords = await GetWordsAsync();

        // 在文本中定位所有敏感词的位置
        return FindWordPositions(realText, sensitiveWords);
    }

    /// <summary>
    /// 查找脱敏词汇（同步版本）
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public Dictionary<string, List<int>> FoundSensitiveWords(string text)
    {
        // 支持读取配置渲染
        var realText = text.Render();

        // 获取脱敏词库
        var sensitiveWords = GetWords();

        // 在文本中定位所有敏感词的位置
        return FindWordPositions(realText, sensitiveWords);
    }

    /// <summary>
    /// 获取嵌入式资源文件流（支持模糊匹配）
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private Stream GetEmbeddedResourceStream()
    {
        var entryAssembly = Reflect.GetEntryAssembly();

        /*
         * 查找脱敏词汇数据的嵌入资源文件。
         * 由于程序集名称可在 .csproj 文件中通过 <AssemblyName> 自定义，
         * 故采用模糊匹配方式查找。请确保资源文件名具有唯一性，避免歧义。
         * 
         * var embedFileNameOfResource = $"{Reflect.GetAssemblyName(entryAssembly)}.{embedFileName}";
         */
        var embedFileNameOfResource = entryAssembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(embedFileName, StringComparison.OrdinalIgnoreCase));

        var readStream = entryAssembly.GetManifestResourceStream(embedFileNameOfResource)
           ?? throw new InvalidOperationException($"The embedded file of path <{embedFileNameOfResource}> is not found.");

        return readStream;
    }

    /// <summary>
    /// 从嵌入式资源加载脱敏词汇（异步）
    /// </summary>
    /// <returns></returns>
    private async Task<string> LoadWordsFromEmbeddedResourceAsync()
    {
        // 获取嵌入式资源文件流
        using var readStream = GetEmbeddedResourceStream();

        // 解析嵌入式文件流
        var buffer = new byte[readStream.Length];
        var position = 0;
        var remaining = buffer.Length;
        while (remaining > 0)
        {
            var read = await readStream.ReadAsync(buffer.AsMemory(position, remaining)).ConfigureAwait(false);
            if (read == 0) break;   // 流结束
            position += read;
            remaining -= read;
        }

        // 同时兼容 UTF-8 BOM，UTF-8
        using var stream = new MemoryStream(buffer);
        using var streamReader = new StreamReader(stream, new UTF8Encoding(true));
        return await streamReader.ReadToEndAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// 从嵌入式资源加载脱敏词汇（同步）
    /// </summary>
    /// <returns></returns>
    private string LoadWordsFromEmbeddedResource()
    {
        // 获取嵌入式资源文件流
        using var readStream = GetEmbeddedResourceStream();

        // 解析嵌入式文件流
        var buffer = new byte[readStream.Length];
        var position = 0;
        var remaining = buffer.Length;
        while (remaining > 0)
        {
            var read = readStream.Read(buffer, position, remaining);
            if (read == 0) break;   // 流结束
            position += read;
            remaining -= read;
        }

        // 同时兼容 UTF-8 BOM，UTF-8
        using var stream = new MemoryStream(buffer);
        using var streamReader = new StreamReader(stream, new UTF8Encoding(true));
        return streamReader.ReadToEnd();
    }

    /// <summary>
    /// 规范化文本并分割为脱敏词汇集合
    /// </summary>
    /// <param name="wordsOfCached"></param>
    /// <returns></returns>
    private static IEnumerable<string> NormalizeAndSplitWords(string wordsOfCached)
    {
        // 统一换行符：先将 \r\n 和 \r 标准化为 \n，再按 \n 和 | 分割
        // 解决跨平台换行符差异导致词汇分割失败的问题
        var normalizedText = wordsOfCached.Replace("\r\n", "\n").Replace("\r", "\n");
        return normalizedText.Split(["\n", "|"], StringSplitOptions.RemoveEmptyEntries)
            .Select(u => u.Trim())
            .Where(u => !string.IsNullOrEmpty(u))
            .Distinct();
    }

    /// <summary>
    /// 在文本中定位所有敏感词的位置
    /// </summary>
    /// <param name="text"></param>
    /// <param name="sensitiveWords"></param>
    /// <returns></returns>
    private static Dictionary<string, List<int>> FindWordPositions(string text, IEnumerable<string> sensitiveWords)
    {
        // 记录脱敏词汇出现位置和次数
        var foundSets = new Dictionary<string, List<int>>();

        // 遍历所有脱敏词汇并查找字符串是否包含
        foreach (var sensitiveWord in sensitiveWords)
        {
            if (string.IsNullOrEmpty(sensitiveWord)) continue;

            var startIndex = 0;
            while (true)
            {
                // 在原始字符串中直接查找，记录绝对位置
                var findIndex = text.IndexOf(sensitiveWord, startIndex, StringComparison.OrdinalIgnoreCase);
                if (findIndex == -1) break;

                if (!foundSets.TryGetValue(sensitiveWord, out var value))
                {
                    value = [];
                    foundSets[sensitiveWord] = value;
                }

                value.Add(findIndex);

                // 从下一个字符开始继续查找，支持重叠匹配
                startIndex = findIndex + 1;
            }
        }

        return foundSets;
    }

    /// <summary>
    /// 根据找到的位置替换敏感词
    /// </summary>
    /// <param name="text"></param>
    /// <param name="foundSets"></param>
    /// <param name="transfer"></param>
    /// <returns></returns>
    private static string ReplaceWordsCore(string text, Dictionary<string, List<int>> foundSets, char transfer)
    {
        // 如果没有敏感词则返回原字符串
        if (foundSets.Count == 0) return text;

        var stringBuilder = new StringBuilder(text);

        // 从后往前替换，避免前面的替换影响后面敏感词的位置
        foreach (var kv in foundSets)
        {
            var sensitiveWord = kv.Key;
            // 按位置降序排列，确保从后往前替换
            var positions = kv.Value.OrderByDescending(p => p);

            foreach (var position in positions)
            {
                for (var i = 0; i < sensitiveWord.Length; i++)
                {
                    stringBuilder[position + i] = transfer;
                }
            }
        }

        return stringBuilder.ToString();
    }
}