using System;

namespace Oproto.Lambda.GraphQL.Attributes;

/// <summary>
/// Configures assembly-level GraphQL schema information.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class GraphQLSchemaAttribute : Attribute
{
    public GraphQLSchemaAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public string? Description { get; set; }
    public string? Version { get; set; }
}
