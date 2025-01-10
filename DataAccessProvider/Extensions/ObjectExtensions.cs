namespace DataAccessProvider.Extensions;
public static class ObjectExtensions
{
    /// <summary>
    /// Attempts to extract a value by key from an object that is expected to be a dictionary.
    /// If the key exists and the value is of the expected type, it returns the value. 
    /// Otherwise, it returns the default value of the specified type.
    /// </summary>
    /// <typeparam name="T">The type to cast the value to.</typeparam>
    /// <param name="source">The object to cast to a dictionary.</param>
    /// <param name="key">The key whose value is to be extracted.</param>
    /// <returns>
    /// The value of type <typeparamref name="T"/> if found and cast successfully, otherwise the default value of <typeparamref name="T"/>.
    /// </returns>
    public static T GetValueByKey<T>(this object source, string key)
    {
        // Cast the object to a dictionary
        var dict = source as Dictionary<string, object>;

        // Check if the dictionary is null or if it doesn't contain the specified key
        if (dict == null || !dict.ContainsKey(key))
            return default(T)!;  // Return default value if key doesn't exist or if the cast fails

        // If the key exists, try to cast the value to the specified type T
        if (dict[key] is T value)
        {
            return value;  // Return the casted value if successful
        }

        return default(T)!;  // Return default if cast fails
    }

    /// <summary>
    /// Attempts to cast an object to a list of dictionaries and extracts values of the specified type from each dictionary based on the provided key.
    /// </summary>
    /// <typeparam name="T">The type to cast the values to.</typeparam>
    /// <param name="source">The object to cast to a list of dictionaries.</param>
    /// <param name="key">The key whose values are to be extracted from each dictionary.</param>
    /// <returns>
    /// A list of values of type <typeparamref name="T"/> if found and cast successfully. 
    /// If the cast or key lookup fails, an empty list is returned.
    /// </returns>
    public static List<T> GetValuesByKey<T>(this object source, string key)
    {
        var resultList = new List<T>();

        // Attempt to cast the object to a List<Dictionary<string, object>>
        var listOfDictionaries = source as List<Dictionary<string, object>>;

        // Return an empty list if the cast fails or if the key is null/empty
        if (listOfDictionaries == null || string.IsNullOrEmpty(key))
            return resultList;

        // Iterate through each dictionary in the list
        foreach (var dict in listOfDictionaries)
        {
            // If the dictionary contains the key and the value can be cast to T, add the value to the result list
            if (dict.ContainsKey(key) && dict[key] is T value)
            {
                resultList.Add(value);
            }
        }

        return resultList;
    }
}
