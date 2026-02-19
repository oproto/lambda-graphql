# Resolver Manifest (resolvers.json)

The `resolvers.json` file is generated during build and provides metadata for CDK deployment of your GraphQL API.

## Purpose

This manifest bridges the gap between your C# Lambda functions and AWS AppSync infrastructure, enabling automated CDK deployment with Lambda Annotations configuration.

## Key Fields Explained

### Resolver Configuration

```json
{
  "typeName": "Query",
  "fieldName": "getProduct",
  "kind": "UNIT",
  "dataSource": "GetProductDataSource",
  "lambdaFunctionName": "GetProduct",
  "lambdaFunctionLogicalId": "GetProductFunction",
  "memorySize": 1024,
  "timeout": 30,
  "usesLambdaContext": false
}
```

| Field | Description |
|-------|-------------|
| `typeName` | GraphQL root type (`Query`, `Mutation`, `Subscription`) |
| `fieldName` | GraphQL field name |
| `kind` | Resolver type: `UNIT` (direct) or `PIPELINE` (multi-step) |
| `dataSource` | AppSync data source name (auto-generated or explicit) |
| `lambdaFunctionName` | C# method name (used for `ANNOTATIONS_HANDLER` env var) |
| `lambdaFunctionLogicalId` | CDK logical ID for the Lambda function resource |
| `memorySize` | Lambda memory in MB (from `[LambdaFunction]` attribute) |
| `timeout` | Lambda timeout in seconds (from `[LambdaFunction]` attribute) |
| `usesLambdaContext` | Whether Lambda uses `ILambdaContext` parameter |

## Lambda Annotations Architecture

With Lambda Annotations, **each `[LambdaFunction]` method becomes a separate Lambda function**:

```csharp
[LambdaFunction(MemorySize = 1024, Timeout = 30)]
[GraphQLQuery("getProduct")]
[GraphQLResolver]  // DataSource auto-generated as "GetProductDataSource"
public Task<Product> GetProduct(string id) { }

[LambdaFunction]
[GraphQLQuery("getUser")]
[GraphQLResolver(DataSource = "UserLambda")]
public Task<User> GetUser(Guid id) { }
```

This generates:
- **One Lambda function** with configuration from `[LambdaFunction]` attribute
- **One AppSync data source** (auto-generated name: `GetProductDataSource`)
- **One resolver** with appropriate payload handling based on method signature

## Lambda Annotations Configuration

Configuration from `[LambdaFunction]` attribute is automatically extracted and used by CDK:

```csharp
[LambdaFunction(
    MemorySize = 1024,      // Lambda memory in MB
    Timeout = 30,           // Lambda timeout in seconds
    ResourceName = "MyFunc", // Optional CloudFormation resource name
    Role = "arn:...",       // Optional IAM role ARN
    Policies = new[] { "AmazonDynamoDBFullAccess" } // Optional IAM policies
)]
[GraphQLQuery("getProduct")]
[GraphQLResolver]
public Task<Product> GetProduct(string id) { }
```

The CDK will create the Lambda with these exact settings, falling back to defaults (512 MB, 30s) if not specified.

## Data Source Naming

### Auto-Generated (Recommended)
```csharp
[GraphQLResolver] // Auto-generates: "{MethodName}DataSource"
```
Each Lambda function gets a unique data source name automatically.

### Explicit Override
```csharp
[GraphQLResolver(DataSource = "ProductsLambda")]
```
Use when you want a specific name for organizational purposes.

**Important**: The system validates that each data source name maps to exactly one Lambda function. Duplicate names pointing to different functions will cause a compile-time error.

## AppSync Resolver Payload Handling

The CDK generates different AppSync resolver code based on your Lambda signature:

### Simple Parameters (Default)
```csharp
public Task<Product> GetProduct(string id) { }
```
AppSync sends: `"1234"` (single value) or `{ "id": "1234", "name": "..." }` (multiple params)

### With Lambda Context
```csharp
public Task<Product> GetProduct(string id, ILambdaContext context) { }
```
AppSync sends full context:
```json
{
  "field": "getProduct",
  "arguments": { "id": "1234" },
  "source": { ... },
  "identity": { ... },
  "request": { ... }
}
```

The `usesLambdaContext` flag in the manifest controls this behavior automatically.

## CDK Integration

The CDK stack uses this manifest to:

1. **Create Lambda functions** - One per resolver with extracted configuration
2. **Create data sources** - One per Lambda function with unique names
3. **Create resolvers** - Context-aware payload handling based on method signature
4. **Set environment variables** - `ANNOTATIONS_HANDLER={lambdaFunctionName}` for routing

### Lambda Annotations Routing

Each Lambda function is deployed with:
```typescript
handler: 'Oproto.Lambda.GraphQL.Examples', // Assembly name
memorySize: resolver.memorySize || 512, // From [LambdaFunction] or default
timeout: cdk.Duration.seconds(resolver.timeout || 30),
environment: {
  ANNOTATIONS_HANDLER: 'GetProduct' // Routes to specific method
}
```

Lambda Annotations uses this to route requests to the correct C# method within the assembly.

## JSON Serialization

When renaming GraphQL fields, ensure JSON serialization matches:

```csharp
[GraphQLField("displayName")]
[JsonPropertyName("displayName")]  // Required for correct serialization
public string Name { get; set; }
```

Without `[JsonPropertyName]`, the Lambda will return `{ Name: "..." }` but GraphQL expects `{ displayName: "..." }`.

## Pipeline Resolvers

For advanced scenarios, you can create pipeline resolvers:

```csharp
[GraphQLResolver(Kind = ResolverKind.Pipeline, Functions = new[] { "AuthFunction", "DataFunction" })]
```

This creates a multi-step resolver that executes functions in sequence.

## Schema Validation

The manifest includes:
- `$schema` - JSON schema URL for validation
- `version` - Manifest format version
- `generatedAt` - Build timestamp

## Example Output

```json
{
  "$schema": "https://lambda-graphql.dev/schemas/resolvers.json",
  "version": "1.0.0",
  "generatedAt": "2026-01-30T15:33:55Z",
  "resolvers": [
    {
      "typeName": "Query",
      "fieldName": "getProduct",
      "kind": "UNIT",
      "dataSource": "ProductsLambda",
      "lambdaFunctionName": "GetProduct",
      "lambdaFunctionLogicalId": "GetProductFunction",
      "runtime": "APPSYNC_JS"
    }
  ],
  "dataSources": [
    {
      "name": "ProductsLambda",
      "type": "AWS_LAMBDA",
      "serviceRoleArn": "${LambdaDataSourceRole.Arn}",
      "lambdaConfig": {
        "functionArn": "${GetProductFunction.Arn}"
      }
    }
  ],
  "functions": []
}
```

## Best Practices

1. **Use explicit data source names** for clarity in CloudWatch logs and metrics
2. **Keep resolver logic simple** - complex logic belongs in your C# Lambda functions
3. **One Lambda per GraphQL field** - aligns with Lambda Annotations architecture
4. **Use pipeline resolvers** only when you need multi-step processing (auth + data, etc.)
