# Getting Started with Oproto.Lambda.GraphQL

This guide will help you get up and running with Oproto.Lambda.GraphQL in just a few minutes. You'll learn how to install the library, create your first GraphQL types, and generate a complete GraphQL schema for AWS AppSync.

## Prerequisites

Before you begin, make sure you have:

- **.NET 6.0 or later** installed
- **Visual Studio 2022** or **VS Code** with C# extension
- **AWS Lambda Annotations** package (will be installed automatically)
- Basic knowledge of **C#** and **GraphQL** concepts

## Installation

### Install via NuGet

```bash
dotnet add package Oproto.Lambda.GraphQL
dotnet add package Amazon.Lambda.Annotations
```

This will automatically install:
- `Oproto.Lambda.GraphQL` - Main package with attributes
- `Oproto.Lambda.GraphQL.SourceGenerator` - Roslyn source generator
- `Oproto.Lambda.GraphQL.Build` - MSBuild tasks for schema extraction

## What Gets Generated

When you build your project, Oproto.Lambda.GraphQL generates several files:

### 1. GraphQL Schema (`schema.graphql`)
The complete GraphQL SDL schema based on your C# types and Lambda functions.

### 2. Resolver Manifest (`resolvers.json`) 
AppSync resolver configuration for CDK deployment.

### 3. Generated C# Source Files
Source generator creates intermediate C# files (visible in `obj/GeneratedFiles/`) that embed the schema metadata in your assembly.

You can inspect these files to understand how the generation works:

```bash
# View generated C# files
find YourProject/obj -name "*.cs" -path "*GraphQLSchemaGenerator*"
```

### Verify Installation

Build your project to ensure everything is installed correctly:

```bash
dotnet build
```

You should see output indicating the source generator is running.

## Your First GraphQL Schema

Let's create a simple product catalog API to demonstrate the core concepts.

### Step 1: Define Your GraphQL Types

Create a new file `Types.cs`:

```csharp
using System;
using System.Threading.Tasks;
using Amazon.Lambda.Annotations;
using Oproto.Lambda.GraphQL.Attributes;

namespace MyGraphQLApi;

// Define a GraphQL object type
[GraphQLType("Product", Description = "A product in our catalog")]
public class Product
{
    [GraphQLField(Description = "Unique product identifier")]
    public Guid Id { get; set; }
    
    [GraphQLField(Description = "Product name")]
    public string Name { get; set; } = string.Empty;
    
    [GraphQLField(Description = "Product description")]
    public string? Description { get; set; }
    
    [GraphQLField(Description = "Product price in USD")]
    public decimal Price { get; set; }
    
    [GraphQLField(Description = "Product category")]
    public ProductCategory Category { get; set; }
    
    [GraphQLField(Description = "When the product was created")]
    public DateTime CreatedAt { get; set; }
}

// Define a GraphQL enum type
[GraphQLType("ProductCategory", Description = "Product categories")]
public enum ProductCategory
{
    [GraphQLEnumValue(Description = "Electronic devices")]
    Electronics,
    
    [GraphQLEnumValue(Description = "Books and media")]
    Books,
    
    [GraphQLEnumValue(Description = "Clothing and accessories")]
    Clothing,
    
    [GraphQLEnumValue(Description = "Home and garden")]
    Home
}

// Define a GraphQL input type
[GraphQLType("CreateProductInput", Kind = GraphQLTypeKind.Input)]
public class CreateProductInput
{
    [GraphQLField(Description = "Product name")]
    public string Name { get; set; } = string.Empty;
    
    [GraphQLField(Description = "Product description")]
    public string? Description { get; set; }
    
    [GraphQLField(Description = "Product price")]
    public decimal Price { get; set; }
    
    [GraphQLField(Description = "Product category")]
    public ProductCategory Category { get; set; }
}
```

### Step 2: Create Lambda Functions with GraphQL Operations

Create a new file `Functions.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Annotations;
using Oproto.Lambda.GraphQL.Attributes;

namespace MyGraphQLApi;

public class ProductFunctions
{
    // Sample data for demonstration
    private static readonly List<Product> Products = new()
    {
        new Product
        {
            Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            Name = "Laptop",
            Description = "High-performance laptop",
            Price = 999.99m,
            Category = ProductCategory.Electronics,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        },
        new Product
        {
            Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
            Name = "Programming Book",
            Description = "Learn advanced programming techniques",
            Price = 49.99m,
            Category = ProductCategory.Books,
            CreatedAt = DateTime.UtcNow.AddDays(-15)
        }
    };

    [LambdaFunction]
    [GraphQLQuery("getProduct", Description = "Get a product by its ID")]
    [GraphQLResolver(DataSource = "ProductsLambda")]
    public async Task<Product?> GetProduct(
        [GraphQLArgument(Description = "Product ID")] Guid id)
    {
        await Task.Delay(1); // Simulate async work
        return Products.Find(p => p.Id == id);
    }

    [LambdaFunction]
    [GraphQLQuery("listProducts", Description = "Get all products")]
    [GraphQLResolver(DataSource = "ProductsLambda")]
    public async Task<List<Product>> ListProducts()
    {
        await Task.Delay(1); // Simulate async work
        return Products;
    }

    [LambdaFunction]
    [GraphQLMutation("createProduct", Description = "Create a new product")]
    [GraphQLResolver(DataSource = "ProductsLambda")]
    public async Task<Product> CreateProduct(
        [GraphQLArgument(Description = "Product data")] CreateProductInput input)
    {
        await Task.Delay(1); // Simulate async work
        
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = input.Name,
            Description = input.Description,
            Price = input.Price,
            Category = input.Category,
            CreatedAt = DateTime.UtcNow
        };
        
        Products.Add(product);
        return product;
    }
}
```

### Step 3: Build and Generate Schema

Build your project to trigger the source generator:

```bash
dotnet build
```

After building, you'll find two generated files in your output directory:

1. **`schema.graphql`** - The complete GraphQL schema
2. **`resolvers.json`** - Resolver configuration for AWS AppSync

### Step 4: Examine the Generated Schema

Open the generated `schema.graphql` file:

```graphql
"""
Generated GraphQL schema from Lambda functions
"""
schema {
  query: Query
  mutation: Mutation
}

"""
A product in our catalog
"""
type Product {
  """
  Unique product identifier
  """
  id: ID!
  """
  Product name
  """
  name: String!
  """
  Product description
  """
  description: String
  """
  Product price in USD
  """
  price: Float!
  """
  Product category
  """
  category: ProductCategory!
  """
  When the product was created
  """
  createdAt: AWSDateTime!
}

"""
Product categories
"""
enum ProductCategory {
  """
  Electronic devices
  """
  ELECTRONICS
  """
  Books and media
  """
  BOOKS
  """
  Clothing and accessories
  """
  CLOTHING
  """
  Home and garden
  """
  HOME
}

input CreateProductInput {
  """
  Product name
  """
  name: String!
  """
  Product description
  """
  description: String
  """
  Product price
  """
  price: Float!
  """
  Product category
  """
  category: ProductCategory!
}

type Query {
  """
  Get a product by its ID
  """
  getProduct(id: ID!): Product
  """
  Get all products
  """
  listProducts: [Product!]!
}

type Mutation {
  """
  Create a new product
  """
  createProduct(input: CreateProductInput!): Product!
}
```

## Understanding the Generated Output

### Type Mappings

Oproto.Lambda.GraphQL automatically maps C# types to GraphQL types:

| C# Type | GraphQL Type |
|---------|--------------|
| `string` | `String` |
| `int`, `long` | `Int` |
| `float`, `double`, `decimal` | `Float` |
| `bool` | `Boolean` |
| `Guid` | `ID` |
| `DateTime` | `AWSDateTime` |
| `DateOnly` | `AWSDate` |
| `TimeOnly` | `AWSTime` |
| `T?` (nullable) | `T` (nullable in GraphQL) |
| `List<T>`, `T[]` | `[T]` |
| Custom classes | Custom Object/Input types |
| Enums | GraphQL Enums |

### Nullability

GraphQL nullability is the inverse of C#:
- C# `string` (nullable reference type) → GraphQL `String` (nullable)
- C# `string?` (explicit nullable) → GraphQL `String` (nullable)
- C# `int` (non-nullable value type) → GraphQL `Int!` (non-null)
- C# `int?` (nullable value type) → GraphQL `Int` (nullable)

## Next Steps

Congratulations! You've successfully created your first GraphQL schema with Oproto.Lambda.GraphQL. Here's what you can explore next:

### 1. Advanced Features
- **[Union Types](advanced-features.md#union-types)** - Return different types from a single field
- **[Interfaces](advanced-features.md#interfaces)** - Define common fields across types
- **[Directives](advanced-features.md#directives)** - Add metadata and behavior to your schema
- **[Subscriptions](advanced-features.md#subscriptions)** - Real-time updates

### 2. AWS Integration
- **[AppSync Deployment](aws-integration.md#appsync-deployment)** - Deploy your schema to AWS AppSync
- **[CDK Integration](aws-integration.md#cdk-integration)** - Use AWS CDK for infrastructure as code
- **[Authentication](aws-integration.md#authentication)** - Secure your GraphQL API

### 3. Best Practices
- **[Error Handling](troubleshooting.md#error-handling)** - Handle errors gracefully
- **[Performance](performance.md)** - Optimize your GraphQL API
- **[Testing](contributing.md#testing)** - Test your GraphQL operations

## Common Issues

### Build Errors

If you encounter build errors, try:

```bash
# Clean and rebuild
dotnet clean
dotnet build

# Check for package conflicts
dotnet list package --outdated
```

### Schema Not Generated

If the schema files aren't generated:

1. Ensure you have `[GraphQLType]` attributes on your classes
2. Ensure you have `[GraphQLQuery]` or `[GraphQLMutation]` attributes on your methods
3. Check that your methods also have `[LambdaFunction]` attributes
4. Verify the build output for any source generator errors

### Need Help?

- Check the [Troubleshooting Guide](troubleshooting.md)
- Review the [Examples](examples.md) for more code samples
- Open an issue on [GitHub](https://github.com/oproto/lambda-graphql/issues)

---

**Next**: Learn about [Advanced Features](advanced-features.md) like union types and directives, or explore [AWS Integration](aws-integration.md) for deployment guidance.
