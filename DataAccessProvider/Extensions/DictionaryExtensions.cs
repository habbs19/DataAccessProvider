using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessProvider.Extensions;
public static class DictionaryExtensions
{
    /// <summary>
    /// Gets values of the specified type from a list of dictionaries, based on the provided key.
    /// </summary>
    /// <typeparam name="T">The type of the values to be extracted.</typeparam>
    /// <param name="source">The list of dictionaries from which values are to be extracted.</param>
    /// <param name="key">The key used to look up the values in each dictionary.</param>
    /// <returns>
    /// A list of values of type <typeparamref name="T"/> extracted from the dictionaries where the specified key exists 
    /// and the value matches the expected type. If no values are found or the key does not exist, an empty list is returned.
    /// </returns>
    public static List<T> GetValuesByKey<T>(this List<Dictionary<string, object>> source, string key)
    {
        var resultList = new List<T>();

        if (source == null || string.IsNullOrEmpty(key))
            return resultList;

        foreach (var dict in source)
        {
            if (dict.ContainsKey(key) && dict[key] is T value)
            {
                resultList.Add(value); 
            }
        }
        return resultList;
    }
}
