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

using System.Text;
using System.Text.RegularExpressions;

namespace Furion.Templates;

/// <summary>
/// 模板静态类
/// </summary>
public static class TP
{
    /// <summary>
    /// 模板正则表达式对象
    /// </summary>
    private static readonly Regex _regex = new(@"^##(?<prop>[^#]*)##[:：]?\s*(?<content>[\s\S]*)", RegexOptions.Compiled);

    /// <summary>
    /// GBK 编码实例
    /// </summary>
    private static readonly Encoding _gbkEncoding = Encoding.GetEncoding("gbk");

    /// <summary>
    /// 静态构造函数
    /// </summary>
    static TP()
    {
        // 注册编码提供器，支持 GBK 等非 UTF-8 编码
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// 生成规范日志模板
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="description">描述</param>
    /// <param name="items">列表项，如果以 ##xxx## 开头，自动生成 xxx: 属性</param>
    /// <param name="filter">日志条目过滤器，返回 false 隐藏该日志条目</param>
    /// <returns><see cref="string"/></returns>
    public static string Wrapper(string title, string description, string[] items, Func<string, bool>? filter)
    {
        var itemFilter = filter ?? (_ => true);

        var stringBuilder = new StringBuilder(512);
        stringBuilder.Append($"┏━━━━━━━━━━━  {title} ━━━━━━━━━━━").AppendLine();

        // 添加描述
        if (!string.IsNullOrWhiteSpace(description))
        {
            stringBuilder.Append($"┣ {description}").AppendLine().Append("┣ ").AppendLine();
        }

        // 添加项
        if (items != null && items.Length > 0)
        {
            var matches = new Match?[items.Length];
            var propMaxLength = 0;

            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var match = _regex.Match(item);

                if (match.Success)
                {
                    matches[i] = match; // 缓存匹配结果，后续直接使用
                    var propLength = match.Groups["prop"].Value.Length;
                    if (propLength > propMaxLength)
                        propMaxLength = propLength;
                }
            }

            // 控制项名称对齐空白占位数
            propMaxLength += (propMaxLength >= 5 ? 10 : 5);

            // 遍历每一项并进行正则表达式匹配
            for (var i = 0; i < items.Length; i++)
            {
                var match = matches[i];

                if (match != null && match.Success)
                {
                    var prop = match.Groups["prop"].Value;

                    // 过滤不需要的项
                    if (!itemFilter(prop)) continue;

                    var content = match.Groups["content"].Value;
                    var propTitle = $"{prop}：";
                    stringBuilder.Append($"┣ {PadRight(propTitle, propMaxLength)}{content}").AppendLine();
                }
                else
                {
                    // 非模板格式项，直接输出
                    stringBuilder.Append($"┣ {items[i]}").AppendLine();
                }
            }
        }

        stringBuilder.Append($"┗━━━━━━━━━━━  {title} ━━━━━━━━━━━");
        return stringBuilder.ToString();
    }

    /// <summary>
    /// 生成规范日志模板
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="description">描述</param>
    /// <param name="items">列表项，如果以 ##xxx## 开头，自动生成 xxx: 属性</param>
    /// <returns><see cref="string"/></returns>
    public static string Wrapper(string title, string description, params string[] items)
    {
        return Wrapper(title, description, items, null);
    }

    /// <summary>
    /// 矩形包裹
    /// </summary>
    /// <param name="lines">多行消息</param>
    /// <param name="align">对齐方式，-1/左对齐；0/居中对其；1/右对齐</param>
    /// <param name="pad">间隙</param>
    /// <returns><see cref="string"/></returns>
    public static string WrapperRectangle(string[] lines, int align = 0, int pad = 20)
    {
        if (lines == null || lines.Length == 0)
            return "+--+";

        // 循环计算最大长度
        var maxLength = 0;
        foreach (var line in lines)
        {
            var len = GetLength(line);
            if (len > maxLength) maxLength = len;
        }
        var width = maxLength + pad;

        var stringBuilder = new StringBuilder((width + 3) * (lines.Length + 2));

        // 添加矩形框的上边框
        stringBuilder.Append('+').Append('-', width - 2).AppendLine("+");

        foreach (var line in lines)
        {
            // 当前字符串的长度
            var len = GetLength(line);
            var padding = align switch
            {
                -1 => 2,                                    // 左对齐：固定 2 空格
                0 => (width - len - 2) / 2,                // 居中：计算居中偏移
                1 => width - len - 4,                       // 右对齐：简化计算逻辑
                _ => 2                                      // 默认左对齐
            };

            // 构建当前行：| + 前空格 + 内容 + 后空格 + |
            stringBuilder.Append('|').Append(' ', padding).Append(line);
            stringBuilder.Append(' ', width - len - 2 - padding).AppendLine("|");
        }

        // 添加矩形框的下边框
        stringBuilder.Append('+').Append('-', width - 2).Append('+');

        return stringBuilder.ToString();
    }

    /// <summary>
    /// 等宽文字对齐
    /// </summary>
    /// <param name="str"></param>
    /// <param name="totalByteCount"></param>
    /// <returns></returns>
    private static string PadRight(string str, int totalByteCount)
    {
        var currentByteCount = _gbkEncoding.GetByteCount(str);
        var paddingCount = totalByteCount - currentByteCount;

        if (paddingCount > 0)
        {
            return str + new string(' ', paddingCount);
        }

        return str;
    }

    /// <summary>
    /// 获取字符串长度
    /// </summary>
    /// <param name="str">字符串</param>
    /// <returns>字符串长度</returns>
    public static int GetLength(string str)
    {
        if (string.IsNullOrEmpty(str))
            return 0;

        return _gbkEncoding.GetByteCount(str);
    }
}