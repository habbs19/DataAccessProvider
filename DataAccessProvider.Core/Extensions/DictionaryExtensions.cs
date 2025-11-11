using System.Reflection;

namespace DataAccessProvider.Core.Extensions;

public static class DataMapper
{
    /// <summary>
    /// Maps a dictionary to an object of type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="row"></param>
    /// <returns></returns>
    public static T MapTo<T>(this Dictionary<string, object> row) where T : new()
    {
        var obj = new T();
        var objType = typeof(T);

        foreach (var kvp in row)
        {
            var prop = objType.GetProperty(
                kvp.Key,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
            );

            if (prop != null && prop.CanWrite && kvp.Value != DBNull.Value)
            {
                try
                {
                    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                    object? safeValue;

                    if (targetType.IsEnum)
                    {
                        if (kvp.Value is string strVal)
                        {
                            // Try parse enum from string
                            safeValue = Enum.Parse(targetType, strVal, ignoreCase: true);
                        }
                        else
                        {
                            // Try parse enum from int
                            safeValue = Enum.ToObject(targetType, kvp.Value);
                        }
                    }
                    else
                    {
                        safeValue = Convert.ChangeType(kvp.Value, targetType);
                    }

                    prop.SetValue(obj, safeValue);
                }
                catch
                {

                }
            }
        }

        return obj;
    }
}