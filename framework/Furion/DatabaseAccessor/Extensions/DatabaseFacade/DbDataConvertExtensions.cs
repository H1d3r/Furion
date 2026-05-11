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
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace Furion.DatabaseAccessor;

/// <summary>
/// 数据库数据转换扩展
/// </summary>
public static class DbDataConvertExtensions
{
    /// <summary>
    /// 缓存类型的属性信息和列名映射
    /// </summary>
    private static readonly ConcurrentDictionary<Type, (PropertyInfo[] Properties, Dictionary<string, string> ColumnMap)> _reflectionCache = new();

    /// <summary>
    /// 将 DataTable 转 List 集合
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="dataTable">DataTable</param>
    /// <returns>List{T}</returns>
    public static List<T> ToList<T>(this DataTable dataTable)
    {
        return dataTable.ToList(typeof(List<T>)) as List<T>;
    }

    /// <summary>
    /// 将 DataTable 转 List 集合
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="dataTable">DataTable</param>
    /// <returns>List{T}</returns>
    public static async Task<List<T>> ToListAsync<T>(this DataTable dataTable)
    {
        var list = await dataTable.ToListAsync(typeof(List<T>));
        return list as List<T>;
    }

    /// <summary>
    /// 将 DataSet 转 元组
    /// </summary>
    /// <typeparam name="T1">元组元素类型</typeparam>
    /// <param name="dataSet">DataSet</param>
    /// <returns>元组类型</returns>
    public static List<T1> ToList<T1>(this DataSet dataSet)
    {
        var tuple = dataSet.ToList(typeof(List<T1>));
        return tuple[0] as List<T1>;
    }

    /// <summary>
    /// 将 DataSet 转 元组
    /// </summary>
    /// <typeparam name="T1">元组元素类型</typeparam>
    /// <typeparam name="T2">元组元素类型</typeparam>
    /// <param name="dataSet">DataSet</param>
    /// <returns>元组类型</returns>
    public static (List<T1> list1, List<T2> list2) ToList<T1, T2>(this DataSet dataSet)
    {
        var tuple = dataSet.ToList(typeof(List<T1>), typeof(List<T2>));
        return (tuple[0] as List<T1>, tuple[1] as List<T2>);
    }

    /// <summary>
    /// 将 DataSet 转 元组
    /// </summary>
    /// <typeparam name="T1">元组元素类型</typeparam>
    /// <typeparam name="T2">元组元素类型</typeparam>
    /// <typeparam name="T3">元组元素类型</typeparam>
    /// <param name="dataSet">DataSet</param>
    /// <returns>元组类型</returns>
    public static (List<T1> list1, List<T2> list2, List<T3> list3) ToList<T1, T2, T3>(this DataSet dataSet)
    {
        var tuple = dataSet.ToList(typeof(List<T1>), typeof(List<T2>), typeof(List<T3>));
        return (tuple[0] as List<T1>, tuple[1] as List<T2>, tuple[2] as List<T3>);
    }

    /// <summary>
    /// 将 DataSet 转 元组
    /// </summary>
    /// <typeparam name="T1">元组元素类型</typeparam>
    /// <typeparam name="T2">元组元素类型</typeparam>
    /// <typeparam name="T3">元组元素类型</typeparam>
    /// <typeparam name="T4">元组元素类型</typeparam>
    /// <param name="dataSet">DataSet</param>
    /// <returns>元组类型</returns>
    public static (List<T1> list1, List<T2> list2, List<T3> list3, List<T4> list4) ToList<T1, T2, T3, T4>(this DataSet dataSet)
    {
        var tuple = dataSet.ToList(typeof(List<T1>), typeof(List<T2>), typeof(List<T3>), typeof(List<T4>));
        return (tuple[0] as List<T1>, tuple[1] as List<T2>, tuple[2] as List<T3>, tuple[3] as List<T4>);
    }

    /// <summary>
    /// 将 DataSet 转 元组
    /// </summary>
    /// <typeparam name="T1">元组元素类型</typeparam>
    /// <typeparam name="T2">元组元素类型</typeparam>
    /// <typeparam name="T3">元组元素类型</typeparam>
    /// <typeparam name="T4">元组元素类型</typeparam>
    /// <typeparam name="T5">元组元素类型</typeparam>
    /// <param name="dataSet">DataSet</param>
    /// <returns>元组类型</returns>
    public static (List<T1> list1, List<T2> list2, List<T3> list3, List<T4> list4, List<T5> list5) ToList<T1, T2, T3, T4, T5>(this DataSet dataSet)
    {
        var tuple = dataSet.ToList(typeof(List<T1>), typeof(List<T2>), typeof(List<T3>), typeof(List<T4>), typeof(List<T5>));
        return (tuple[0] as List<T1>, tuple[1] as List<T2>, tuple[2] as List<T3>, tuple[3] as List<T4>, tuple[4] as List<T5>);
    }

    /// <summary>
    /// 将 DataSet 转 元组
    /// </summary>
    /// <typeparam name="T1">元组元素类型</typeparam>
    /// <typeparam name="T2">元组元素类型</typeparam>
    /// <typeparam name="T3">元组元素类型</typeparam>
    /// <typeparam name="T4">元组元素类型</typeparam>
    /// <typeparam name="T5">元组元素类型</typeparam>
    /// <typeparam name="T6">元组元素类型</typeparam>
    /// <param name="dataSet">DataSet</param>
    /// <returns>元组类型</returns>
    public static (List<T1> list1, List<T2> list2, List<T3> list3, List<T4> list4, List<T5> list5, List<T6> list6) ToList<T1, T2, T3, T4, T5, T6>(this DataSet dataSet)
    {
        var tuple = dataSet.ToList(typeof(List<T1>), typeof(List<T2>), typeof(List<T3>), typeof(List<T4>), typeof(List<T5>), typeof(List<T6>));
        return (tuple[0] as List<T1>, tuple[1] as List<T2>, tuple[2] as List<T3>, tuple[3] as List<T4>, tuple[4] as List<T5>, tuple[5] as List<T6>);
    }

    /// <summary>
    /// 将 DataSet 转 元组
    /// </summary>
    /// <typeparam name="T1">元组元素类型</typeparam>
    /// <typeparam name="T2">元组元素类型</typeparam>
    /// <typeparam name="T3">元组元素类型</typeparam>
    /// <typeparam name="T4">元组元素类型</typeparam>
    /// <typeparam name="T5">元组元素类型</typeparam>
    /// <typeparam name="T6">元组元素类型</typeparam>
    /// <typeparam name="T7">元组元素类型</typeparam>
    /// <param name="dataSet">DataSet</param>
    /// <returns>元组类型</returns>
    public static (List<T1> list1, List<T2> list2, List<T3> list3, List<T4> list4, List<T5> list5, List<T6> list6, List<T7> list7) ToList<T1, T2, T3, T4, T5, T6, T7>(this DataSet dataSet)
    {
        var tuple = dataSet.ToList(typeof(List<T1>), typeof(List<T2>), typeof(List<T3>), typeof(List<T4>), typeof(List<T5>), typeof(List<T6>), typeof(List<T7>));
        return (tuple[0] as List<T1>, tuple[1] as List<T2>, tuple[2] as List<T3>, tuple[3] as List<T4>, tuple[4] as List<T5>, tuple[5] as List<T6>, tuple[6] as List<T7>);
    }

    /// <summary>
    /// 将 DataSet 转 元组
    /// </summary>
    /// <typeparam name="T1">元组元素类型</typeparam>
    /// <typeparam name="T2">元组元素类型</typeparam>
    /// <typeparam name="T3">元组元素类型</typeparam>
    /// <typeparam name="T4">元组元素类型</typeparam>
    /// <typeparam name="T5">元组元素类型</typeparam>
    /// <typeparam name="T6">元组元素类型</typeparam>
    /// <typeparam name="T7">元组元素类型</typeparam>
    /// <typeparam name="T8">元组元素类型</typeparam>
    /// <param name="dataSet">DataSet</param>
    /// <returns>元组类型</returns>
    public static (List<T1> list1, List<T2> list2, List<T3> list3, List<T4> list4, List<T5> list5, List<T6> list6, List<T7> list7, List<T8> list8) ToList<T1, T2, T3, T4, T5, T6, T7, T8>(this DataSet dataSet)
    {
        var tuple = dataSet.ToList(typeof(List<T1>), typeof(List<T2>), typeof(List<T3>), typeof(List<T4>), typeof(List<T5>), typeof(List<T6>), typeof(List<T7>), typeof(List<T8>));
        return (tuple[0] as List<T1>, tuple[1] as List<T2>, tuple[2] as List<T3>, tuple[3] as List<T4>, tuple[4] as List<T5>, tuple[5] as List<T6>, tuple[6] as List<T7>, tuple[7] as List<T8>);
    }

    /// <summary>
    /// 将 DataSet 转 特定类型
    /// </summary>
    /// <param name="dataSet">DataSet</param>
    /// <param name="returnTypes">特定类型集合</param>
    /// <returns>List{object}</returns>
    public static List<object> ToList(this DataSet dataSet, params Type[] returnTypes)
    {
        // 获取所有的 DataTable
        var dataTables = dataSet.Tables;

        // 处理元组类型
        if (returnTypes.Length == 1 && returnTypes[0].IsValueType)
        {
            returnTypes = returnTypes[0].GenericTypeArguments;
        }

        // 处理不传入 returnTypes 类型
        if (returnTypes == null || returnTypes.Length == 0)
        {
            returnTypes = Enumerable.Range(0, dataTables.Count).Select(u => typeof(List<object>)).ToArray();
        }

        // 使用数组索引访问
        var results = new List<object>(returnTypes.Length);
        var count = Math.Min(returnTypes.Length, dataTables.Count);
        for (var i = 0; i < count; i++)
        {
            results.Add(dataTables[i].ToList(returnTypes[i]));
        }
        return results;
    }

    /// <summary>
    /// 将 DataSet 转 特定类型
    /// </summary>
    /// <param name="dataSet">DataSet</param>
    /// <param name="returnTypes">特定类型集合</param>
    /// <returns>object</returns>
    public static Task<List<object>> ToListAsync(this DataSet dataSet, params Type[] returnTypes)
    {
        return Task.FromResult(dataSet.ToList(returnTypes));
    }

    /// <summary>
    /// 将 DataTable 转 特定类型
    /// </summary>
    /// <param name="dataTable">DataTable</param>
    /// <param name="returnType">返回值类型</param>
    /// <returns>object</returns>
    public static object ToList(this DataTable dataTable, Type returnType)
    {
        var isGenericType = returnType.IsGenericType;
        // 获取类型真实返回类型
        var underlyingType = isGenericType ? returnType.GenericTypeArguments.First() : (returnType.IsArray ? returnType.GetElementType() : returnType);

        var resultType = typeof(List<>).MakeGenericType(underlyingType);
        var list = Activator.CreateInstance(resultType);
        var addMethod = resultType.GetMethod("Add");

        // 将 DataTable 转为行集合
        var dataRows = dataTable.AsEnumerable();

        // 如果是基元类型
        if (underlyingType.IsRichPrimitive())
        {
            // 遍历所有行
            foreach (var dataRow in dataRows)
            {
                // 只取第一列数据
                var firstColumnValue = dataRow[0];
                // 转换成目标类型数据
                var destValue = firstColumnValue?.ChangeType(underlyingType);
                // 添加到集合中
                _ = addMethod.Invoke(list, [destValue]);
            }
        }
        // 处理Object类型
        else if (underlyingType == typeof(object))
        {
            // 获取所有列名
            var columns = dataTable.Columns;

            // 遍历所有行
            foreach (var dataRow in dataRows)
            {
                var dic = new Dictionary<string, object>();
                foreach (DataColumn column in columns)
                {
                    dic.Add(column.ColumnName, dataRow[column]);
                }
                _ = addMethod.Invoke(list, [dic]);
            }
        }
        else
        {
            // 二次处理数组类型
            var isArrayType = underlyingType.IsArray;
            var actType = isArrayType ? underlyingType.GetElementType() : underlyingType;

            // 缓存反射信息
            var (properties, columnMap) = _reflectionCache.GetOrAdd(actType, type =>
            {
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var prop in props)
                {
                    // 获取属性对应的真实列名（支持 [Column] 特性）
                    var columnName = prop.Name;
                    if (prop.IsDefined(typeof(ColumnAttribute), true))
                    {
                        var columnAttribute = prop.GetCustomAttribute<ColumnAttribute>(true);
                        if (!string.IsNullOrWhiteSpace(columnAttribute?.Name))
                        {
                            columnName = columnAttribute.Name;
                        }
                    }
                    map[prop.Name] = columnName;
                }
                return (props, map);
            });

            // 预构建列名查找表
            var columnNames = new HashSet<string>(dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName), StringComparer.OrdinalIgnoreCase);

            // 遍历所有行
            foreach (var dataRow in dataRows)
            {
                var model = Activator.CreateInstance(actType);

                // 遍历所有属性并一一赋值
                foreach (var property in properties)
                {
                    // 获取属性对应的真实列名
                    var columnName = columnMap[property.Name];

                    // 如果 DataTable 不包含该列名，则跳过
                    if (!columnNames.Contains(columnName))
                    {
                        // 降级处理：尝试多种命名风格匹配
                        var splitColumnName = string.Join('_', property.Name.SplitCamelCase());
                        var candidates = new[] { splitColumnName, splitColumnName.ToUpper(), splitColumnName.ToLower(),
                                                columnName.ToUpper(), columnName.ToLower() };

                        var matched = candidates.FirstOrDefault(c => columnNames.Contains(c));
                        if (matched == null) continue;
                        columnName = matched;
                    }

                    // 获取列值
                    var columnValue = dataRow[columnName];
                    // 如果列值未空，则跳过
                    if (columnValue == DBNull.Value) continue;

                    // 转换成目标类型数据
                    var destValue = columnValue?.ChangeType(property.PropertyType);
                    property.SetValue(model, destValue);
                }

                // 处理数组类型
                object pams;
                if (isArrayType)
                {
                    var listType = typeof(List<>).MakeGenericType(actType);
                    var listArray = Activator.CreateInstance(listType);
                    listType.GetMethod("Add").Invoke(listArray, [model]);
                    pams = ((dynamic)listArray).ToArray();
                }
                else pams = model;

                // 添加到集合中
                _ = addMethod.Invoke(list, [pams]);
            }
        }

        return list;
    }

    /// <summary>
    /// 将 DataTable 转 特定类型
    /// </summary>
    /// <param name="dataTable">DataTable</param>
    /// <param name="returnType">返回值类型</param>
    /// <returns>object</returns>
    public static Task<object> ToListAsync(this DataTable dataTable, Type returnType)
    {
        return Task.FromResult(dataTable.ToList(returnType));
    }

    /// <summary>
    /// 将 DbDataReader 转 DataTable
    /// </summary>
    /// <param name="dataReader"></param>
    /// <returns></returns>
    public static DataTable ToDataTable(this DbDataReader dataReader)
    {
        var dataTable = new DataTable();

        // 创建列
        for (var i = 0; i < dataReader.FieldCount; i++)
        {
            var dataClumn = new DataColumn
            {
                DataType = dataReader.GetFieldType(i),
                ColumnName = dataReader.GetName(i)
            };

            dataTable.Columns.Add(dataClumn);
        }

        // 循环读取
        while (dataReader.Read())
        {
            // 创建行
            var dataRow = dataTable.NewRow();
            for (var i = 0; i < dataReader.FieldCount; i++)
            {
                dataRow[i] = dataReader[i];
            }

            dataTable.Rows.Add(dataRow);
        }

        return dataTable;
    }

    /// <summary>
    /// 将 DbDataReader 转 DataSet
    /// </summary>
    /// <param name="dataReader"></param>
    /// <returns></returns>
    public static DataSet ToDataSet(this DbDataReader dataReader)
    {
        var dataSet = new DataSet();

        do
        {
            // 获取元数据
            var schemaTable = dataReader.GetSchemaTable();
            var dataTable = new DataTable();

            if (schemaTable != null)
            {
                for (var i = 0; i < schemaTable.Rows.Count; i++)
                {
                    var dataRow = schemaTable.Rows[i];

                    var columnName = (string)dataRow["ColumnName"];
                    var column = new DataColumn(columnName, (Type)dataRow["DataType"]);
                    dataTable.Columns.Add(column);
                }

                dataSet.Tables.Add(dataTable);

                // 循环读取
                while (dataReader.Read())
                {
                    var dataRow = dataTable.NewRow();

                    for (var i = 0; i < dataReader.FieldCount; i++)
                    {
                        dataRow[i] = dataReader.GetValue(i);
                    }

                    dataTable.Rows.Add(dataRow);
                }
            }
            else
            {
                var column = new DataColumn("RecordsAffected");
                dataTable.Columns.Add(column);
                dataSet.Tables.Add(dataTable);

                var dataRow = dataTable.NewRow();
                dataRow[0] = dataReader.RecordsAffected;
                dataTable.Rows.Add(dataRow);
            }
        }

        // 读取下一个结果
        while (dataReader.NextResult());

        return dataSet;
    }

    /// <summary>
    /// 处理元组类型返回值
    /// </summary>
    /// <param name="dataSet">数据集</param>
    /// <param name="tupleType">返回值类型</param>
    /// <returns></returns>
    internal static object ToValueTuple(this DataSet dataSet, Type tupleType)
    {
        // 获取元组最底层类型
        var underlyingTypes = tupleType.GetGenericArguments().Select(u => u.IsGenericType ? u.GetGenericArguments().First() : u);

        var toListMethod = typeof(DbDataConvertExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(u => u.Name == "ToList" && u.IsGenericMethod && u.GetGenericArguments().Length == tupleType.GetGenericArguments().Length)
            .MakeGenericMethod(underlyingTypes.ToArray());

        return toListMethod.Invoke(null, [dataSet]);
    }
}