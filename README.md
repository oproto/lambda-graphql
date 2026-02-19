<p align="center">
  <img src="docs/assets/LambdaGraphQLLogo.svg" alt="Oproto.Lambda.GraphQL Logo" width="300">
</p>

# Oproto.Lambda.GraphQL

[![Build](https://github.com/oproto/lambda-graphql/actions/workflows/build.yml/badge.svg)](https://github.com/oproto/lambda-graphql/actions/workflows/build.yml)
[![Tests](https://github.com/oproto/lambda-graphql/actions/workflows/test.yml/badge.svg)](https://github.com/oproto/lambda-graphql/actions/workflows/test.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Sponsor](https://img.shields.io/badge/Sponsor-❤-ea4aaa)](https://github.com/sponsors/dguisinger)

[![NuGet](https://img.shields.io/nuget/v/Oproto.Lambda.GraphQL.svg?label=Lambda.GraphQL)](https://www.nuget.org/packages/Oproto.Lambda.GraphQL/)
[![NuGet](https://img.shields.io/nuget/v/Oproto.Lambda.GraphQL.Build.svg?label=Build)](https://www.nuget.org/packages/Oproto.Lambda.GraphQL.Build/)
[![NuGet](https://img.shields.io/nuget/v/Oproto.Lambda.GraphQL.SourceGenerator.svg?label=SourceGenerator)](https://www.nuget.org/packages/Oproto.Lambda.GraphQL.SourceGenerator/)

A .NET library that generates GraphQL schemas from C# Lambda functions for AWS AppSync. Provides compile-time schema generation through Roslyn source generators and MSBuild tasks, enabling type-safe GraphQL API development with AWS Lambda Annotations. Zero runtime dependencies, full Native AOT compatibility.

## Quick Start

### Installation

```bash
dotnet add package Oproto.Lambda.GraphQL
dotnet add package Amazon.Lambda.Annotations
```

> **Note:** The source generator and build tasks are bundled in the main package. No additional packages are required for basic usage.

### Define Your First GraphQL Schema

```csharp
using Oproto.Lambda.GraphQL.Attributes;
using Amazon.Lambda.Annotations;

// 1. Define your GraphQL types
[GraphQLType("Product", Description = "A product in the catalog")]
public class Product
{
    [GraphQLField(Description = "Unique product identifier")]
    public Guid Id { get; set; }

    [GraphQLField(Description = "Product name")]
    public string Name { get; set; }

    [GraphQLField(Description = "Product price in USD")]
    public decimal Price { get; set; }
}

// 2. Create input types
[GraphQLType("CreateProductInput", Kind = GraphQLTypeKind.Input)]
public class CreateProductInput
{
    [GraphQLField(Description = "Product name")]
    public string Name { get; set; }

    [GraphQLField(Description = "Product price")]
    public decimal Price { get; set; }
}

// 3. Implement Lambda functions with GraphQL operations
public class ProductFunctions
{
    [LambdaFunction(MemorySize = 1024, Timeout = 30)]
    [GraphQLQuery("getProduct", Description = "Get a product by ID")]
    [GraphQLResolver]
    public async Task<Product> GetProduct(
        [GraphQLArgument(Description = "Product ID")] Guid id)
    {
        return new Product { Id = id, Name = "Sample Product", Price = 29.99m };
    }

    [LambdaFunction(MemorySize = 512, Timeout = 15)]
    [GraphQLMutation("createProduct", Description = "Create a new product")]
    [GraphQLResolver]
    public async Task<Product> CreateProduct(
        [GraphQLArgument] CreateProductInput input)
    {
        return new Product { Id = Guid.NewGuid(), Name = input.Name, Price = input.Price };
    }
}
```

Build your project and the library automatically generates:
- **`schema.graphql`** — Complete GraphQL SDL schema
- **`resolvers.json`** — Resolver configuration manifest for CDK deployment

**Next Steps:** See the [Getting Started Guide](docs/getting-started.md) for detailed setup instructions and more examples.

## Key Features

### 🔧 Compile-Time Generation
GraphQL schemas generated during build with zero runtime overhead. Uses Roslyn incremental source generators for fast, cached builds.

### 🛡️ Type Safety
Full C# type safety with GraphQL schema validation and IntelliSense. Automatic mapping of C# types to GraphQL types including AWS AppSync scalars.

### ☁️ AWS Native
Built specifically for AWS AppSync with CDK integration. Generates resolver manifests with Lambda Annotations configuration for automated deployment.

### 🚀 Advanced GraphQL
Union types, interfaces, directives, subscriptions, custom scalars, and AWS authentication directives.

### 📦 Zero Runtime Dependencies
Pure compile-time source generation. No reflection, no runtime schema building, no dynamic type inspection.

### 🔄 AOT Compatible
Works seamlessly with Native AOT compilation for faster cold starts and lower memory usage.

## Advanced Features

### Union Types
```csharp
[GraphQLUnion("SearchResult", "Product", "User", "Order")]
public class SearchResult { }

[LambdaFunction]
[GraphQLQuery("search")]
public async Task<List<object>> Search(string term)
{
    // Return mixed types - AppSync handles union resolution
    return results;
}
```

### AWS Scalar Types
```csharp
[GraphQLType("User")]
public class User
{
    [GraphQLField] public Guid Id { get; set; }           // → ID!
    [GraphQLField] public DateTime CreatedAt { get; set; } // → AWSDateTime!
    [GraphQLField] public DateOnly BirthDate { get; set; } // → AWSDate
    [GraphQLField] public System.Net.Mail.MailAddress Email { get; set; } // → AWSEmail!

    [GraphQLField]
    [GraphQLTimestamp]
    public long LastLoginTimestamp { get; set; }           // → AWSTimestamp!
}
```

### Subscriptions
```csharp
[LambdaFunction]
[GraphQLSubscription("orderStatusChanged")]
[GraphQLResolver(DataSource = "OrderSubscriptionLambda")]
public async Task<Order> OrderStatusChanged(Guid orderId)
{
    // Subscription implementation
}
```

### Auth Directives
```csharp
[GraphQLType("AdminData")]
[GraphQLAuthDirective(AuthMode.UserPools, CognitoGroups = "admin")]
public class AdminData
{
    [GraphQLField] public string SensitiveInfo { get; set; }
}
```

### Lambda Annotations Configuration
```csharp
[LambdaFunction(
    MemorySize = 2048,
    Timeout = 60,
    Policies = new[] { "AmazonDynamoDBFullAccess" }
)]
[GraphQLQuery("complexQuery")]
public async Task<Result> ComplexQuery(string param) { }
```
Configuration automatically flows to deployed Lambda functions via CDK.

## Documentation

- **[Getting Started](docs/getting-started.md)** — Installation, setup, and your first GraphQL schema
- **[API Reference](docs/api-reference.md)** — Complete API documentation for all attributes
- **[Attributes Guide](docs/attributes.md)** — Detailed guide to all GraphQL attributes
- **[Examples](docs/examples.md)** — Working code examples from basic to advanced
- **[Architecture](docs/architecture.md)** — Source generator design and internals
- **[Performance](docs/performance.md)** — Performance considerations and optimization
- **[Troubleshooting](docs/troubleshooting.md)** — Common issues and solutions
- **[Contributing](docs/contributing.md)** — Development setup and contribution guidelines

## Packages

| Package | Purpose |
|---------|---------|
| `Oproto.Lambda.GraphQL` | Main package with attributes and build integration |
| `Oproto.Lambda.GraphQL.Build` | MSBuild tasks for schema extraction |
| `Oproto.Lambda.GraphQL.SourceGenerator` | Roslyn incremental source generator |

## Requirements

- **.NET 6.0+** — For source generator support
- **C# 10+** — For nullable reference types and modern language features
- **AWS Lambda Annotations** — For Lambda function definitions

## About

**Oproto.Lambda.GraphQL** is developed and maintained by [Oproto Inc](https://oproto.com),
a company building modern SaaS solutions for small business finance and accounting.

### Related Projects

- [FluentDynamoDb](https://fluentdynamodb.dev)
- [LambdaOpenApi](https://lambdaopenapi.dev)

### Links
- 🏢 **Company**: [oproto.com](https://oproto.com)
- 👨‍💻 **Developer Portal**: [oproto.io](https://oproto.io)
- 📚 **Documentation**: [lambdagraphql.dev](https://lambdagraphql.dev)

### Maintainer
- **Dan Guisinger** — [danguisinger.com](https://danguisinger.com)

## ❤️ Support the Project

Oproto maintains this library as part of a broader open-source ecosystem for building high-quality AWS-native .NET applications. If Oproto.Lambda.GraphQL saves you time or helps your team ship features faster, please consider supporting ongoing development.

Your support helps:
- Fund continued maintenance of the Oproto open source ecosystem
- Keep libraries AOT-compatible and aligned with new AWS features
- Improve documentation, samples, and test coverage
- Sustain long-term open-source availability

**👉 [GitHub Sponsors](https://github.com/sponsors/dguisinger)** — Recurring support for those who want to help sustain long-term development.

**👉 [Buy Me a Coffee](https://buymeacoffee.com/danguisinger)** — A simple, one-time "thanks" for helping you ship faster.

## Community & Support

- **Issues:** [GitHub Issues](https://github.com/oproto/lambda-graphql/issues)
- **Discussions:** [GitHub Discussions](https://github.com/oproto/lambda-graphql/discussions)
- **License:** [MIT License](LICENSE)

## Contributing

Contributions are welcome! Please see our [Contributing Guide](docs/contributing.md) for details on setting up the development environment, running tests, and submitting pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Built with ❤️ for the .NET and AWS communities**
