using System;

namespace Oproto.Lambda.GraphQL.Attributes;

/// <summary>
/// Marks a class as a GraphQL type (Object, Input, Interface, or Enum).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum)]
public sealed class GraphQLTypeAttribute : Attribute
{
    public GraphQLTypeAttribute(string? name = null)
    {
        Name = name;
    }

    public string? Name { get; }
    public string? Description { get; set; }
    public GraphQLTypeKind Kind { get; set; } = GraphQLTypeKind.Object;
}

/// <summary>
/// Specifies the kind of GraphQL type.
/// </summary>
public enum GraphQLTypeKind
{
    Object,
    Input,
    Interface,
    Enum,
    Union
}
