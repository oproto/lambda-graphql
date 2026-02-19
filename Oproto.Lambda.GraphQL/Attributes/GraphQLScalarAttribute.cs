using System;

namespace Oproto.Lambda.GraphQL.Attributes;

/// <summary>
/// Marks a class as a custom GraphQL scalar type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class GraphQLScalarAttribute : Attribute
{
    public GraphQLScalarAttribute(string? name = null)
    {
        Name = name;
    }

    public string? Name { get; }
    public string? Description { get; set; }
}
