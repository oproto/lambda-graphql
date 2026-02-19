using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Oproto.Lambda.GraphQL.Runtime;

/// <summary>
/// Filters serialized JSON output to include only fields present in a <see cref="FieldSelection"/>.
/// </summary>
public static class ResponseShaper
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serializes the value to JSON and removes properties not in the selection.
    /// Uses default JsonSerializerOptions with CamelCase naming policy.
    /// </summary>
    public static string ShapeResponse<T>(T value, FieldSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        return ShapeResponse(value, selection, DefaultOptions);
    }

    /// <summary>
    /// Serializes the value to JSON and removes properties not in the selection,
    /// using the provided serializer options.
    /// </summary>
    public static string ShapeResponse<T>(T value, FieldSelection selection, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(options);

        if (value is null)
            return "null";

        var json = JsonSerializer.Serialize(value, options);

        if (selection.IsAll)
            return json;

        using var doc = JsonDocument.Parse(json);
        var typeMap = BuildTypeMap(typeof(T), options);
        return FilterJson(doc.RootElement, selection, typeMap);
    }

    /// <summary>
    /// Holds the reverse lookup (serialized name → C# name) and nested type maps for a given type.
    /// </summary>
    private sealed class TypePropertyMap
    {
        public Dictionary<string, string> ReverseLookup { get; } = new();
        public Dictionary<string, TypePropertyMap> NestedTypes { get; } = new();
    }

    /// <summary>
    /// Builds a recursive type map: for each property, maps serialized JSON name → C# name,
    /// and for complex object properties, recursively builds nested type maps.
    /// </summary>
    private static TypePropertyMap BuildTypeMap(Type type, JsonSerializerOptions options)
    {
        var map = new TypePropertyMap();
        var visited = new HashSet<Type>();
        PopulateTypeMap(map, type, options, visited);
        return map;
    }

    private static void PopulateTypeMap(
        TypePropertyMap map,
        Type type,
        JsonSerializerOptions options,
        HashSet<Type> visited)
    {
        if (!visited.Add(type))
            return;

        var policy = options.PropertyNamingPolicy;
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (prop.GetMethod == null)
                continue;

            var jsonPropAttr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
            string serializedName;

            if (jsonPropAttr != null)
            {
                serializedName = jsonPropAttr.Name;
            }
            else if (policy != null)
            {
                serializedName = policy.ConvertName(prop.Name);
            }
            else
            {
                serializedName = prop.Name;
            }

            map.ReverseLookup[serializedName] = prop.Name;

            // For complex object properties, build nested type maps
            var propType = prop.PropertyType;
            if (IsComplexObjectType(propType))
            {
                var nested = new TypePropertyMap();
                PopulateTypeMap(nested, propType, options, visited);
                map.NestedTypes[prop.Name] = nested;
            }
        }

        visited.Remove(type);
    }

    private static bool IsComplexObjectType(Type type)
    {
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
            type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
            type == typeof(Guid) || type.IsEnum)
            return false;

        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(Nullable<>))
                return IsComplexObjectType(Nullable.GetUnderlyingType(type)!);
        }

        // Arrays and collections are not "complex objects" for our purposes
        if (type.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            return false;

        return type.IsClass || (type.IsValueType && !type.IsPrimitive);
    }

    private static string FilterJson(
        JsonElement element,
        FieldSelection selection,
        TypePropertyMap typeMap)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return element.GetRawText();

        using var stream = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteFilteredObject(writer, element, selection, typeMap);
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteFilteredObject(
        Utf8JsonWriter writer,
        JsonElement element,
        FieldSelection selection,
        TypePropertyMap typeMap)
    {
        writer.WriteStartObject();

        foreach (var property in element.EnumerateObject())
        {
            var csharpName = typeMap.ReverseLookup.TryGetValue(property.Name, out var mapped)
                ? mapped
                : property.Name;

            if (!selection.IsRequested(csharpName))
                continue;

            writer.WritePropertyName(property.Name);

            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                var nestedSelection = selection.ForNestedType(csharpName);
                if (nestedSelection.IsAll)
                {
                    property.Value.WriteTo(writer);
                }
                else
                {
                    var nestedTypeMap = typeMap.NestedTypes.TryGetValue(csharpName, out var nested)
                        ? nested
                        : new TypePropertyMap();
                    WriteFilteredObject(writer, property.Value, nestedSelection, nestedTypeMap);
                }
            }
            else
            {
                property.Value.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
    }
}
