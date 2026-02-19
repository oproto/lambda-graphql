# Examples

This guide provides comprehensive examples of using Oproto.Lambda.GraphQL, from basic usage to advanced features. All examples are working code that you can copy and adapt for your own projects.

## Table of Contents

- [Basic Examples](#basic-examples)
- [Intermediate Examples](#intermediate-examples)
- [Advanced Examples](#advanced-examples)
- [Real-World Scenarios](#real-world-scenarios)
- [Complete Applications](#complete-applications)

---

## Basic Examples

### Simple Product Catalog

Let's start with a basic product catalog API.

#### Define Types

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Annotations;
using Oproto.Lambda.GraphQL.Attributes;

// Basic GraphQL object type
[GraphQLType("Product", Description = "A product in our catalog")]
public class Product
{
    [GraphQLField(Description = "Unique product identifier")]
    public Guid Id { get; set; }
    
    [GraphQLField(Description = "Product name")]
    public string Name { get; set; } = string.Empty;
    
    [GraphQLField(Description = "Product price in USD")]
    public decimal Price { get; set; }
    
    [GraphQLField(Description = "Product category")]
    public ProductCategory Category { get; set; }
}

// GraphQL enum type
[GraphQLType("ProductCategory")]
public enum ProductCategory
{
    [GraphQLEnumValue(Description = "Electronic devices")]
    Electronics,
    
    [GraphQLEnumValue(Description = "Books and media")]
    Books,
    
    [GraphQLEnumValue(Description = "Clothing")]
    Clothing
}

// GraphQL input type
[GraphQLType("CreateProductInput", Kind = GraphQLTypeKind.Input)]
public class CreateProductInput
{
    [GraphQLField(Description = "Product name")]
    public string Name { get; set; } = string.Empty;
    
    [GraphQLField(Description = "Product price")]
    public decimal Price { get; set; }
    
    [GraphQLField(Description = "Product category")]
    public ProductCategory Category { get; set; }
}
```

#### Lambda Functions

```csharp
public class ProductFunctions
{
    [LambdaFunction]
    [GraphQLQuery("getProduct", Description = "Get a product by ID")]
    [GraphQLResolver(DataSource = "ProductsLambda")]
    public async Task<Product> GetProduct(
        [GraphQLArgument(Description = "Product ID")] Guid id)
    {
        // Your implementation here
        return new Product 
        { 
            Id = id, 
            Name = "Sample Product", 
            Price = 29.99m,
            Category = ProductCategory.Electronics
        };
    }

    [LambdaFunction]
    [GraphQLMutation("createProduct", Description = "Create a new product")]
    [GraphQLResolver(DataSource = "ProductsLambda")]
    public async Task<Product> CreateProduct(
        [GraphQLArgument] CreateProductInput input)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = input.Name,
            Price = input.Price,
            Category = input.Category
        };
    }
}
```

#### Generated Schema

```graphql
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
  Product price in USD
  """
  price: Float!
  """
  Product category
  """
  category: ProductCategory!
}

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
  Clothing
  """
  CLOTHING
}

input CreateProductInput {
  """
  Product name
  """
  name: String!
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
  Get a product by ID
  """
  getProduct(id: ID!): Product!
}

type Mutation {
  """
  Create a new product
  """
  createProduct(input: CreateProductInput!): Product!
}
```

---

## Intermediate Examples

### User Management with AWS Scalars

This example demonstrates AWS scalar types and authentication.

```csharp
// User type with AWS scalars
[GraphQLType("User", Description = "A user in the system")]
[GraphQLAuthDirective(AuthMode.UserPools)]
public class User
{
    [GraphQLField(Description = "Unique user identifier")]
    public Guid Id { get; set; }
    
    [GraphQLField(Description = "User's email address")]
    public System.Net.Mail.MailAddress Email { get; set; } = null!; // → AWSEmail!
    
    [GraphQLField(Description = "User creation timestamp")]
    public DateTime CreatedAt { get; set; } // → AWSDateTime!
    
    [GraphQLField(Description = "User's birth date")]
    public DateOnly? BirthDate { get; set; } // → AWSDate
    
    [GraphQLField(Description = "Preferred notification time")]
    public TimeOnly? NotificationTime { get; set; } // → AWSTime
    
    [GraphQLField(Description = "User metadata as JSON")]
    public System.Text.Json.JsonElement? Metadata { get; set; } // → AWSJSON
    
    [GraphQLField(Description = "Account creation timestamp (Unix seconds)")]
    [GraphQLTimestamp]
    public long CreatedAtTimestamp { get; set; } // → AWSTimestamp!
}

// Input type for user creation
[GraphQLType("CreateUserInput", Kind = GraphQLTypeKind.Input)]
public class CreateUserInput
{
    [GraphQLField(Description = "User's email address")]
    public string Email { get; set; } = string.Empty;
    
    [GraphQLField(Description = "User's birth date")]
    public DateOnly? BirthDate { get; set; }
    
    [GraphQLField(Description = "User metadata")]
    public System.Text.Json.JsonElement? Metadata { get; set; }
}

// User functions with authentication
public class UserFunctions
{
    [LambdaFunction]
    [GraphQLQuery("getUser", Description = "Get a user by ID")]
    [GraphQLResolver(DataSource = "UserLambda")]
    [GraphQLAuthDirective(AuthMode.UserPools)]
    public async Task<User> GetUser(
        [GraphQLArgument(Description = "User ID")] Guid id)
    {
        return new User
        {
            Id = id,
            Email = new System.Net.Mail.MailAddress("user@example.com"),
            CreatedAt = DateTime.UtcNow,
            CreatedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    [LambdaFunction]
    [GraphQLMutation("createUser", Description = "Create a new user")]
    [GraphQLResolver(DataSource = "UserLambda")]
    [GraphQLAuthDirective(AuthMode.UserPools, CognitoGroups = "admin")]
    public async Task<User> CreateUser(
        [GraphQLArgument] CreateUserInput input)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = new System.Net.Mail.MailAddress(input.Email),
            CreatedAt = DateTime.UtcNow,
            BirthDate = input.BirthDate,
            Metadata = input.Metadata,
            CreatedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }
}
```

### Interface Implementation

```csharp
// Define a GraphQL interface
[GraphQLType("Node", Kind = GraphQLTypeKind.Interface)]
public interface INode
{
    [GraphQLField(Description = "Unique identifier")]
    Guid Id { get; }
    
    [GraphQLField(Description = "Creation timestamp")]
    DateTime CreatedAt { get; }
}

// Implement the interface in multiple types
[GraphQLType("Product")]
public class Product : INode
{
    [GraphQLField(Description = "Unique product identifier")]
    public Guid Id { get; set; }
    
    [GraphQLField(Description = "Product creation timestamp")]
    public DateTime CreatedAt { get; set; }
    
    [GraphQLField(Description = "Product name")]
    public string Name { get; set; } = string.Empty;
    
    [GraphQLField(Description = "Product price")]
    public decimal Price { get; set; }
}

[GraphQLType("Order")]
public class Order : INode
{
    [GraphQLField(Description = "Unique order identifier")]
    public Guid Id { get; set; }
    
    [GraphQLField(Description = "Order creation timestamp")]
    public DateTime CreatedAt { get; set; }
    
    [GraphQLField(Description = "Order total amount")]
    public decimal Total { get; set; }
    
    [GraphQLField(Description = "Order status")]
    public OrderStatus Status { get; set; }
}

[GraphQLType("OrderStatus")]
public enum OrderStatus
{
    [GraphQLEnumValue(Description = "Order is pending")]
    Pending,
    
    [GraphQLEnumValue(Description = "Order is being processed")]
    Processing,
    
    [GraphQLEnumValue(Description = "Order has been shipped")]
    Shipped,
    
    [GraphQLEnumValue(Description = "Order has been delivered")]
    Delivered
}
```

---

## Advanced Examples

### Union Types for Polymorphic Results

```csharp
// Define a union type for search results
[GraphQLUnion("SearchResult", "Product", "User", "Order")]
public class SearchResult
{
    // This class serves as a marker for the union type
    // Actual resolution happens in Lambda functions
}

// Search function returning mixed types
[LambdaFunction]
[GraphQLQuery("search", Description = "Search for products, users, or orders")]
[GraphQLResolver(DataSource = "SearchLambda")]
public async Task<List<object>> Search(
    [GraphQLArgument(Description = "Search term")] string term,
    [GraphQLArgument(Description = "Maximum results to return")] int limit)
{
    var results = new List<object>();
    
    // Add different types to the result
    // AppSync will handle union type resolution
    results.AddRange(await SearchProducts(term, limit / 3));
    results.AddRange(await SearchUsers(term, limit / 3));
    results.AddRange(await SearchOrders(term, limit / 3));
    
    return results;
}

private async Task<List<Product>> SearchProducts(string term, int limit)
{
    // Implementation for product search
    return new List<Product>();
}

private async Task<List<User>> SearchUsers(string term, int limit)
{
    // Implementation for user search
    return new List<User>();
}

private async Task<List<Order>> SearchOrders(string term, int limit)
{
    // Implementation for order search
    return new List<Order>();
}
```

### Subscriptions for Real-Time Updates

```csharp
// Subscription for order status changes
[LambdaFunction]
[GraphQLSubscription("orderStatusChanged", Description = "Subscribe to order status changes")]
[GraphQLResolver(DataSource = "OrderSubscriptionLambda")]
[GraphQLAuthDirective(AuthMode.UserPools)]
public async Task<Order> OrderStatusChanged(
    [GraphQLArgument(Description = "Order ID to watch")] Guid orderId)
{
    // Subscription implementation
    // This would typically connect to EventBridge, SNS, or DynamoDB Streams
    return new Order 
    { 
        Id = orderId, 
        CreatedAt = DateTime.UtcNow, 
        Total = 0, 
        Status = OrderStatus.Pending 
    };
}

// Subscription for product updates by category
[LambdaFunction]
[GraphQLSubscription("productUpdated", Description = "Subscribe to product updates")]
[GraphQLResolver(DataSource = "ProductSubscriptionLambda")]
public async Task<Product> ProductUpdated(
    [GraphQLArgument(Description = "Product category to watch")] ProductCategory category)
{
    // Filtered subscription implementation
    return new Product
    {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        Name = "Updated Product",
        Price = 99.99m,
        Category = category
    };
}
```

### Custom Directives

```csharp
// Define custom directives at assembly level
[assembly: GraphQLDirective("auth",
    Description = "Requires authentication with specific permissions",
    Locations = DirectiveLocation.FieldDefinition | DirectiveLocation.Object,
    Arguments = "requires: String!")]

[assembly: GraphQLDirective("rateLimit",
    Description = "Applies rate limiting to field access",
    Locations = DirectiveLocation.FieldDefinition,
    Arguments = "max: Int!, window: Int!")]

// Apply custom directives
[GraphQLType("AdminPanel")]
[GraphQLApplyDirective("auth", Arguments = "requires: \"ADMIN\"")]
public class AdminPanel
{
    [GraphQLField(Description = "System metrics")]
    [GraphQLApplyDirective("rateLimit", Arguments = "max: 10, window: 60")]
    public SystemMetrics Metrics { get; set; } = new();
    
    [GraphQLField(Description = "User management")]
    [GraphQLApplyDirective("auth", Arguments = "requires: \"USER_ADMIN\"")]
    public UserManagement Users { get; set; } = new();
}

[GraphQLType("SystemMetrics")]
public class SystemMetrics
{
    [GraphQLField] public int ActiveUsers { get; set; }
    [GraphQLField] public int DatabaseConnections { get; set; }
    [GraphQLField] public double CpuUsage { get; set; }
}

[GraphQLType("UserManagement")]
public class UserManagement
{
    [GraphQLField] public int TotalUsers { get; set; }
    [GraphQLField] public int ActiveSessions { get; set; }
}
```

---

## Real-World Scenarios

### E-Commerce API

Complete e-commerce API with products, orders, and users.

```csharp
// Product management
[GraphQLType("Product")]
public class Product
{
    [GraphQLField] public Guid Id { get; set; }
    [GraphQLField] public string Name { get; set; } = string.Empty;
    [GraphQLField] public string Description { get; set; } = string.Empty;
    [GraphQLField] public decimal Price { get; set; }
    [GraphQLField] public ProductCategory Category { get; set; }
    [GraphQLField] public List<string> Tags { get; set; } = new();
    [GraphQLField] public DateTime CreatedAt { get; set; }
    [GraphQLField] public DateTime UpdatedAt { get; set; }
    [GraphQLField] public bool IsActive { get; set; }
    
    [GraphQLField(Description = "Product inventory count")]
    [GraphQLAuthDirective(AuthMode.UserPools, CognitoGroups = "admin,inventory")]
    public int StockCount { get; set; }
}

// Order management
[GraphQLType("Order")]
public class Order
{
    [GraphQLField] public Guid Id { get; set; }
    [GraphQLField] public Guid UserId { get; set; }
    [GraphQLField] public List<OrderItem> Items { get; set; } = new();
    [GraphQLField] public decimal Total { get; set; }
    [GraphQLField] public OrderStatus Status { get; set; }
    [GraphQLField] public DateTime CreatedAt { get; set; }
    [GraphQLField] public DateTime? ShippedAt { get; set; }
    [GraphQLField] public DateTime? DeliveredAt { get; set; }
    [GraphQLField] public ShippingAddress ShippingAddress { get; set; } = new();
}

[GraphQLType("OrderItem")]
public class OrderItem
{
    [GraphQLField] public Guid ProductId { get; set; }
    [GraphQLField] public string ProductName { get; set; } = string.Empty;
    [GraphQLField] public int Quantity { get; set; }
    [GraphQLField] public decimal UnitPrice { get; set; }
    [GraphQLField] public decimal TotalPrice { get; set; }
}

[GraphQLType("ShippingAddress")]
public class ShippingAddress
{
    [GraphQLField] public string Street { get; set; } = string.Empty;
    [GraphQLField] public string City { get; set; } = string.Empty;
    [GraphQLField] public string State { get; set; } = string.Empty;
    [GraphQLField] public string ZipCode { get; set; } = string.Empty;
    [GraphQLField] public string Country { get; set; } = string.Empty;
}

// Input types
[GraphQLType("CreateOrderInput", Kind = GraphQLTypeKind.Input)]
public class CreateOrderInput
{
    [GraphQLField] public List<OrderItemInput> Items { get; set; } = new();
    [GraphQLField] public ShippingAddressInput ShippingAddress { get; set; } = new();
}

[GraphQLType("OrderItemInput", Kind = GraphQLTypeKind.Input)]
public class OrderItemInput
{
    [GraphQLField] public Guid ProductId { get; set; }
    [GraphQLField] public int Quantity { get; set; }
}

[GraphQLType("ShippingAddressInput", Kind = GraphQLTypeKind.Input)]
public class ShippingAddressInput
{
    [GraphQLField] public string Street { get; set; } = string.Empty;
    [GraphQLField] public string City { get; set; } = string.Empty;
    [GraphQLField] public string State { get; set; } = string.Empty;
    [GraphQLField] public string ZipCode { get; set; } = string.Empty;
    [GraphQLField] public string Country { get; set; } = string.Empty;
}

// Lambda functions
public class ECommerceFunctions
{
    [LambdaFunction]
    [GraphQLQuery("getProduct")]
    [GraphQLResolver(DataSource = "ProductsLambda")]
    public async Task<Product?> GetProduct(Guid id) { /* Implementation */ }

    [LambdaFunction]
    [GraphQLQuery("listProducts")]
    [GraphQLResolver(DataSource = "ProductsLambda")]
    public async Task<List<Product>> ListProducts(
        ProductCategory? category = null,
        int limit = 50,
        int offset = 0) { /* Implementation */ }

    [LambdaFunction]
    [GraphQLMutation("createOrder")]
    [GraphQLResolver(DataSource = "OrdersLambda")]
    [GraphQLAuthDirective(AuthMode.UserPools)]
    public async Task<Order> CreateOrder(CreateOrderInput input) { /* Implementation */ }

    [LambdaFunction]
    [GraphQLQuery("getOrder")]
    [GraphQLResolver(DataSource = "OrdersLambda")]
    [GraphQLAuthDirective(AuthMode.UserPools)]
    public async Task<Order?> GetOrder(Guid id) { /* Implementation */ }

    [LambdaFunction]
    [GraphQLSubscription("orderStatusChanged")]
    [GraphQLResolver(DataSource = "OrderSubscriptionLambda")]
    [GraphQLAuthDirective(AuthMode.UserPools)]
    public async Task<Order> OrderStatusChanged(Guid orderId) { /* Implementation */ }
}
```

### Content Management System

```csharp
// Content types with rich metadata
[GraphQLType("Article")]
public class Article
{
    [GraphQLField] public Guid Id { get; set; }
    [GraphQLField] public string Title { get; set; } = string.Empty;
    [GraphQLField] public string Content { get; set; } = string.Empty;
    [GraphQLField] public string Excerpt { get; set; } = string.Empty;
    [GraphQLField] public Guid AuthorId { get; set; }
    [GraphQLField] public List<string> Tags { get; set; } = new();
    [GraphQLField] public ArticleStatus Status { get; set; }
    [GraphQLField] public DateTime CreatedAt { get; set; }
    [GraphQLField] public DateTime UpdatedAt { get; set; }
    [GraphQLField] public DateTime? PublishedAt { get; set; }
    
    [GraphQLField(Description = "SEO metadata")]
    public SeoMetadata Seo { get; set; } = new();
    
    [GraphQLField(Description = "Article analytics")]
    [GraphQLAuthDirective(AuthMode.UserPools, CognitoGroups = "editor,admin")]
    public ArticleAnalytics Analytics { get; set; } = new();
}

[GraphQLType("ArticleStatus")]
public enum ArticleStatus
{
    [GraphQLEnumValue] Draft,
    [GraphQLEnumValue] Review,
    [GraphQLEnumValue] Published,
    [GraphQLEnumValue] Archived
}

[GraphQLType("SeoMetadata")]
public class SeoMetadata
{
    [GraphQLField] public string MetaTitle { get; set; } = string.Empty;
    [GraphQLField] public string MetaDescription { get; set; } = string.Empty;
    [GraphQLField] public List<string> Keywords { get; set; } = new();
    [GraphQLField] public string CanonicalUrl { get; set; } = string.Empty;
}

[GraphQLType("ArticleAnalytics")]
public class ArticleAnalytics
{
    [GraphQLField] public int Views { get; set; }
    [GraphQLField] public int Shares { get; set; }
    [GraphQLField] public int Comments { get; set; }
    [GraphQLField] public double AvgReadTime { get; set; }
}

// Author management
[GraphQLType("Author")]
public class Author
{
    [GraphQLField] public Guid Id { get; set; }
    [GraphQLField] public string Name { get; set; } = string.Empty;
    [GraphQLField] public string Bio { get; set; } = string.Empty;
    [GraphQLField] public System.Net.Mail.MailAddress Email { get; set; } = null!;
    [GraphQLField] public Uri? Website { get; set; }
    [GraphQLField] public DateTime JoinedAt { get; set; }
    
    [GraphQLField(Description = "Author's published articles")]
    public List<Article> Articles { get; set; } = new();
}
```

---

## Complete Applications

### Blog Platform

A complete blog platform with authentication, content management, and real-time features.

```csharp
// Assembly-level configuration
[assembly: GraphQLSchema("BlogPlatform",
    Description = "Complete blog platform with real-time features",
    Version = "1.0.0")]

// Union type for search results
[GraphQLUnion("SearchResult", "Article", "Author", "Tag")]
public class SearchResult { }

// Main content types
[GraphQLType("Article")]
public class Article
{
    [GraphQLField] public Guid Id { get; set; }
    [GraphQLField] public string Title { get; set; } = string.Empty;
    [GraphQLField] public string Content { get; set; } = string.Empty;
    [GraphQLField] public string Slug { get; set; } = string.Empty;
    [GraphQLField] public Guid AuthorId { get; set; }
    [GraphQLField] public Author Author { get; set; } = new();
    [GraphQLField] public List<Tag> Tags { get; set; } = new();
    [GraphQLField] public ArticleStatus Status { get; set; }
    [GraphQLField] public DateTime CreatedAt { get; set; }
    [GraphQLField] public DateTime UpdatedAt { get; set; }
    [GraphQLField] public DateTime? PublishedAt { get; set; }
    [GraphQLField] public int ViewCount { get; set; }
    [GraphQLField] public List<Comment> Comments { get; set; } = new();
}

[GraphQLType("Comment")]
public class Comment
{
    [GraphQLField] public Guid Id { get; set; }
    [GraphQLField] public Guid ArticleId { get; set; }
    [GraphQLField] public Guid AuthorId { get; set; }
    [GraphQLField] public Author Author { get; set; } = new();
    [GraphQLField] public string Content { get; set; } = string.Empty;
    [GraphQLField] public DateTime CreatedAt { get; set; }
    [GraphQLField] public bool IsApproved { get; set; }
}

[GraphQLType("Tag")]
public class Tag
{
    [GraphQLField] public Guid Id { get; set; }
    [GraphQLField] public string Name { get; set; } = string.Empty;
    [GraphQLField] public string Slug { get; set; } = string.Empty;
    [GraphQLField] public string Description { get; set; } = string.Empty;
    [GraphQLField] public int ArticleCount { get; set; }
}

// Complete function set
public class BlogFunctions
{
    // Article operations
    [LambdaFunction]
    [GraphQLQuery("getArticle")]
    [GraphQLResolver(DataSource = "BlogLambda")]
    public async Task<Article?> GetArticle(string slug) { /* Implementation */ }

    [LambdaFunction]
    [GraphQLQuery("listArticles")]
    [GraphQLResolver(DataSource = "BlogLambda")]
    public async Task<List<Article>> ListArticles(
        ArticleStatus? status = null,
        Guid? authorId = null,
        List<string>? tags = null,
        int limit = 20,
        int offset = 0) { /* Implementation */ }

    [LambdaFunction]
    [GraphQLMutation("createArticle")]
    [GraphQLResolver(DataSource = "BlogLambda")]
    [GraphQLAuthDirective(AuthMode.UserPools, CognitoGroups = "author,editor,admin")]
    public async Task<Article> CreateArticle(CreateArticleInput input) { /* Implementation */ }

    [LambdaFunction]
    [GraphQLMutation("publishArticle")]
    [GraphQLResolver(DataSource = "BlogLambda")]
    [GraphQLAuthDirective(AuthMode.UserPools, CognitoGroups = "editor,admin")]
    public async Task<Article> PublishArticle(Guid id) { /* Implementation */ }

    // Comment operations
    [LambdaFunction]
    [GraphQLMutation("addComment")]
    [GraphQLResolver(DataSource = "BlogLambda")]
    [GraphQLAuthDirective(AuthMode.UserPools)]
    public async Task<Comment> AddComment(AddCommentInput input) { /* Implementation */ }

    [LambdaFunction]
    [GraphQLMutation("approveComment")]
    [GraphQLResolver(DataSource = "BlogLambda")]
    [GraphQLAuthDirective(AuthMode.UserPools, CognitoGroups = "moderator,admin")]
    public async Task<Comment> ApproveComment(Guid id) { /* Implementation */ }

    // Real-time subscriptions
    [LambdaFunction]
    [GraphQLSubscription("articlePublished")]
    [GraphQLResolver(DataSource = "BlogSubscriptionLambda")]
    public async Task<Article> ArticlePublished() { /* Implementation */ }

    [LambdaFunction]
    [GraphQLSubscription("commentAdded")]
    [GraphQLResolver(DataSource = "BlogSubscriptionLambda")]
    public async Task<Comment> CommentAdded(Guid articleId) { /* Implementation */ }

    // Search functionality
    [LambdaFunction]
    [GraphQLQuery("search")]
    [GraphQLResolver(DataSource = "SearchLambda")]
    public async Task<List<object>> Search(
        string query,
        int limit = 20) { /* Implementation */ }
}
```

---

## Testing Your Examples

### Build and Generate

```bash
# Build your project to generate schema
dotnet build

# Check generated files
ls -la bin/Debug/net6.0/
cat bin/Debug/net6.0/schema.graphql
cat bin/Debug/net6.0/resolvers.json
```

### Validate Schema

```bash
# If you have GraphQL CLI installed
graphql-schema-linter schema.graphql

# Or use online validators
# Copy schema.graphql content to https://graphql-schema-linter.com/
```

### Test with AppSync

Deploy the generated schema and resolvers to AWS AppSync to test the complete functionality.

---

## Next Steps

- **[Advanced Features](advanced-features.md)** - Learn about union types, directives, and subscriptions
- **[AWS Integration](aws-integration.md)** - Deploy your GraphQL API to AWS AppSync
- **[Performance](performance.md)** - Optimize your GraphQL API for production
- **[Troubleshooting](troubleshooting.md)** - Common issues and solutions

All examples in this guide are based on working code from the Oproto.Lambda.GraphQL.Examples project. You can find the complete source code in the repository for reference.
