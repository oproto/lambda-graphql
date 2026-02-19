using System;

namespace Oproto.Lambda.GraphQL.Attributes;

/// <summary>
/// Marks a Lambda function as a GraphQL subscription operation.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class GraphQLSubscriptionAttribute : Attribute
{
    public GraphQLSubscriptionAttribute(string? name = null)
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
