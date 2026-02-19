using System;

namespace Oproto.Lambda.GraphQL.Attributes;

/// <summary>
/// Excludes a property or method from GraphQL schema generation.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class GraphQLIgnoreAttribute : Attribute
{
}
