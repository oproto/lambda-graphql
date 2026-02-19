using System;

namespace Oproto.Lambda.GraphQL.Attributes;

/// <summary>
/// Marks a class as a GraphQL union type with specified member types.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class GraphQLUnionAttribute : Attribute
{
    public GraphQLUnionAttribute(string? name = null, params string[] memberTypes)
    {
        Name = name;
        MemberTypes = memberTypes ?? Array.Empty<string>();
    }

    public string? Name { get; }
    public string? Description { get; set; }
    public string[] MemberTypes { get; }
}
