using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Linq;

namespace Oproto.Lambda.GraphQL.SourceGenerator;

/// <summary>
/// Utility for extracting and mapping return types from method symbols to GraphQL types.
/// </summary>
public static class ReturnTypeExtractor
{
    // Cache for expensive ConstructedFrom.ToDisplayString() operations
    private static readonly ConcurrentDictionary<INamedTypeSymbol, string> ConstructedFromCache = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Gets the cached ConstructedFrom display string for a named type symbol.
    /// </summary>
    private static string GetCachedConstructedFrom(INamedTypeSymbol namedType)
    {
        return ConstructedFromCache.GetOrAdd(namedType.ConstructedFrom, key => key.ToDisplayString());
    }
    /// <summary>
    /// Extracts the GraphQL return type from a method symbol.
    /// </summary>
    /// <param name="methodSymbol">The method symbol to extract return type from.</param>
    /// <returns>The GraphQL type name for the return type.</returns>
    public static string ExtractReturnType(IMethodSymbol methodSymbol)
    {
        if (methodSymbol?.ReturnType == null)
            return "String"; // Fallback

        var returnType = methodSymbol.ReturnType;

        // Handle Task<T> and ValueTask<T> - unwrap to get the actual return type
        if (returnType is INamedTypeSymbol namedReturnType && namedReturnType.IsGenericType)
        {
            var constructedFrom = GetCachedConstructedFrom(namedReturnType);
            // Check for Task<T> pattern - the constructed from will be like "System.Threading.Tasks.Task<T>"
            if (constructedFrom.StartsWith("System.Threading.Tasks.Task<") ||
                constructedFrom.StartsWith("System.Threading.Tasks.ValueTask<"))
            {
                // Get the T from Task<T>
                if (namedReturnType.TypeArguments.Length > 0)
                {
                    returnType = namedReturnType.TypeArguments[0];
                }
            }
        }

        // Handle void/Task (no return value)
        if (returnType.SpecialType == SpecialType.System_Void)
        {
            return "Boolean"; // GraphQL operations should return something, use Boolean for void
        }

        // Handle non-generic Task (void async)
        if (returnType.ToDisplayString() == "System.Threading.Tasks.Task")
        {
            return "Boolean";
        }

        // Use TypeMapper to convert the unwrapped type to GraphQL
        return TypeMapper.MapType(returnType);
    }

    /// <summary>
    /// Determines if the return type should be nullable in GraphQL.
    /// </summary>
    /// <param name="methodSymbol">The method symbol to check.</param>
    /// <returns>True if the return type should be nullable in GraphQL.</returns>
    public static bool IsReturnTypeNullable(IMethodSymbol methodSymbol)
    {
        if (methodSymbol?.ReturnType == null)
            return true; // Default to nullable for safety

        var returnType = methodSymbol.ReturnType;

        // Handle Task<T> and ValueTask<T> - unwrap to get the actual return type
        if (returnType is INamedTypeSymbol namedReturnType && namedReturnType.IsGenericType)
        {
            var constructedFrom = GetCachedConstructedFrom(namedReturnType);
            if (constructedFrom.StartsWith("System.Threading.Tasks.Task<") ||
                constructedFrom.StartsWith("System.Threading.Tasks.ValueTask<"))
            {
                // Get the T from Task<T>
                if (namedReturnType.TypeArguments.Length > 0)
                {
                    returnType = namedReturnType.TypeArguments[0];
                }
            }
        }

        // Handle void/Task (no return value) - these are always non-null
        if (returnType.SpecialType == SpecialType.System_Void)
        {
            return false; // Boolean return for void is non-null
        }

        // Handle non-generic Task (void async)
        if (returnType.ToDisplayString() == "System.Threading.Tasks.Task")
        {
            return false;
        }

        // Use TypeMapper to determine nullability
        return !TypeMapper.IsNonNull(returnType);
    }

    /// <summary>
    /// Gets the formatted GraphQL return type string including nullability.
    /// </summary>
    /// <param name="methodSymbol">The method symbol to extract return type from.</param>
    /// <returns>The formatted GraphQL return type (e.g., "Product!", "String", "[Product]").</returns>
    public static string GetFormattedReturnType(IMethodSymbol methodSymbol)
    {
        var baseType = ExtractReturnType(methodSymbol);
        var isNullable = IsReturnTypeNullable(methodSymbol);

        return isNullable ? baseType : $"{baseType}!";
    }
}
