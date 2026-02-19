using System.Collections.Generic;

namespace Oproto.Lambda.GraphQL.SourceGenerator.Models;

/// <summary>
/// Represents a GraphQL resolver configuration.
/// </summary>
public sealed class ResolverInfo
{
    public string TypeName { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ResolverKind Kind { get; set; }
    public string? DataSource { get; set; }
    public string? LambdaFunctionName { get; set; }
    public string? LambdaFunctionLogicalId { get; set; }
    public string? RequestMapping { get; set; }
    public string? ResponseMapping { get; set; }
    public List<string> Functions { get; set; } = new();
    public string ReturnType { get; set; } = "String";
    public List<ArgumentInfo> Arguments { get; set; } = new();
    public List<AppliedDirectiveInfo> Directives { get; set; } = new();
    
    // Lambda Annotations configuration
    public string? ResourceName { get; set; }
    public int? MemorySize { get; set; }
    public int? Timeout { get; set; }
    public List<string> Policies { get; set; } = new();
    public string? Role { get; set; }
    
    // Resolver behavior flags
    public bool UsesLambdaContext { get; set; }
}

/// <summary>
/// Represents a GraphQL field argument.
/// </summary>
public sealed class ArgumentInfo
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public string? DefaultValue { get; set; }
}

/// <summary>
/// Represents the kind of AppSync resolver.
/// </summary>
public enum ResolverKind
{
    Unit,
    Pipeline
}
