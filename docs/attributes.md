# Attributes Reference

This guide provides detailed documentation for each GraphQL attribute in Oproto.Lambda.GraphQL, with practical examples and usage patterns.

## Table of Contents

- [Type Definition Attributes](#type-definition-attributes)
- [Field and Property Attributes](#field-and-property-attributes)
- [Operation Attributes](#operation-attributes)
- [Advanced Type Attributes](#advanced-type-attributes)
- [Directive Attributes](#directive-attributes)
- [Configuration Attributes](#configuration-attributes)
- [Modifier Attributes](#modifier-attributes)

---

## Type Definition Attributes

### GraphQLTypeAttribute

The primary attribute for defining GraphQL types from C# classes, interfaces, and enums.

#### Basic Usage

```csharp
// Simple object type
[GraphQLType("Product")]
public class Product
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

#### Input Types

```csharp
[GraphQLType("CreateProductInput", Kind = GraphQLTypeKind.Input)]
public class CreateProductInput
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

#### Interface Types

```csharp
[GraphQLType("Node", Kind = GraphQLTypeKind.Interface)]
public interface INode
{
    Guid Id { get; }
    DateTime CreatedAt { get; }
}

[GraphQLType("Product")]
public class Product : INode
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Name { get; set; }
}
```

#### Enum Types

```csharp
[GraphQLType("OrderStatus", Description = "Order processing status")]
public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered
}
```

### GraphQLUnionAttribute

Creates GraphQL union types for polymorphic return values.

#### Basic Union

```csharp
[GraphQLUnion("SearchResult", "Product", "User", "Order")]
public class SearchResult
{
    // Marker class for union type
}

[LambdaFunction]
[GraphQLQuery("search")]
public async Task<List<object>> Search(string term)
{
    var results = new List<object>();
    
    // Add different types to the result
    results.AddRange(await SearchProducts(term));
    results.AddRange(await SearchUsers(term));
    results.AddRange(await SearchOrders(term));
    
    return results;
}
```

#### Union with Description

```csharp
[GraphQLUnion("MediaContent", "Image", "Video", "Document")]
public class MediaContent
{
    // Union for different media types
}
```

---

## Field and Property Attributes

### GraphQLFieldAttribute

Defines GraphQL fields with metadata and customization options.

#### Basic Field Definition

```csharp
[GraphQLType("Product")]
public class Product
{
    [GraphQLField(Description = "Unique product identifier")]
    public Guid Id { get; set; }
    
    [GraphQLField(Description = "Product display name")]
    public string Name { get; set; }
    
    [GraphQLField(Description = "Product price in USD")]
    public decimal Price { get; set; }
}
```

#### Custom Field Names

```csharp
[GraphQLType("User")]
public class User
{
    [GraphQLField("id", Description = "User identifier")]
    public Guid UserId { get; set; }
    
    [GraphQLField("displayName", Description = "User's display name")]
    public string FullName { get; set; }
}
```

#### Deprecated Fields

```csharp
[GraphQLType("Product")]
public class Product
{
    [GraphQLField(Description = "Product name")]
    public string Name { get; set; }
    
    [GraphQLField(Deprecated = true, DeprecationReason = "Use Name instead")]
    public string Title { get; set; }
    
    [GraphQLField("displayPrice", Description = "Formatted price with currency")]
    public string FormattedPrice { get; set; }
    
    [GraphQLField(Deprecated = true, DeprecationReason = "Use displayPrice instead")]
    public decimal Price { get; set; }
}
```

### GraphQLArgumentAttribute

Defines arguments for GraphQL operations with descriptions and custom names.

#### Basic Arguments

```csharp
[LambdaFunction]
[GraphQLQuery("getProduct")]
public async Task<Product> GetProduct(
    [GraphQLArgument(Description = "Product ID")] Guid id)
{
    return await productService.GetByIdAsync(id);
}
```

#### Multiple Arguments

```csharp
[LambdaFunction]
[GraphQLQuery("searchProducts")]
public async Task<List<Product>> SearchProducts(
    [GraphQLArgument(Description = "Search term")] string query,
    [GraphQLArgument(Description = "Product category filter")] ProductCategory? category,
    [GraphQLArgument(Description = "Maximum number of results")] int limit,
    [GraphQLArgument(Description = "Results offset for pagination")] int offset)
{
    return await productService.SearchAsync(query, category, limit, offset);
}
```

#### Custom Argument Names

```csharp
[LambdaFunction]
[GraphQLQuery("getUser")]
public async Task<User> GetUser(
    [GraphQLArgument("userId", Description = "User identifier")] Guid id,
    [GraphQLArgument("includeProfile", Description = "Include user profile data")] bool profile = false)
{
    return await userService.GetAsync(id, profile);
}
```

---

## Operation Attributes

### GraphQLQueryAttribute

Marks Lambda functions as GraphQL query operations.

#### Basic Query

```csharp
[LambdaFunction]
[GraphQLQuery("getProduct", Description = "Retrieve a product by ID")]
[GraphQLResolver(DataSource = "ProductsLambda")]
public async Task<Product> GetProduct(Guid id)
{
    return await productService.GetByIdAsync(id);
}
```

#### List Query

```csharp
[LambdaFunction]
[GraphQLQuery("listProducts", Description = "Get all products with optional filtering")]
[GraphQLResolver(DataSource = "ProductsLambda")]
public async Task<List<Product>> ListProducts(
    ProductCategory? category = null,
    int limit = 50)
{
    return await productService.GetAllAsync(category, limit);
}
```

### GraphQLMutationAttribute

Marks Lambda functions as GraphQL mutation operations.

#### Create Mutation

```csharp
[LambdaFunction]
[GraphQLMutation("createProduct", Description = "Create a new product")]
[GraphQLResolver(DataSource = "ProductsLambda")]
public async Task<Product> CreateProduct(CreateProductInput input)
{
    return await productService.CreateAsync(input);
}
```

#### Update Mutation

```csharp
[LambdaFunction]
[GraphQLMutation("updateProduct", Description = "Update an existing product")]
[GraphQLResolver(DataSource = "ProductsLambda")]
public async Task<Product> UpdateProduct(Guid id, UpdateProductInput input)
{
    return await productService.UpdateAsync(id, input);
}
```

#### Delete Mutation

```csharp
[LambdaFunction]
[GraphQLMutation("deleteProduct", Description = "Delete a product")]
[GraphQLResolver(DataSource = "ProductsLambda")]
public async Task<bool> DeleteProduct(Guid id)
{
    return await productService.DeleteAsync(id);
}
```

### GraphQLSubscriptionAttribute

Marks Lambda functions as GraphQL subscription operations for real-time updates.

#### Basic Subscription

```csharp
[LambdaFunction]
[GraphQLSubscription("orderStatusChanged", Description = "Subscribe to order status changes")]
[GraphQLResolver(DataSource = "OrderSubscriptionLambda")]
public async Task<Order> OrderStatusChanged(Guid orderId)
{
    // Subscription implementation
    return await orderService.GetAsync(orderId);
}
```

#### Filtered Subscription

```csharp
[LambdaFunction]
[GraphQLSubscription("productUpdated", Description = "Subscribe to product updates by category")]
[GraphQLResolver(DataSource = "ProductSubscriptionLambda")]
public async Task<Product> ProductUpdated(ProductCategory category)
{
    // Filtered subscription implementation
    return await productService.GetLatestInCategoryAsync(category);
}
```

---

## Advanced Type Attributes

### GraphQLEnumValueAttribute

Provides metadata for GraphQL enum values.

#### Basic Enum Values

```csharp
[GraphQLType("OrderStatus")]
public enum OrderStatus
{
    [GraphQLEnumValue(Description = "Order has been placed but not processed")]
    Pending,
    
    [GraphQLEnumValue(Description = "Order is being prepared")]
    Processing,
    
    [GraphQLEnumValue(Description = "Order has been shipped")]
    Shipped,
    
    [GraphQLEnumValue(Description = "Order has been delivered")]
    Delivered
}
```

#### Deprecated Enum Values

```csharp
[GraphQLType("PaymentMethod")]
public enum PaymentMethod
{
    [GraphQLEnumValue(Description = "Credit card payment")]
    CreditCard,
    
    [GraphQLEnumValue(Description = "PayPal payment")]
    PayPal,
    
    [GraphQLEnumValue(Deprecated = true, DeprecationReason = "Use CreditCard instead")]
    Visa,
    
    [GraphQLEnumValue(Description = "Bank transfer")]
    BankTransfer
}
```

#### Custom Enum Value Names

```csharp
[GraphQLType("Priority")]
public enum TaskPriority
{
    [GraphQLEnumValue("LOW", Description = "Low priority task")]
    Low,
    
    [GraphQLEnumValue("MEDIUM", Description = "Medium priority task")]
    Medium,
    
    [GraphQLEnumValue("HIGH", Description = "High priority task")]
    High,
    
    [GraphQLEnumValue("CRITICAL", Description = "Critical priority task")]
    Critical
}
```

---

## Directive Attributes

### GraphQLDirectiveAttribute

Defines custom GraphQL directives at the assembly level.

#### Custom Authentication Directive

```csharp
[assembly: GraphQLDirective("auth",
    Description = "Requires authentication with specific permissions",
    Locations = DirectiveLocation.FieldDefinition | DirectiveLocation.Object,
    Arguments = "requires: String!")]
```

#### Rate Limiting Directive

```csharp
[assembly: GraphQLDirective("rateLimit",
    Description = "Applies rate limiting to field or type",
    Locations = DirectiveLocation.FieldDefinition,
    Arguments = "max: Int!, window: Int!")]
```

### GraphQLApplyDirectiveAttribute

Applies directives to types and fields.

#### Apply Custom Directive

```csharp
[GraphQLType("AdminPanel")]
[GraphQLApplyDirective("auth", Arguments = "requires: \"ADMIN\"")]
public class AdminPanel
{
    [GraphQLField]
    [GraphQLApplyDirective("rateLimit", Arguments = "max: 10, window: 60")]
    public string SensitiveData { get; set; }
}
```

### GraphQLAuthDirectiveAttribute

Applies AWS AppSync authentication directives.

#### Cognito User Pools Authentication

```csharp
[GraphQLType("UserProfile")]
[GraphQLAuthDirective(AuthMode.UserPools)]
public class UserProfile
{
    [GraphQLField]
    public string Email { get; set; }
    
    [GraphQLField]
    [GraphQLAuthDirective(AuthMode.UserPools, CognitoGroups = "admin,moderator")]
    public string AdminNotes { get; set; }
}
```

#### IAM Authentication

```csharp
[GraphQLType("SystemMetrics")]
[GraphQLAuthDirective(AuthMode.IAM)]
public class SystemMetrics
{
    [GraphQLField]
    [GraphQLAuthDirective(AuthMode.IAM, IamResource = "arn:aws:dynamodb:us-east-1:123456789012:table/metrics")]
    public int DatabaseConnections { get; set; }
}
```

#### Multiple Authentication Modes

```csharp
[GraphQLType("Product")]
public class Product
{
    [GraphQLField]
    public string Name { get; set; }
    
    [GraphQLField]
    [GraphQLAuthDirective(AuthMode.UserPools)]
    [GraphQLAuthDirective(AuthMode.IAM)]
    public decimal Cost { get; set; } // Accessible via User Pools OR IAM
}
```

---

## Configuration Attributes

### GraphQLSchemaAttribute

Provides assembly-level schema configuration.

```csharp
[assembly: GraphQLSchema("ECommerceAPI",
    Description = "E-commerce GraphQL API for product management",
    Version = "2.1.0")]
```

### GraphQLResolverAttribute

Configures resolver settings for GraphQL operations.

#### Unit Resolver

```csharp
[LambdaFunction]
[GraphQLQuery("getProduct")]
[GraphQLResolver(DataSource = "ProductsLambda")]
public async Task<Product> GetProduct(Guid id) { }
```

#### Pipeline Resolver

```csharp
[LambdaFunction]
[GraphQLMutation("processOrder")]
[GraphQLResolver(
    Kind = "Pipeline",
    Functions = new[] { "ValidateOrder", "ProcessPayment", "CreateOrder", "SendConfirmation" })]
public async Task<Order> ProcessOrder(OrderInput input) { }
```

#### Custom Mapping Templates

```csharp
[LambdaFunction]
[GraphQLQuery("complexQuery")]
[GraphQLResolver(
    DataSource = "ComplexLambda",
    RequestMapping = "custom-request.vtl",
    ResponseMapping = "custom-response.vtl")]
public async Task<ComplexResult> ComplexQuery(ComplexInput input) { }
```

### GraphQLScalarAttribute

Defines custom scalar types.

```csharp
[GraphQLScalar("DateTime", Description = "Custom date-time scalar")]
public class CustomDateTime
{
    public DateTime Value { get; set; }
}
```

---

## Modifier Attributes

### GraphQLIgnoreAttribute

Excludes properties from the GraphQL schema.

```csharp
[GraphQLType("User")]
public class User
{
    [GraphQLField]
    public Guid Id { get; set; }
    
    [GraphQLField]
    public string Name { get; set; }
    
    [GraphQLIgnore]
    public string PasswordHash { get; set; } // Not included in schema
    
    [GraphQLIgnore]
    public DateTime LastLoginInternal { get; set; } // Internal tracking
}
```

### GraphQLNonNullAttribute

Overrides nullability to make fields non-null in GraphQL.

```csharp
[GraphQLType("Product")]
public class Product
{
    [GraphQLField]
    [GraphQLNonNull]
    public string? Name { get; set; } // Becomes String! in GraphQL
    
    [GraphQLField]
    public string? Description { get; set; } // Remains String (nullable)
    
    [GraphQLField]
    [GraphQLNonNull]
    public decimal? Price { get; set; } // Becomes Float! in GraphQL
}
```

### GraphQLTimestampAttribute

Marks fields as AWS timestamp scalars for Unix timestamp values.

```csharp
[GraphQLType("User")]
public class User
{
    [GraphQLField]
    public DateTime CreatedAt { get; set; } // → AWSDateTime!
    
    [GraphQLField(Description = "Account creation timestamp (Unix seconds)")]
    [GraphQLTimestamp]
    public long CreatedAtTimestamp { get; set; } // → AWSTimestamp!
    
    [GraphQLField(Description = "Last login timestamp")]
    [GraphQLTimestamp]
    public long? LastLoginTimestamp { get; set; } // → AWSTimestamp
}
```

---

## Best Practices

### Naming Conventions

```csharp
// Use PascalCase for types
[GraphQLType("ProductCategory")]
public enum ProductCategory { }

// Use camelCase for fields and arguments
[GraphQLField("displayName")]
public string Name { get; set; }

[GraphQLArgument("userId")]
public Guid Id { get; set; }
```

### Descriptions

```csharp
// Always provide meaningful descriptions
[GraphQLType("Product", Description = "A product in the e-commerce catalog")]
public class Product
{
    [GraphQLField(Description = "Unique product identifier used for lookups")]
    public Guid Id { get; set; }
    
    [GraphQLField(Description = "Product display name shown to customers")]
    public string Name { get; set; }
}
```

### Deprecation

```csharp
// Use deprecation instead of removing fields
[GraphQLType("User")]
public class User
{
    [GraphQLField(Description = "User's full name")]
    public string FullName { get; set; }
    
    [GraphQLField(
        Deprecated = true, 
        DeprecationReason = "Use FullName instead. Will be removed in v3.0")]
    public string Name { get; set; }
}
```

### Input Validation

```csharp
// Use descriptive input types
[GraphQLType("CreateUserInput", Kind = GraphQLTypeKind.Input)]
public class CreateUserInput
{
    [GraphQLField(Description = "User's email address (must be valid email format)")]
    public string Email { get; set; }
    
    [GraphQLField(Description = "User's full name (2-100 characters)")]
    public string FullName { get; set; }
    
    [GraphQLField(Description = "User's age (must be 13 or older)")]
    public int Age { get; set; }
}
```

---

For more examples and advanced usage patterns, see the [Examples Guide](examples.md) and [Advanced Features](advanced-features.md).
