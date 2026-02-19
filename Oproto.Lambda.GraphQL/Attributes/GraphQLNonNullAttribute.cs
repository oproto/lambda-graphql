using System;

namespace Oproto.Lambda.GraphQL.Attributes;

/// <summary>
/// Overrides nullability for a property, making it non-null in the GraphQL schema.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class GraphQLNonNullAttribute : Attribute
{
}
