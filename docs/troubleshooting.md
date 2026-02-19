# Troubleshooting Guide

This guide helps you resolve common issues when using Oproto.Lambda.GraphQL.

## Table of Contents

- [Build Issues](#build-issues)
- [Schema Generation Issues](#schema-generation-issues)
- [Source Generator Issues](#source-generator-issues)
- [Type Mapping Issues](#type-mapping-issues)
- [Resolver Configuration Issues](#resolver-configuration-issues)
- [AWS AppSync Integration Issues](#aws-appsync-integration-issues)

---

## Build Issues

### Schema Files Not Generated

**Symptom**: `schema.graphql` or `resolvers.json` files are not created after build.

**Possible Causes**:
1. MSBuild task not running
2. No GraphQL types or operations defined
3. Build output directory incorrect

**Solutions**:

```bash
# 1. Clean and rebuild
dotnet clean
dotnet build

# 2. Check build output for extraction messages
dotnet build -v detailed | grep "GraphQL"

# 3. Verify Oproto.Lambda.GraphQL package is referenced
dotnet list package | grep Oproto.Lambda.GraphQL
```

**Expected Output**:
```
Oproto.Lambda.GraphQL: Extracting schema from /path/to/bin/Debug/net6.0/YourProject.dll
Generated GraphQL schema: /path/to/schema.graphql
Generated resolver manifest: /path/to/resolvers.json
```

### Build Fails with "Could not load file or assembly"

**Symptom**: Build error about missing assemblies.

**Solution**:
```bash
# Restore packages
dotnet restore

# Clear NuGet cache if needed
dotnet nuget locals all --clear
dotnet restore
```

### Source Generator Not Running

**Symptom**: No generated code, no compile-time errors for invalid GraphQL definitions.

**Possible Causes**:
1. Source generator DLL not loaded
2. IDE/build server caching old generator

**Solutions**:

```bash
# Shutdown build server (CRITICAL after generator changes)
dotnet build-server shutdown

# Clean and rebuild
dotnet clean
dotnet build
```

**For Visual Studio**:
- Close Visual Studio
- Delete `bin/` and `obj/` directories
- Reopen and rebuild

**For Rider**:
- File → Invalidate Caches / Restart
- Choose "Invalidate and Restart"

---

## Schema Generation Issues

### Empty Schema Generated

**Symptom**: `schema.graphql` exists but contains only schema definition, no types.

**Cause**: No types or operations marked with GraphQL attributes.

**Solution**:

```csharp
// Ensure types have [GraphQLType] attribute
[GraphQLType("Product")]
public class Product
{
    [GraphQLField] public Guid Id { get; set; }
    [GraphQLField] public string Name { get; set; }
}

// Ensure operations have operation attributes
[LambdaFunction]
[GraphQLQuery("getProduct")]
[GraphQLResolver(DataSource = "ProductsLambda")]
public async Task<Product> GetProduct(Guid id) { }
```

### Incorrect GraphQL Syntax in Generated Schema

**Symptom**: AppSync rejects schema with syntax errors.

**Common Issues**:

1. **Missing required fields**:
```csharp
// ❌ Wrong - nullable reference type becomes optional field
[GraphQLField] public string? Name { get; set; }

// ✅ Correct - non-nullable for required fields
[GraphQLField] public string Name { get; set; } = string.Empty;
```

2. **Invalid type names**:
```csharp
// ❌ Wrong - GraphQL names must be alphanumeric
[GraphQLType("Product-Type")]

// ✅ Correct
[GraphQLType("ProductType")]
```

3. **Circular references**:
```csharp
// ❌ Wrong - circular reference
[GraphQLType("User")]
public class User
{
    [GraphQLField] public List<User> Friends { get; set; }
}

// ✅ Correct - use nullable or separate connection type
[GraphQLType("User")]
public class User
{
    [GraphQLField] public List<User>? Friends { get; set; }
}
```

### Descriptions Not Appearing

**Symptom**: Generated schema lacks descriptions.

**Solution**:

```csharp
// Add Description parameter to attributes
[GraphQLType("Product", Description = "A product in the catalog")]
public class Product
{
    [GraphQLField(Description = "Unique product identifier")]
    public Guid Id { get; set; }
}

[GraphQLQuery("getProduct", Description = "Get a product by ID")]
public async Task<Product> GetProduct(
    [GraphQLArgument(Description = "Product ID")] Guid id) { }
```

---

## Source Generator Issues

### Compile Errors in Generated Code

**Symptom**: Build fails with errors in generated files.

**Diagnosis**:
```bash
# View generated files
find obj -name "*GraphQLSchemaGenerator*.cs" -type f
```

**Common Causes**:
1. Invalid attribute parameters
2. Unsupported type in GraphQL schema
3. Generator bug

**Solution**:
```bash
# Check for diagnostic warnings
dotnet build -v detailed

# Report issue with minimal reproduction
# Include: attribute usage, type definitions, error message
```

### Generator Diagnostics/Warnings

**Symptom**: Build warnings about GraphQL schema issues.

**Common Warnings**:

1. **GRAPHQL001**: Missing return type
```csharp
// ❌ Wrong - void return type
[GraphQLQuery("doSomething")]
public void DoSomething() { }

// ✅ Correct
[GraphQLQuery("doSomething")]
public async Task<bool> DoSomething() { }
```

2. **GRAPHQL002**: Invalid resolver configuration
```csharp
// ❌ Wrong - missing DataSource
[GraphQLQuery("getProduct")]
public async Task<Product> GetProduct(Guid id) { }

// ✅ Correct
[GraphQLQuery("getProduct")]
[GraphQLResolver(DataSource = "ProductsLambda")]
public async Task<Product> GetProduct(Guid id) { }
```

---

## Type Mapping Issues

### C# Type Not Mapping to GraphQL Type

**Symptom**: Build error or incorrect GraphQL type generated.

**Supported Mappings**:

| C# Type | GraphQL Type | Notes |
|---------|--------------|-------|
| `string` | `String!` | Non-nullable by default |
| `string?` | `String` | Nullable |
| `int`, `long` | `Int!` | |
| `float`, `double`, `decimal` | `Float!` | |
| `bool` | `Boolean!` | |
| `Guid` | `ID!` | |
| `DateTime` | `AWSDateTime!` | ISO 8601 |
| `DateOnly` | `AWSDate` | YYYY-MM-DD |
| `TimeOnly` | `AWSTime` | HH:mm:ss.SSS |
| `MailAddress` | `AWSEmail!` | |
| `IPAddress` | `IPAddress` | |
| `List<T>` | `[T]!` | Non-null list |
| `List<T>?` | `[T]` | Nullable list |

**Unsupported Types**:

```csharp
// ❌ Wrong - Dictionary not supported
[GraphQLField] public Dictionary<string, object> Metadata { get; set; }

// ✅ Correct - use AWSJSON scalar
[GraphQLField] public string Metadata { get; set; } // Store as JSON string
```

### Timestamp Mapping Issues

**Symptom**: `long` or `ulong` mapped incorrectly.

**Solution**:

```csharp
// For Unix timestamps, use explicit attribute
[GraphQLField]
[GraphQLTimestamp]
public long CreatedAtTimestamp { get; set; }

// For regular numbers, use int or long without attribute
[GraphQLField]
public long Count { get; set; } // Maps to Int
```

### Enum Values Not Generating

**Symptom**: Enum type is empty or missing values.

**Solution**:

```csharp
// Ensure enum has [GraphQLType] attribute
[GraphQLType("OrderStatus")]
public enum OrderStatus
{
    [GraphQLEnumValue(Description = "Order is pending")]
    Pending,
    
    [GraphQLEnumValue(Description = "Order is processing")]
    Processing,
    
    [GraphQLEnumValue(Description = "Order is complete")]
    Complete
}
```

---

## Resolver Configuration Issues

### Resolvers Not in Manifest

**Symptom**: `resolvers.json` missing expected resolvers.

**Checklist**:
- [ ] Method has `[LambdaFunction]` attribute
- [ ] Method has operation attribute (`[GraphQLQuery]`, `[GraphQLMutation]`, `[GraphQLSubscription]`)
- [ ] Method has `[GraphQLResolver]` attribute with DataSource
- [ ] Method is public
- [ ] Method returns Task<T> or T

**Example**:
```csharp
[LambdaFunction] // ✅ Required
[GraphQLQuery("getProduct")] // ✅ Required
[GraphQLResolver(DataSource = "ProductsLambda")] // ✅ Required
public async Task<Product> GetProduct(Guid id) // ✅ Public, returns Task<T>
{
    // Implementation
}
```

### Data Source Configuration Incorrect

**Symptom**: CDK deployment fails with data source errors.

**Solution**:

```csharp
// Ensure DataSource name matches across resolvers
[GraphQLResolver(DataSource = "ProductsLambda")] // Use consistent naming

// In CDK, create matching data source:
const dataSource = api.addLambdaDataSource('ProductsLambda', productFunction);
```

---

## AWS AppSync Integration Issues

### Schema Upload Fails

**Symptom**: AppSync rejects schema with validation errors.

**Common Issues**:

1. **Unsupported directives**:
```graphql
# ❌ Wrong - custom directive not defined
type User @customDirective {
  id: ID!
}

# ✅ Correct - use AWS directives or define custom
type User @aws_cognito_user_pools {
  id: ID!
}
```

2. **Invalid scalar types**:
```graphql
# ❌ Wrong - custom scalar not defined
type User {
  metadata: CustomScalar
}

# ✅ Correct - use AWS scalars or define custom
type User {
  metadata: AWSJSON
}
```

### Resolver Execution Fails

**Symptom**: GraphQL queries return errors in AppSync.

**Debugging Steps**:

1. **Check CloudWatch Logs**:
```bash
aws logs tail /aws/lambda/YourFunction --follow
```

2. **Verify Lambda Permissions**:
```bash
aws lambda get-policy --function-name YourFunction
```

3. **Test Lambda Directly**:
```bash
aws lambda invoke \
  --function-name YourFunction \
  --payload '{"arguments": {"id": "123"}}' \
  response.json
```

### Auth Directives Not Working

**Symptom**: Authorization fails despite correct directives.

**Solution**:

```csharp
// Ensure auth directive is properly configured
[GraphQLType("User")]
[GraphQLAuthDirective(AuthMode.UserPools, CognitoGroups = "admin")]
public class User { }

// In AppSync, ensure auth mode is enabled:
// - API_KEY
// - AWS_IAM
// - AMAZON_COGNITO_USER_POOLS
// - OPENID_CONNECT
```

---

## Performance Issues

### Slow Build Times

**Symptom**: Build takes longer than expected.

**Solutions**:

1. **Use incremental builds**:
```bash
# Don't clean unless necessary
dotnet build # Instead of: dotnet clean && dotnet build
```

2. **Disable source generator for development**:
```xml
<!-- In .csproj, temporarily disable -->
<PropertyGroup>
  <EmitCompilerGeneratedFiles>false</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

3. **Optimize test runs**:
```bash
# Run specific tests
dotnet test --filter "FullyQualifiedName~TypeMapperTests"

# Skip build if already built
dotnet test --no-build
```

### Large Schema Generation

**Symptom**: Schema file is very large, build is slow.

**Solutions**:

1. **Split into multiple schemas** (if using federation)
2. **Remove unnecessary descriptions** (if not needed)
3. **Use interfaces** to reduce duplication

---

## Getting Help

### Before Reporting Issues

1. **Check this guide** for common solutions
2. **Search existing issues** on GitHub
3. **Verify your setup**:
   ```bash
   dotnet --version # Should be 6.0+
   dotnet list package | grep Oproto.Lambda.GraphQL
   ```

### Reporting Issues

Include:
- Oproto.Lambda.GraphQL version
- .NET SDK version
- Minimal reproduction code
- Full error message
- Build output (with `-v detailed`)
- Generated schema (if applicable)

### Community Support

- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: Questions and community help
- **Stack Overflow**: Tag with `lambda-graphql` and `aws-appsync`

---

## Additional Resources

- [Getting Started Guide](getting-started.md)
- [API Reference](api-reference.md)
- [Architecture Documentation](architecture.md)
- [AWS AppSync Documentation](https://docs.aws.amazon.com/appsync/)
- [GraphQL Specification](https://spec.graphql.org/)
