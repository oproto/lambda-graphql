# Architecture

This document explains the architecture and design of Oproto.Lambda.GraphQL, including how the source generator works, the data flow through the system, and key design decisions.

## Overview

Oproto.Lambda.GraphQL is built around three core components that work together to provide compile-time GraphQL schema generation:

1. **Attributes Library** - Provides GraphQL metadata through C# attributes
2. **Source Generator** - Analyzes code and generates GraphQL schemas at compile time
3. **MSBuild Task** - Extracts generated schemas and writes output files

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  Developer Code                                                  │
│  • C# classes with [GraphQLType] attributes                     │
│  • Lambda functions with [GraphQLQuery/Mutation] attributes     │
│  • Assembly-level [GraphQLSchema] configuration                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  Roslyn Source Generator (compile-time)                         │
│  • Analyzes syntax trees and semantic models                    │
│  • Extracts type definitions and resolver mappings              │
│  • Generates GraphQL SDL as assembly metadata                   │
│  • Generates resolver manifest JSON                             │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  MSBuild Task (post-build)                                      │
│  • Extracts SDL from generated assembly metadata                │
│  • Writes schema.graphql to output directory                    │
│  • Writes resolvers.json manifest                               │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  Output Files                                                    │
│  • schema.graphql - GraphQL SDL schema                          │
│  • resolvers.json - Resolver configuration manifest             │
└─────────────────────────────────────────────────────────────────┘
```

## Component Details

### 1. Attributes Library (`Oproto.Lambda.GraphQL`)

The attributes library provides the developer-facing API for defining GraphQL schemas through C# attributes.

#### Package Structure

```
Oproto.Lambda.GraphQL/
├── Attributes/
│   ├── GraphQLTypeAttribute.cs          # Type definitions
│   ├── GraphQLFieldAttribute.cs         # Field metadata
│   ├── GraphQLArgumentAttribute.cs      # Argument definitions
│   ├── GraphQLQueryAttribute.cs         # Query operations
│   ├── GraphQLMutationAttribute.cs      # Mutation operations
│   ├── GraphQLSubscriptionAttribute.cs  # Subscription operations
│   ├── GraphQLUnionAttribute.cs         # Union types
│   ├── GraphQLDirectiveAttribute.cs     # Custom directives
│   ├── GraphQLAuthDirectiveAttribute.cs # AWS auth directives
│   ├── GraphQLTimestampAttribute.cs     # Timestamp scalar override
│   ├── GraphQLResolverAttribute.cs      # Resolver configuration
│   ├── GraphQLSchemaAttribute.cs        # Assembly-level schema info
│   ├── GraphQLEnumValueAttribute.cs     # Enum value metadata
│   ├── GraphQLIgnoreAttribute.cs        # Exclude from schema
│   ├── GraphQLNonNullAttribute.cs       # Nullability override
│   ├── GraphQLScalarAttribute.cs        # Custom scalar types
│   └── GraphQLApplyDirectiveAttribute.cs # Apply directives
└── build/
    ├── Oproto.Lambda.GraphQL.props             # MSBuild properties
    └── Oproto.Lambda.GraphQL.targets           # MSBuild targets
```

#### Design Principles

- **Zero Runtime Dependencies** - All attributes are compile-time only
- **AOT Compatibility** - No reflection or dynamic code generation at runtime
- **Type Safety** - Leverage C# type system for GraphQL schema validation
- **Developer Experience** - IntelliSense support and compile-time validation

### 2. Source Generator (`Oproto.Lambda.GraphQL.SourceGenerator`)

The source generator is a Roslyn-based incremental generator that analyzes C# code and produces GraphQL schemas.

#### Generator Pipeline

```csharp
[Generator(LanguageNames.CSharp)]
public partial class GraphQLSchemaGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Find classes/enums with GraphQL attributes
        var typeDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => IsGraphQLType(s),
                transform: (ctx, _) => ExtractTypeInfoWithDiagnostics(ctx))
            .Where(t => t.result != null);

        // 2. Find Lambda functions with GraphQL operation attributes
        var operationDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => IsGraphQLOperation(s),
                transform: (ctx, _) => ExtractOperationInfoWithDiagnostics(ctx))
            .Where(o => o.result != null);

        // 3. Combine types, operations, and compilation
        var combined = typeDeclarations.Collect()
            .Combine(operationDeclarations.Collect())
            .Combine(context.CompilationProvider);

        // 4. Generate schema
        context.RegisterSourceOutput(combined, GenerateSchema);
    }
}
```

#### Key Components

**Type Extraction**
- Analyzes classes, interfaces, and enums with GraphQL attributes
- Extracts field information, nullability, and metadata
- Handles inheritance and interface implementations
- Maps C# types to GraphQL types using `TypeMapper`

**Operation Extraction**
- Finds Lambda functions with GraphQL operation attributes
- Extracts method signatures and parameter information
- Builds resolver configuration for AppSync
- Handles authentication and authorization metadata

**Schema Generation**
- Combines type and operation information
- Generates GraphQL SDL using `SdlGenerator`
- Creates resolver manifest using `ResolverManifestGenerator`
- Embeds output as assembly metadata

#### Data Models

```csharp
// Core type information
public sealed class TypeInfo
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public TypeKind Kind { get; set; }
    public List<FieldInfo> Fields { get; set; }
    public List<string> UnionMembers { get; set; }
    public List<string> InterfaceImplementations { get; set; }
    public List<EnumValueInfo> EnumValues { get; set; }
}

// Resolver configuration
public sealed class ResolverInfo
{
    public string TypeName { get; set; }
    public string FieldName { get; set; }
    public string? Description { get; set; }
    public ResolverKind Kind { get; set; }
    public string? DataSource { get; set; }
    public string LambdaFunctionName { get; set; }
    public List<ArgumentInfo> Arguments { get; set; }
}
```

### 3. MSBuild Task (`Oproto.Lambda.GraphQL.Build`)

The MSBuild task extracts the generated schema from the compiled assembly and writes it to output files.

#### Task Implementation

```csharp
public class ExtractGraphQLSchemaTask : Task
{
    public override bool Execute()
    {
        try
        {
            // Load the compiled assembly
            using var context = new MetadataLoadContext(resolver);
            var assembly = context.LoadFromAssemblyPath(AssemblyPath);
            
            // Extract schema from assembly metadata
            var schemaMetadata = assembly.GetCustomAttributesData()
                .FirstOrDefault(attr => attr.AttributeType.Name == "AssemblyMetadataAttribute" &&
                                       attr.ConstructorArguments[0].Value?.ToString() == "GraphQL.Schema");
            
            if (schemaMetadata != null)
            {
                var schema = schemaMetadata.ConstructorArguments[1].Value?.ToString();
                if (!string.IsNullOrEmpty(schema))
                {
                    var schemaPath = Path.Combine(OutputPath, "schema.graphql");
                    File.WriteAllText(schemaPath, schema);
                }
            }
            
            // Extract resolver manifest
            var resolverMetadata = assembly.GetCustomAttributesData()
                .FirstOrDefault(attr => attr.AttributeType.Name == "AssemblyMetadataAttribute" &&
                                       attr.ConstructorArguments[0].Value?.ToString() == "GraphQL.ResolverManifest");
            
            if (resolverMetadata != null)
            {
                var manifest = resolverMetadata.ConstructorArguments[1].Value?.ToString();
                if (!string.IsNullOrEmpty(manifest))
                {
                    var manifestPath = Path.Combine(OutputPath, "resolvers.json");
                    File.WriteAllText(manifestPath, manifest);
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Log.LogError($"Failed to extract GraphQL schema: {ex.Message}");
            return false;
        }
    }
}
```

## Design Decisions

### 1. Compile-Time vs Runtime Generation

**Decision**: Generate schemas at compile time using source generators.

**Rationale**:
- **Performance** - Zero runtime overhead
- **AOT Compatibility** - Works with Native AOT compilation
- **Developer Experience** - Immediate feedback during development
- **Type Safety** - Compile-time validation of GraphQL schemas

**Trade-offs**:
- More complex implementation
- Requires understanding of Roslyn APIs
- Limited to information available at compile time

### 2. Attribute-Based vs Convention-Based

**Decision**: Use explicit attributes for GraphQL metadata.

**Rationale**:
- **Explicit Control** - Developers specify exactly what should be in the schema
- **Flexibility** - Support for custom names, descriptions, and configurations
- **Clarity** - Clear intent in the code about GraphQL mapping
- **Compatibility** - Works with existing C# codebases without changes

**Trade-offs**:
- More verbose than convention-based approaches
- Requires learning the attribute API
- Potential for inconsistency if not used systematically

### 3. Multi-Package Architecture

**Decision**: Split functionality across multiple NuGet packages.

**Rationale**:
- **Separation of Concerns** - Clear boundaries between components
- **Dependency Management** - Consumers only get what they need
- **Versioning** - Independent versioning of components
- **Build Integration** - MSBuild tasks separate from runtime attributes

**Packages**:
- `Oproto.Lambda.GraphQL` - Main package with attributes (runtime dependency)
- `Oproto.Lambda.GraphQL.SourceGenerator` - Source generator (build-time dependency)
- `Oproto.Lambda.GraphQL.Build` - MSBuild tasks (build-time dependency)

### 4. AWS AppSync Focus

**Decision**: Target AWS AppSync specifically rather than generic GraphQL.

**Rationale**:
- **Integration** - Deep integration with AWS services and CDK
- **Scalar Types** - Support for AWS-specific scalar types
- **Authentication** - Built-in support for AWS auth patterns
- **Resolver Configuration** - Generate AppSync-compatible resolver manifests

**Trade-offs**:
- Not portable to other GraphQL servers
- Tied to AWS ecosystem
- May not support all GraphQL features

## Type System

### C# to GraphQL Mapping

The type mapper handles conversion from C# types to GraphQL types:

```csharp
public static class TypeMapper
{
    public static string MapType(ITypeSymbol typeSymbol)
    {
        // 1. Check AWS scalar mappings first
        var awsScalarType = AwsScalarMapper.GetAwsScalarType(typeName);
        if (awsScalarType != null) return awsScalarType;
        
        // 2. Handle nullable value types (int?)
        if (IsNullableValueType(typeSymbol))
            return MapType(GetUnderlyingType(typeSymbol));
        
        // 3. Handle collections (List<T>, T[])
        if (IsCollectionType(typeSymbol))
            return $"[{MapType(GetElementType(typeSymbol))}]";
        
        // 4. Handle Dictionary<string, T> as AWSJSON
        if (IsDictionaryType(typeSymbol))
            return "AWSJSON";
        
        // 5. Check built-in GraphQL type mappings
        if (TypeMappings.TryGetValue(typeName, out var graphqlType))
            return graphqlType;
        
        // 6. Use custom type name for classes/enums
        return typeSymbol.Name;
    }
}
```

### Nullability Handling

GraphQL nullability is the inverse of C# nullability:

| C# Type | C# Nullability | GraphQL Type | GraphQL Nullability |
|---------|----------------|--------------|---------------------|
| `string` | Nullable reference | `String` | Nullable |
| `string?` | Explicit nullable | `String` | Nullable |
| `int` | Non-nullable value | `Int` | Non-null (`Int!`) |
| `int?` | Nullable value | `Int` | Nullable |

Override with `[GraphQLNonNull]`:

```csharp
[GraphQLField]
[GraphQLNonNull]
public string? RequiredField { get; set; } // → String!
```

## Source Generator Implementation

### Incremental Generation

Oproto.Lambda.GraphQL uses Roslyn's incremental generator pattern for optimal performance:

```csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
{
    // Create syntax providers for types and operations
    var typeDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
        predicate: IsGraphQLType,
        transform: ExtractTypeInfo);
    
    var operationDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
        predicate: IsGraphQLOperation,
        transform: ExtractOperationInfo);
    
    // Combine and generate
    var combined = typeDeclarations.Collect()
        .Combine(operationDeclarations.Collect())
        .Combine(context.CompilationProvider);
    
    context.RegisterSourceOutput(combined, GenerateSchema);
}
```

### Error Handling

The generator includes comprehensive error handling and diagnostics:

```csharp
private static (object? result, IEnumerable<Diagnostic> diagnostics) 
    ExtractTypeInfoWithDiagnostics(GeneratorSyntaxContext context)
{
    try
    {
        // Type extraction logic
        return (typeInfo, Enumerable.Empty<Diagnostic>());
    }
    catch (ArgumentException ex)
    {
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.TypeExtractionError,
            context.Node.GetLocation(),
            context.Node.ToString(),
            ex.Message);
        return (null, new[] { diagnostic });
    }
}
```

### Performance Optimizations

- **Incremental Generation** - Only regenerates when relevant code changes
- **Caching** - Caches expensive operations like type symbol resolution
- **Lazy Evaluation** - Defers expensive operations until needed
- **StringBuilder Usage** - Efficient string building for large schemas

## Data Flow

### 1. Code Analysis Phase

```
C# Source Code
    ↓
Syntax Tree Analysis
    ↓
Semantic Model Analysis
    ↓
Attribute Extraction
    ↓
Type Information Models
```

### 2. Schema Generation Phase

```
Type Information Models
    ↓
SDL Generation (SdlGenerator)
    ↓
Resolver Manifest Generation
    ↓
Assembly Metadata Embedding
```

### 3. Build Integration Phase

```
Compiled Assembly
    ↓
MSBuild Task Execution
    ↓
Metadata Extraction
    ↓
File Output (schema.graphql, resolvers.json)
```

## Key Design Patterns

### 1. Incremental Source Generation

Uses Roslyn's incremental generator pattern for optimal build performance:

- **Syntax Providers** - Efficiently filter relevant syntax nodes
- **Semantic Analysis** - Only analyze nodes that pass syntax filtering
- **Caching** - Cache expensive semantic model operations
- **Dependency Tracking** - Only regenerate when dependencies change

### 2. Diagnostic Reporting

Comprehensive error reporting with actionable messages:

```csharp
public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor TypeExtractionError = new(
        "LGQ001",
        "Type extraction error",
        "Failed to extract GraphQL type information from '{0}': {1}",
        "Oproto.Lambda.GraphQL",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
```

### 3. Extensible Type System

Modular type system that supports extension:

- **Type Mappers** - Pluggable type mapping strategies
- **AWS Scalar Mapper** - Specialized AWS type mappings
- **Custom Scalars** - Support for user-defined scalar types
- **Directive System** - Extensible directive definitions and applications

## Security Considerations

### 1. Input Validation

- **Attribute Parameter Validation** - Validate attribute parameters at compile time
- **Type Safety** - Leverage C# type system to prevent invalid schemas
- **Null Safety** - Proper handling of nullable reference types

### 2. Code Generation Safety

- **String Escaping** - Proper escaping of generated string literals
- **Path Validation** - Validate file paths in MSBuild task
- **Assembly Loading** - Safe assembly loading with proper disposal

### 3. AWS Integration Security

- **Auth Directive Generation** - Generate but don't enforce auth directives
- **IAM Resource Validation** - Basic validation of IAM resource ARNs
- **Cognito Group Validation** - Validate Cognito group names

## Performance Characteristics

### Compile-Time Performance

- **Incremental Generation** - Only processes changed files
- **Efficient Filtering** - Syntax-based filtering before semantic analysis
- **Cached Operations** - Cache expensive reflection operations
- **Parallel Processing** - Leverage Roslyn's parallel processing capabilities

### Runtime Performance

- **Zero Overhead** - No runtime dependencies or reflection
- **AOT Compatible** - Works with Native AOT compilation
- **Memory Efficient** - No runtime schema objects or caches

### Build Integration Performance

- **Conditional Execution** - MSBuild task only runs when needed
- **Fast Assembly Loading** - Efficient metadata-only assembly loading
- **Minimal File I/O** - Only write files when content changes

## Extensibility Points

### 1. Custom Type Mappers

Add support for new type mappings:

```csharp
public static class CustomTypeMapper
{
    public static string? MapCustomType(ITypeSymbol typeSymbol)
    {
        // Custom mapping logic
        return null;
    }
}
```

### 2. Custom Scalar Types

Define new scalar types:

```csharp
[GraphQLScalar("CustomDateTime")]
public class CustomDateTime
{
    public DateTime Value { get; set; }
}
```

### 3. Custom Directives

Define application-specific directives:

```csharp
[assembly: GraphQLDirective("validate",
    Locations = DirectiveLocation.ArgumentDefinition,
    Arguments = "pattern: String!")]
```

## Testing Architecture

### 1. Unit Testing

- **Attribute Testing** - Test attribute behavior and properties
- **Type Mapping Testing** - Test C# to GraphQL type mappings
- **SDL Generation Testing** - Test schema generation logic
- **Resolver Manifest Testing** - Test resolver configuration generation

### 2. Integration Testing

- **End-to-End Testing** - Test complete schema generation pipeline
- **Build Integration Testing** - Test MSBuild task execution
- **Example Validation** - Test all documentation examples

### 3. Property-Based Testing

- **Schema Validity** - Ensure all generated schemas are valid GraphQL
- **Deterministic Output** - Same input always produces same output
- **Round-Trip Testing** - Generated schemas can be parsed and validated

## Future Architecture Considerations

### 1. Federation Support

Potential support for Apollo Federation:
- `@key` directive support
- `@external` field marking
- `@requires` dependency specification
- `@provides` field provision

### 2. Schema Validation

Enhanced compile-time validation:
- GraphQL specification compliance
- AppSync limitation checking
- Circular reference detection
- Field resolution validation

### 3. Performance Monitoring

Build-time performance monitoring:
- Generation time metrics
- Memory usage tracking
- Cache hit/miss ratios
- Diagnostic reporting

---

For implementation details and code examples, see the [Source Code](https://github.com/oproto/lambda-graphql) and [Contributing Guide](contributing.md).
