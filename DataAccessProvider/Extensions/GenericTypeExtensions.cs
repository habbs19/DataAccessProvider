namespace DataAccessProvider.Extensions;

public static class GenericTypeExtensions
{
    public static string GetGenericTypeName(this Type type)
    {
        string typeName;

        if (type.IsGenericType)
        {
            var genericTypes = string.Join(",", type.GetGenericArguments().Select(t => t.Name).ToArray());
            typeName = $"{type.Name.Remove(type.Name.IndexOf('`'))}<{genericTypes}>";
        }
        else
        {
            typeName = type.Name;
        }

        return typeName;
    }

    public static string GetGenericTypeName(this object @object)
    {
        return @object.GetType().GetGenericTypeName();
    }
    public static string GetCleanGenericTypeName(this Type type)
    {
        var typeName = type.IsGenericType
          ? type.GetGenericTypeDefinition().Name.Split('`')[0]  // Removes the "`1" part
          : type.Name;
        return typeName;
    }
}