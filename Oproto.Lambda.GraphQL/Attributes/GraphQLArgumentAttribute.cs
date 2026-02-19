using System;

namespace Oproto.Lambda.GraphQL.Attributes;

/// <summary>
/// Configures a method parameter as a GraphQL argument.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class GraphQLArgumentAttribute : Attribute
{
    public GraphQLArgumentAttribute(string? name = null)
    {
        Name = name;
    }

    public string? Name { get; }
    public string? Description { get; set; }
    public bool Deprecated { get; set; }
    public string? DeprecationReason { get; set; }
}
