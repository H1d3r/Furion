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
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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

        // 获取 Split 配置
        var split = GetSplitSetting(bindingContext);

        // 获取 URL 参数集合
        var queryCollection = bindingContext.HttpContext.Request.Query;

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
    private static bool GetSplitSetting(ModelBindingContext bindingContext)
    {
        var modelMetadata = bindingContext.ModelMetadata;

        // 方案1： 尝试从控制器 Action 参数中查找
        if (bindingContext.ActionContext.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
        {
            var parameter = actionDescriptor.MethodInfo.GetParameters()// ParameterInfo[]
                .FirstOrDefault(p => string.Equals(p.Name, bindingContext.ModelName, StringComparison.OrdinalIgnoreCase));

            if (parameter != null)
            {
                var attr = parameter.GetCustomAttributes(typeof(FlexibleArrayAttribute<T>),false)
                    .FirstOrDefault() as FlexibleArrayAttribute<T>;
                if (attr != null)
                {
                    return attr.Split;
                }
            }
        }

        // 方案2：直接从模型元数据特性中查找
        var flexibleArrayAttr = ((Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultModelMetadata)modelMetadata).Attributes.PropertyAttributes
            ?.OfType<FlexibleArrayAttribute<T>>().FirstOrDefault();
        if (flexibleArrayAttr != null)
        {
            return flexibleArrayAttr.Split;
        }

        // 默认返回 true
        return true;
    }

    /// <summary>
    /// 尝试从查询字符串中获取值
    /// </summary>
    private static IEnumerable<string> TryGetValues(IQueryCollection queryCollection, string modelName, bool split)
    {
        // status[]=value1&status[]=value2 ac格式
        if (queryCollection.ContainsKey(modelName + "[]"))
        {
            return queryCollection[modelName + "[]"]
                .Where(s => !string.IsNullOrWhiteSpace(s));
        }

        if (queryCollection.ContainsKey(modelName) && queryCollection[modelName].Count > 0)
        {
            var values = queryCollection[modelName];

            // 多个同名键（如 sort=age,asc&sort=name,desc）,每个键值作为独立元素，不拆分
            if (values.Count > 1)
            {
                return values.Where(s => !string.IsNullOrWhiteSpace(s));
            }

            // 单个键：根据 Split 决定是否按逗号拆分
            if (split)
            {
                return values.ToString()
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s));
            }

            return new[] { values.ToString().Trim() };
        }

        return null;
    }

    /// <summary>
    /// 转换集合类型值为模型类型值
    /// </summary>
    private static object ConvertValues(IEnumerable<string> values, Type modelType)
    {
        var convertedList = values
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(value => value.ChangeType<T>())
            .Where(v => v != null || _isNullableType)
            .ToList();

        // 根据目标类型创建相应的集合
        if (modelType.IsArray)
        {
            // 创建数组并填充转换后的值
            var array = Array.CreateInstance(_elementType, convertedList.Count);
            for (var i = 0; i < convertedList.Count; i++)
            {
                array.SetValue(convertedList[i], i);
            }

            return array;
        }

        if (modelType.IsGenericType)
        {
            var genericTypeDefinition = modelType.GetGenericTypeDefinition();

            if (genericTypeDefinition == typeof(List<>))
            {
                var listType = typeof(List<>).MakeGenericType(_elementType);
                var list = (IList)Activator.CreateInstance(listType);

                foreach (var item in convertedList)
                {
                    list.Add(item);
                }

                return list;
            }

            // 支持其他 IEnumerable<T> 衍生类型
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
    /// 创建空集合
    /// </summary>
    private static object CreateEmptyCollection(Type modelType)
    {
        // 处理数组类型
        if (modelType.IsArray)
        {
            return Array.CreateInstance(_elementType, 0);
        }
        // 处理 List 类型
        if (modelType.IsGenericType && modelType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var listType = typeof(List<>).MakeGenericType(_elementType);
            return Activator.CreateInstance(listType);
        }

        return Array.Empty<T>();
    }
}