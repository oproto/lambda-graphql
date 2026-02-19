using System.Collections.Generic;

namespace Oproto.Lambda.GraphQL.SourceGenerator.Models;

/// <summary>
/// Represents a GraphQL union type extracted from C# code.
/// </summary>
public sealed class UnionInfo
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> MemberTypes { get; set; } = new();
}
