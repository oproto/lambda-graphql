using System;
using System.Collections.Generic;

namespace Oproto.Lambda.GraphQL.SourceGenerator.Models;

/// <summary>
/// Represents a GraphQL directive definition extracted from C# code.
/// </summary>
public sealed class DirectiveInfo
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DirectiveLocation Locations { get; set; }
    public List<DirectiveArgumentInfo> Arguments { get; set; } = new();
}

/// <summary>
/// Represents a directive argument.
/// </summary>
public sealed class DirectiveArgumentInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
}

/// <summary>
/// Specifies where a directive can be applied (local copy for source generator).
/// </summary>
[Flags]
public enum DirectiveLocation
{
    Query = 1,
    Mutation = 2,
    Subscription = 4,
    Field = 8,
    FragmentDefinition = 16,
    FragmentSpread = 32,
    InlineFragment = 64,
    VariableDefinition = 128,
    Schema = 256,
    Scalar = 512,
    Object = 1024,
    FieldDefinition = 2048,
    ArgumentDefinition = 4096,
    Interface = 8192,
    Union = 16384,
    Enum = 32768,
    EnumValue = 65536,
    InputObject = 131072,
    InputFieldDefinition = 262144
}
