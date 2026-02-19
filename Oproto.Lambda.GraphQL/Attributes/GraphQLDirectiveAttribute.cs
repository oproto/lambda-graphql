using System;

namespace Oproto.Lambda.GraphQL.Attributes;

/// <summary>
/// Defines a custom GraphQL directive.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class GraphQLDirectiveAttribute : Attribute
{
    public GraphQLDirectiveAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public string? Description { get; set; }
    public DirectiveLocation Locations { get; set; } = DirectiveLocation.FieldDefinition;
    public string? Arguments { get; set; }
}

/// <summary>
/// Specifies where a directive can be applied.
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
