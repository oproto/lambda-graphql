using System;

namespace Oproto.Lambda.GraphQL.Attributes;

/// <summary>
/// Configures resolver settings for a GraphQL operation.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class GraphQLResolverAttribute : Attribute
{
    public string? DataSource { get; set; }
    public ResolverKind Kind { get; set; } = ResolverKind.Unit;
    public string[]? Functions { get; set; }
    public string? RequestMapping { get; set; }
    public string? ResponseMapping { get; set; }
}

/// <summary>
/// Specifies the kind of AppSync resolver.
/// </summary>
public enum ResolverKind
{
    Unit,
    Pipeline
}
