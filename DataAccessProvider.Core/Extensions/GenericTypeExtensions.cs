using System.Reflection;

namespace DataAccessProvider.Core.Extensions;

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
    /// <summary>
    /// Sets the value of the specified property on the given object, with special handling for enums and nullable types.
    /// </summary>
    /// <typeparam name="TValue">The type of the object on which the property is being set. Must be a reference type.</typeparam>
    /// <param name="property">The property to set the value for.</param>
    /// <param name="obj">The object whose property is being set. This should be an instance of <typeparamref name="TValue"/>.</param>
    /// <param name="value">The value to set for the property. Can be null, a value of the correct type, or a value that can be converted to the correct type.</param>
    /// <exception cref="InvalidCastException">Thrown when the value cannot be cast to the target property type (e.g., when the property is an enum and the value does not match).</exception>
    /// <exception cref="ArgumentException">Thrown when the value is not appropriate for the property (e.g., trying to set a value to a read-only property).</exception>
    /// <exception cref="Exception">Thrown when an unexpected error occurs during the setting of the property.</exception>
    public static void SetPropertyValue<TValue>(this PropertyInfo property,TValue obj,object value) where TValue: class
    {    
        try
        {
            // Handle null or empty values for nullable types
            if (value == null || value == DBNull.Value)
            {
                property.SetValue(obj, null);
            }
            else
            {
                // Check if the property type is an enum
                if (property.PropertyType.IsEnum)
                {
                    // Convert the string value to the appropriate enum
                    var enumValue = Enum.Parse(property.PropertyType, value.ToString()!);
                    property.SetValue(obj, enumValue);
                }
                else
                {
                    // Handle other types normally
                    property.SetValue(obj, Convert.ChangeType(value, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType));
                }
            }
        }
        catch (InvalidCastException e)
        {
            throw new InvalidCastException(
                $"Invalid cast encountered while setting property '{property.Name}' on type '{typeof(TValue).Name}'. " +
                $"Expected property type: {property.PropertyType}, but received value: '{value}' (Type: {value?.GetType()}). " +
                $"Please ensure the types are compatible. Error details: {e.Message}", e);
        }
        catch (ArgumentException e)
        {
            throw new ArgumentException(
                $"Argument issue when setting property '{property.Name}' on type '{typeof(TValue).Name}'. " +
                $"The value '{value}' (Type: {value?.GetType()}) could not be assigned to property of type {property.PropertyType}. " +
                $"Check if the property is read-only or if an invalid argument was passed. Error details: {e.Message}", e);
        }
        catch (Exception e)
        {
            throw new Exception(
                $"Unexpected error occurred while setting property '{property.Name}' on type '{typeof(TValue).Name}'. " +
                $"An issue occurred with value: '{value}' (Type: {value?.GetType()}). " +
                $"Error details: {e.Message}. Please review the stack trace for more information.", e);
        }
    }
}