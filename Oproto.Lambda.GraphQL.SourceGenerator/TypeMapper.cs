using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Oproto.Lambda.GraphQL.SourceGenerator;

/// <summary>
/// Maps C# types to GraphQL types according to the specification.
/// </summary>
public static class TypeMapper
{
    private static readonly Dictionary<string, string> TypeMappings = new()
    {
        { "System.String", "String" },
        { "string", "String" },
        { "System.Int32", "Int" },
        { "int", "Int" },
        { "System.Int64", "Int" },
        { "long", "Int" },
        { "System.Single", "Float" },
        { "float", "Float" },
        { "System.Double", "Float" },
        { "double", "Float" },
        { "System.Decimal", "Float" },
        { "decimal", "Float" },
        { "System.Boolean", "Boolean" },
        { "bool", "Boolean" },
        { "System.Guid", "ID" },
        { "System.DateTime", "AWSDateTime" },
        { "System.DateTimeOffset", "AWSDateTime" },
        { "System.DateOnly", "AWSDate" },
        { "System.TimeOnly", "AWSTime" }
    };

    /// <summary>
    /// Maps a C# type symbol to a GraphQL type name.
    /// </summary>
    public static string MapType(ITypeSymbol typeSymbol)
    {
        // Use fully qualified name for proper AWS scalar mapping
        var typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", ""); // Remove global:: prefix
        
        // Handle nullable value types (e.g., int?)
        if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            if (namedType.ConstructedFrom.ToDisplayString() == "System.Nullable<T>")
            {
                var underlyingType = namedType.TypeArguments[0];
                return MapType(underlyingType);
            }
            
            // Handle collections
            if (namedType.ConstructedFrom.ToDisplayString().StartsWith("System.Collections.Generic.List<") ||
                namedType.ConstructedFrom.ToDisplayString().StartsWith("System.Collections.Generic.IList<") ||
                namedType.ConstructedFrom.ToDisplayString().StartsWith("System.Collections.Generic.IEnumerable<"))
            {
                var elementType = namedType.TypeArguments[0];
                return $"[{MapType(elementType)}]";
            }
            
            // Handle Dictionary<string, T> as AWSJSON
            if (namedType.ConstructedFrom.ToDisplayString().StartsWith("System.Collections.Generic.Dictionary<") &&
                namedType.TypeArguments.Length == 2 &&
                namedType.TypeArguments[0].ToDisplayString() == "System.String")
            {
                return "AWSJSON";
            }
        }
        
        // Handle arrays
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            return $"[{MapType(arrayType.ElementType)}]";
        }
        
        // Check AWS scalar mappings first
        var awsScalarType = AwsScalarMapper.GetAwsScalarType(typeName);
        if (awsScalarType != null)
        {
            return awsScalarType;
        }
        
        // Check built-in type mappings
        if (TypeMappings.TryGetValue(typeName, out var graphqlType))
        {
            return graphqlType;
        }
        
        // Also check the simple name for built-in types
        var simpleName = typeSymbol.Name;
        if (TypeMappings.TryGetValue(simpleName, out var simpleGraphqlType))
        {
            return simpleGraphqlType;
        }
        
        // For custom types, use the type name
        return typeSymbol.Name;
    }

    /// <summary>
    /// Determines if a type should be non-null in GraphQL based on C# nullability.
    /// </summary>
    public static bool IsNonNull(ITypeSymbol typeSymbol)
    {
        // Value types are non-null unless they're Nullable<T>
        if (typeSymbol.IsValueType)
        {
            if (typeSymbol is INamedTypeSymbol namedType && 
                namedType.IsGenericType && 
                namedType.ConstructedFrom.ToDisplayString() == "System.Nullable<T>")
            {
                return false; // Nullable<T> is nullable
            }
            return true; // Regular value types are non-null
        }
        
        // Reference types: check nullability annotation
        return typeSymbol.NullableAnnotation == NullableAnnotation.NotAnnotated;
    }
}
