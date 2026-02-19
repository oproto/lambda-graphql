using System;

namespace Oproto.Lambda.GraphQL.Attributes;

/// <summary>
/// Configures an enum value with GraphQL-specific metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class GraphQLEnumValueAttribute : Attribute
{
    public GraphQLEnumValueAttribute(string? name = null)
    {
        Name = name;
    }

    public string? Name { get; }
    public string? Description { get; set; }
    public bool Deprecated { get; set; }
    public string? DeprecationReason { get; set; }
}
