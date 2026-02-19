using System;

namespace Oproto.Lambda.GraphQL.Attributes;

/// <summary>
/// Applies a directive to a GraphQL type or field.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = true)]
public sealed class GraphQLApplyDirectiveAttribute : Attribute
{
    public GraphQLApplyDirectiveAttribute(string directiveName)
    {
        DirectiveName = directiveName;
    }

    public string DirectiveName { get; }
    public string? Arguments { get; set; }
}
