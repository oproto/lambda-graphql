using System;

namespace Oproto.Lambda.GraphQL.Attributes;

/// <summary>
/// Marks a Lambda function as a GraphQL query operation.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class GraphQLQueryAttribute : Attribute
{
    public GraphQLQueryAttribute(string? name = null)
    {
        Name = name;
    }

    public string? Name { get; }
    public string? Description { get; set; }
    
    /// <summary>
    /// Explicit GraphQL return type override. Use for union types or when the C# return type
    /// doesn't map directly to the desired GraphQL type (e.g., "SearchResult" for a union).
    /// </summary>
    public string? ReturnType { get; set; }
}

/// <summary>
/// Marks a Lambda function as a GraphQL mutation operation.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class GraphQLMutationAttribute : Attribute
{
    public GraphQLMutationAttribute(string? name = null)
    {
        Name = name;
    }

    public string? Name { get; }
    public string? Description { get; set; }
    
    /// <summary>
    /// Explicit GraphQL return type override. Use for union types or when the C# return type
    /// doesn't map directly to the desired GraphQL type.
    /// </summary>
    public string? ReturnType { get; set; }
}
