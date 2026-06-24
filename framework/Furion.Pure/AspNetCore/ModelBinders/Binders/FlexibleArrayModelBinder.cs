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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Furion.AspNetCore;

/// <summary>
/// 数组 URL 地址参数模型绑定
/// </summary>
internal class FlexibleArrayModelBinder<T> : IModelBinder
{
    // 缓存元素类型信息
    private static readonly Type _elementType = typeof(T);
    private static readonly bool _isNullableType = _elementType.IsGenericType && _elementType.GetGenericTypeDefinition() == typeof(Nullable<>);

    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        // 空检查
        ArgumentNullException.ThrowIfNull(bindingContext);

        // 获取模型名和类型
        var modelName = bindingContext.ModelName;
        var modelType = bindingContext.ModelType;

        // 获取 URL 参数集合
        var queryCollection = bindingContext.HttpContext.Request.Query;

        // 获取 FlexibleArrayAttribute<T> 的 Split 配置
        var split = GetSplitSetting(bindingContext);

        // 尝试从查询字符串中获取值
        var values = TryGetValues(queryCollection, modelName, split);

        if (values != null && values.Any())
        {
            var convertedValues = ConvertValues(values, modelType);
            bindingContext.Result = ModelBindingResult.Success(convertedValues);
        }
        else
        {
            bindingContext.Result = ModelBindingResult.Success(CreateEmptyCollection(modelType));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取 Split 配置
    /// </summary>
    /// <param name="bindingContext"><see cref="ModelBindingContext"/></param>
    /// <returns></returns>
    private static bool GetSplitSetting(ModelBindingContext bindingContext)
    {
        // 获取 FlexibleArrayAttribute<T> 特性实例
        var flexibleArrayAttribute = (bindingContext.ModelMetadata as DefaultModelMetadata).Attributes
            .Attributes.OfType<FlexibleArrayAttribute<T>>().FirstOrDefault();

        return flexibleArrayAttribute?.Split ?? true;
    }

    /// <summary>
    /// 尝试从查询字符串中获取值
    /// </summary>
    /// <param name="queryCollection"><see cref="IQueryCollection"/></param>
    /// <param name="modelName">模型名称</param>
    /// <param name="split">是否启用逗号拆分</param>
    /// <returns></returns>
    private static IEnumerable<string> TryGetValues(IQueryCollection queryCollection, string modelName, bool split)
    {
        // 处理 status[]=value1&status[]=value2 格式
        if (queryCollection.TryGetValue(modelName + "[]", out var arrValues) && arrValues.Count > 0)
        {
            return arrValues.Where(s => !string.IsNullOrWhiteSpace(s));
        }

        // 处理 status=value1&status=value2 格式
        if (queryCollection.TryGetValue(modelName, out var values) && values.Count > 0)
        {
            // 处理多个同名键 sort=age,asc&sort=name,desc
            if (values.Count > 1)
            {
                return values.Where(s => !string.IsNullOrWhiteSpace(s));
            }

            // 处理单个键，根据 Split 决定是否按逗号拆分
            if (split)
            {
                return values.ToString()
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s));
            }

            // 不拆分时直接返回单一元素
            return [values.ToString().Trim()];
        }

        return null;
    }

    /// <summary>
    /// 将字符串集合转换为目标模型类型
    /// </summary>
    /// <param name="values">已过滤空值的字符串序列</param>
    /// <param name="modelType">目标模型类型</param>
    /// <returns></returns>
    private static object ConvertValues(IEnumerable<string> values, Type modelType)
    {
        var convertedList = values
            .Select(value => value.ChangeType<T>())
            .Where(v => v != null || _isNullableType)
            .ToList();

        // 检查目标类型是否是数组类型
        if (modelType.IsArray)
        {
            return convertedList.ToArray();
        }

        // 检查目标类型是否是泛型
        if (modelType.IsGenericType)
        {
            var genericTypeDefinition = modelType.GetGenericTypeDefinition();

            if (genericTypeDefinition == typeof(List<>))
            {
                return convertedList;
            }

            // 其他 IEnumerable<T> 衍生类型
            if (genericTypeDefinition == typeof(IEnumerable<>) ||
                genericTypeDefinition == typeof(ICollection<>) ||
                genericTypeDefinition == typeof(IReadOnlyList<>) ||
                genericTypeDefinition == typeof(IReadOnlyCollection<>))
            {
                return convertedList;
            }
        }

        return convertedList;
    }

    /// <summary>
    /// 创建与模型类型匹配的空集合
    /// </summary>
    /// <param name="modelType">模型类型</param>
    /// <returns></returns>
    private static object CreateEmptyCollection(Type modelType)
    {
        // 处理数组类型
        if (modelType.IsArray)
        {
            return Array.Empty<T>();
        }

        // 处理 List 类型
        if (modelType.IsGenericType && modelType.GetGenericTypeDefinition() == typeof(List<>))
        {
            return new List<T>();
        }

        return Array.Empty<T>();
    }
}