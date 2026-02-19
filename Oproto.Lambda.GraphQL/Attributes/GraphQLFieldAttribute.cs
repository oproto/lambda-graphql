using System;

namespace Oproto.Lambda.GraphQL.Attributes;

/// <summary>
/// Configures a property or method as a GraphQL field.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class GraphQLFieldAttribute : Attribute
{
    public GraphQLFieldAttribute(string? name = null)
    {
        Name = name;
    }

    public string? Name { get; }
    public string? Description { get; set; }
    public bool Deprecated { get; set; }
    public string? DeprecationReason { get; set; }
}
