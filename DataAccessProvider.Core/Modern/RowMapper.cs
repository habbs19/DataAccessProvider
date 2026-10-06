using System.Data.Common;
using System.Globalization;
using System.Reflection;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace DataAccessProvider.Core;

public sealed class MappingException(string member, Type source, Type target, string category)
    : InvalidOperationException($"Cannot map '{member}' from {source.Name} to {target.Name} ({category}).");

internal static class ValueConversion
{
    public static object? ConvertValue(object? value, Type destination, string member)
    {
        var target = Nullable.GetUnderlyingType(destination) ?? destination;
        if (value is null or DBNull) return null;
        try
        {
            if (target.IsInstanceOfType(value)) return value;
            if (target.IsEnum) return value is string text ? Enum.Parse(target, text, true) : Enum.ToObject(target, value);
            if (target == typeof(Guid)) return value is byte[] bytes ? new Guid(bytes) : Guid.Parse((string)value);
            if (target == typeof(DateOnly) && value is DateTime date) return DateOnly.FromDateTime(date);
            if (target == typeof(TimeOnly) && value is TimeSpan time) return TimeOnly.FromTimeSpan(time);
            if (target == typeof(TimeSpan)) return value is long ticks ? TimeSpan.FromTicks(ticks) : TimeSpan.Parse((string)value, CultureInfo.InvariantCulture);
            if (target == typeof(bool) && value is string boolean) return boolean switch { "1" => true, "0" => false, _ => bool.Parse(boolean) };
            return Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            // Conversion exception messages can themselves contain the raw value. Do not retain them as InnerException.
            throw new MappingException(member, value.GetType(), destination, ex.GetType().Name);
        }
    }
}

internal static class RowMapper<T> where T : class, new()
{
    private sealed record Member(PropertyInfo Property, Action<T, object?> Set, bool Nullable);
    private static readonly Dictionary<string, Member> Members = BuildMembers();
    private static Dictionary<string, Member> BuildMembers()
    {
        var nullability = new NullabilityInfoContext();
        return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod?.IsPublic == true && p.GetIndexParameters().Length == 0)
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g =>
            {
                var p = g.First();
                Action<T, object?> setter;
                if (RuntimeFeature.IsDynamicCodeSupported)
                {
                    var instance = Expression.Parameter(typeof(T)); var value = Expression.Parameter(typeof(object));
                    setter = Expression.Lambda<Action<T, object?>>(Expression.Assign(Expression.Property(instance, p), Expression.Convert(value, p.PropertyType)), instance, value).Compile();
                }
                else setter = (instance, value) => p.SetValue(instance, value);
                var nullable = Nullable.GetUnderlyingType(p.PropertyType) != null ||
                    (!p.PropertyType.IsValueType && nullability.Create(p).WriteState != NullabilityState.NotNull);
                return new Member(p, setter, nullable);
            }, StringComparer.OrdinalIgnoreCase);
    }
    public static Func<DbDataReader, T> Create(DbDataReader reader, bool strict = true)
    {
        var columns = Enumerable.Range(0, reader.FieldCount).Where(i => Members.ContainsKey(reader.GetName(i)))
            .Select(i => (Ordinal: i, Member: Members[reader.GetName(i)])).ToArray();
        return row =>
        {
            var result = new T();
            foreach (var (ordinal, member) in columns)
            {
                if (row.IsDBNull(ordinal))
                {
                    if (member.Nullable) member.Set(result, null);
                    else if (strict) throw new MappingException(member.Property.Name, typeof(DBNull), member.Property.PropertyType, "Null");
                    continue;
                }
                member.Set(result, ValueConversion.ConvertValue(row.GetValue(ordinal), member.Property.PropertyType, member.Property.Name));
            }
            return result;
        };
    }
}
