# API Reference

This document provides comprehensive API documentation for all classes, attributes, and methods in the Oproto.Lambda.GraphQL library.

## Table of Contents

- [Core Attributes](#core-attributes)
  - [GraphQLTypeAttribute](#graphqltypeattribute)
  - [GraphQLFieldAttribute](#graphqlfieldattribute)
  - [GraphQLArgumentAttribute](#graphqlargumentattribute)
- [Operation Attributes](#operation-attributes)
  - [GraphQLQueryAttribute](#graphqlqueryattribute)
  - [GraphQLMutationAttribute](#graphqlmutationattribute)
  - [GraphQLSubscriptionAttribute](#graphqlsubscriptionattribute)
- [Advanced Type Attributes](#advanced-type-attributes)
  - [GraphQLUnionAttribute](#graphqlunionattribute)
  - [GraphQLEnumValueAttribute](#graphqlenumvalueattribute)
- [Modifier Attributes](#modifier-attributes)
  - [GraphQLIgnoreAttribute](#graphqlignoreattribute)
  - [GraphQLNonNullAttribute](#graphqlnonnullattribute)
  - [GraphQLTimestampAttribute](#graphqltimestampattribute)
- [Directive Attributes](#directive-attributes)
  - [GraphQLDirectiveAttribute](#graphqldirectiveattribute)
  - [GraphQLApplyDirectiveAttribute](#graphqlapplydirectiveattribute)
  - [GraphQLAuthDirectiveAttribute](#graphqlauthdirectiveattribute)
- [Configuration Attributes](#configuration-attributes)
  - [GraphQLSchemaAttribute](#graphqlschemaattribute)
  - [GraphQLResolverAttribute](#graphqlresolverattribute)
  - [GraphQLScalarAttribute](#graphqlscalarattribute)
- [Enums and Types](#enums-and-types)
  - [GraphQLTypeKind](#graphqltypekind)
  - [AuthMode](#authmode)
  - [DirectiveLocation](#directivelocation)

---

## Core Attributes

### GraphQLTypeAttribute

Marks a class, interface, or enum as a GraphQL type.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`  
**Targets**: `Class`, `Interface`, `Enum`

#### Constructors

```csharp
public GraphQLTypeAttribute(string? name = null)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string?` | The GraphQL type name. If null, uses the C# type name. |
| `Description` | `string?` | Description for the GraphQL type. |
| `Kind` | `GraphQLTypeKind` | The kind of GraphQL type (Object, Input, Interface, Enum, Union). |

#### Examples

```csharp
// Basic object type
[GraphQLType("Product")]
public class Product { }

// Input type with description
[GraphQLType("CreateProductInput", Kind = GraphQLTypeKind.Input)]
public class CreateProductInput { }

// Interface type
[GraphQLType("Node", Kind = GraphQLTypeKind.Interface)]
public interface INode { }

// Enum type with custom name
[GraphQLType("ProductCategory")]
public enum Category { }
```

### GraphQLFieldAttribute

Marks a property or field as a GraphQL field.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`  
**Targets**: `Property`, `Field`

#### Constructors

```csharp
public GraphQLFieldAttribute(string? name = null)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string?` | The GraphQL field name. If null, uses the property name. |
| `Description` | `string?` | Description for the GraphQL field. |
| `Deprecated` | `bool` | Whether the field is deprecated. |
| `DeprecationReason` | `string?` | Reason for deprecation. |

#### Examples

```csharp
public class Product
{
    [GraphQLField(Description = "Product identifier")]
    public Guid Id { get; set; }
    
    [GraphQLField("displayName", Description = "Product display name")]
    public string Name { get; set; }
    
    [GraphQLField(Deprecated = true, DeprecationReason = "Use displayPrice instead")]
    public decimal Price { get; set; }
}
```

### GraphQLArgumentAttribute

Marks a method parameter as a GraphQL argument.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`  
**Targets**: `Parameter`

#### Constructors

```csharp
public GraphQLArgumentAttribute(string? name = null)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string?` | The GraphQL argument name. If null, uses the parameter name. |
| `Description` | `string?` | Description for the GraphQL argument. |

#### Examples

```csharp
[GraphQLQuery("getProduct")]
public async Task<Product> GetProduct(
    [GraphQLArgument(Description = "Product ID")] Guid id,
    [GraphQLArgument("includeDetails")] bool details = false)
{
    // Implementation
}
```

---

## Operation Attributes

### GraphQLQueryAttribute

Marks a Lambda function as a GraphQL query operation.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`  
**Targets**: `Method`

#### Constructors

```csharp
public GraphQLQueryAttribute(string? name = null)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string?` | The GraphQL query name. If null, uses the method name. |
| `Description` | `string?` | Description for the GraphQL query. |

#### Examples

```csharp
[LambdaFunction]
[GraphQLQuery("getProduct", Description = "Get a product by ID")]
public async Task<Product> GetProduct(Guid id) { }

[LambdaFunction]
[GraphQLQuery] // Uses method name "ListProducts"
public async Task<List<Product>> ListProducts() { }
```

### GraphQLMutationAttribute

Marks a Lambda function as a GraphQL mutation operation.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`  
**Targets**: `Method`

#### Constructors

```csharp
public GraphQLMutationAttribute(string? name = null)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string?` | The GraphQL mutation name. If null, uses the method name. |
| `Description` | `string?` | Description for the GraphQL mutation. |

#### Examples

```csharp
[LambdaFunction]
[GraphQLMutation("createProduct", Description = "Create a new product")]
public async Task<Product> CreateProduct(CreateProductInput input) { }
```

### GraphQLSubscriptionAttribute

Marks a Lambda function as a GraphQL subscription operation.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`  
**Targets**: `Method`

#### Constructors

```csharp
public GraphQLSubscriptionAttribute(string? name = null)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string?` | The GraphQL subscription name. If null, uses the method name. |
| `Description` | `string?` | Description for the GraphQL subscription. |

#### Examples

```csharp
[LambdaFunction]
[GraphQLSubscription("orderStatusChanged", Description = "Subscribe to order status changes")]
public async Task<Order> OrderStatusChanged(Guid orderId) { }
```

---

## Advanced Type Attributes

### GraphQLUnionAttribute

Marks a class as a GraphQL union type with specified member types.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`  
**Targets**: `Class`

#### Constructors

```csharp
public GraphQLUnionAttribute(string? name = null, params string[] memberTypes)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string?` | The GraphQL union name. If null, uses the class name. |
| `Description` | `string?` | Description for the GraphQL union. |
| `MemberTypes` | `string[]` | Array of member type names for the union. |

#### Examples

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

### GraphQLEnumValueAttribute

Provides metadata for GraphQL enum values.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`  
**Targets**: `Field` (enum members)

#### Constructors

```csharp
public GraphQLEnumValueAttribute(string? name = null)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string?` | The GraphQL enum value name. If null, uses the enum member name. |
| `Description` | `string?` | Description for the GraphQL enum value. |
| `Deprecated` | `bool` | Whether the enum value is deprecated. |
| `DeprecationReason` | `string?` | Reason for deprecation. |

#### Examples

```csharp
[GraphQLType("OrderStatus")]
public enum OrderStatus
{
    [GraphQLEnumValue(Description = "Order is pending")]
    Pending,
    
    [GraphQLEnumValue(Deprecated = true, DeprecationReason = "Use Shipped instead")]
    Processing,
    
    Shipped
}
```

---

## Modifier Attributes

### GraphQLIgnoreAttribute

Excludes a property or field from the GraphQL schema.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`  
**Targets**: `Property`, `Field`

#### Examples

```csharp
public class Product
{
    [GraphQLField]
    public string Name { get; set; }
    
    [GraphQLIgnore]
    public DateTime InternalTimestamp { get; set; } // Not included in schema
}
```

### GraphQLNonNullAttribute

Overrides nullability to make a field non-null in GraphQL.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`  
**Targets**: `Property`, `Field`

#### Examples

```csharp
public class Product
{
    [GraphQLField]
    [GraphQLNonNull]
    public string? RequiredField { get; set; } // Becomes String! in GraphQL
}
```

### GraphQLTimestampAttribute

Marks a field as an AWS timestamp scalar (AWSTimestamp).

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`  
**Targets**: `Property`, `Field`

#### Examples

```csharp
public class User
{
    [GraphQLField(Description = "Account creation timestamp (Unix seconds)")]
    [GraphQLTimestamp]
    public long CreatedAtTimestamp { get; set; } // Becomes AWSTimestamp! in GraphQL
}
```

---

## Directive Attributes

### GraphQLDirectiveAttribute

Defines a custom GraphQL directive.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`  
**Targets**: `Assembly`  
**AllowMultiple**: `true`

#### Constructors

```csharp
public GraphQLDirectiveAttribute(string name)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | The directive name. |
| `Description` | `string?` | Description for the directive. |
| `Locations` | `DirectiveLocation` | Where the directive can be applied. |
| `Arguments` | `string?` | Directive arguments definition. |

#### Examples

```csharp
[assembly: GraphQLDirective("auth", 
    Locations = DirectiveLocation.FieldDefinition,
    Arguments = "requires: String!")]
```

### GraphQLApplyDirectiveAttribute

Applies a directive to a GraphQL type or field.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`  
**Targets**: `Class`, `Property`, `Method`, `Field`  
**AllowMultiple**: `true`

#### Constructors

```csharp
public GraphQLApplyDirectiveAttribute(string directiveName)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `DirectiveName` | `string` | The name of the directive to apply. |
| `Arguments` | `string?` | Arguments for the directive. |

#### Examples

```csharp
[GraphQLField]
[GraphQLApplyDirective("auth", Arguments = "requires: \"ADMIN\"")]
public string AdminOnlyField { get; set; }
```

### GraphQLAuthDirectiveAttribute

Applies AWS authentication directives to GraphQL types or fields.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`  
**Targets**: `Class`, `Property`, `Method`, `Field`, `Enum`  
**AllowMultiple**: `true`

#### Constructors

```csharp
public GraphQLAuthDirectiveAttribute(AuthMode authMode)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `AuthMode` | `AuthMode` | The AWS authentication mode. |
| `CognitoGroups` | `string?` | Comma-separated list of Cognito groups. |
| `IamResource` | `string?` | IAM resource ARN. |

#### Examples

```csharp
[GraphQLType("AdminData")]
[GraphQLAuthDirective(AuthMode.UserPools, CognitoGroups = "admin")]
public class AdminData { }

[GraphQLField]
[GraphQLAuthDirective(AuthMode.IAM, IamResource = "arn:aws:dynamodb:*")]
public string SensitiveData { get; set; }
```

---

## Configuration Attributes

### GraphQLSchemaAttribute

Provides assembly-level schema configuration.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`  
**Targets**: `Assembly`

#### Constructors

```csharp
public GraphQLSchemaAttribute(string name)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | The schema name. |
| `Description` | `string?` | Description for the schema. |
| `Version` | `string?` | Schema version. |

#### Examples

```csharp
[assembly: GraphQLSchema("ProductsAPI", 
    Description = "Product catalog GraphQL API",
    Version = "1.0.0")]
```

### GraphQLResolverAttribute

Configures resolver settings for GraphQL operations.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`  
**Targets**: `Method`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `DataSource` | `string?` | The AppSync data source name. |
| `Kind` | `string?` | Resolver kind ("Unit" or "Pipeline"). |
| `RequestMapping` | `string?` | Custom request mapping template. |
| `ResponseMapping` | `string?` | Custom response mapping template. |
| `Functions` | `string[]?` | Pipeline function names (for pipeline resolvers). |

#### Examples

```csharp
[LambdaFunction]
[GraphQLQuery("getProduct")]
[GraphQLResolver(DataSource = "ProductsLambda")]
public async Task<Product> GetProduct(Guid id) { }

[LambdaFunction]
[GraphQLMutation("processOrder")]
[GraphQLResolver(Kind = "Pipeline", Functions = new[] { "ValidateOrder", "ProcessPayment", "CreateOrder" })]
public async Task<Order> ProcessOrder(OrderInput input) { }
```

### GraphQLScalarAttribute

Marks a class as a custom GraphQL scalar type.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`  
**Targets**: `Class`, `Struct`

#### Constructors

```csharp
public GraphQLScalarAttribute(string? name = null)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string?` | The GraphQL scalar name. If null, uses the class name. |
| `Description` | `string?` | Description for the scalar. |

#### Examples

```csharp
[GraphQLScalar("DateTime")]
public class CustomDateTime { }
```

---

## Enums and Types

### GraphQLTypeKind

Specifies the kind of GraphQL type.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`

#### Values

| Value | Description |
|-------|-------------|
| `Object` | GraphQL object type (default) |
| `Input` | GraphQL input type |
| `Interface` | GraphQL interface type |
| `Enum` | GraphQL enum type |
| `Union` | GraphQL union type |

### AuthMode

AWS AppSync authentication modes.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`

#### Values

| Value | Description |
|-------|-------------|
| `ApiKey` | API key authentication |
| `UserPools` | Cognito User Pools authentication |
| `IAM` | AWS IAM authentication |
| `OpenIDConnect` | OpenID Connect authentication |
| `Lambda` | Lambda authorizer authentication |

### DirectiveLocation

Specifies where a directive can be applied.

**Namespace**: `Oproto.Lambda.GraphQL.Attributes`

#### Values

| Value | Description |
|-------|-------------|
| `Query` | Query operation |
| `Mutation` | Mutation operation |
| `Subscription` | Subscription operation |
| `Field` | Field selection |
| `FragmentDefinition` | Fragment definition |
| `FragmentSpread` | Fragment spread |
| `InlineFragment` | Inline fragment |
| `VariableDefinition` | Variable definition |
| `Schema` | Schema definition |
| `Scalar` | Scalar type definition |
| `Object` | Object type definition |
| `FieldDefinition` | Field definition |
| `ArgumentDefinition` | Argument definition |
| `Interface` | Interface type definition |
| `Union` | Union type definition |
| `Enum` | Enum type definition |
| `EnumValue` | Enum value definition |
| `InputObject` | Input object type definition |
| `InputFieldDefinition` | Input field definition |

---

## Type Mappings

Oproto.Lambda.GraphQL automatically maps C# types to GraphQL types:

| C# Type | GraphQL Type | Notes |
|---------|--------------|-------|
| `string` | `String` | |
| `int`, `long` | `Int` | |
| `float`, `double`, `decimal` | `Float` | |
| `bool` | `Boolean` | |
| `Guid` | `ID` | |
| `DateTime` | `AWSDateTime` | AWS AppSync scalar |
| `DateTimeOffset` | `AWSDateTime` | AWS AppSync scalar |
| `DateOnly` | `AWSDate` | AWS AppSync scalar |
| `TimeOnly` | `AWSTime` | AWS AppSync scalar |
| `System.Net.Mail.MailAddress` | `AWSEmail` | AWS AppSync scalar |
| `System.Uri` | `AWSURL` | AWS AppSync scalar |
| `System.Net.IPAddress` | `AWSIPAddress` | AWS AppSync scalar |
| `System.Text.Json.JsonElement` | `AWSJSON` | AWS AppSync scalar |
| `T?` (nullable) | `T` | Nullable in GraphQL |
| `List<T>`, `T[]` | `[T]` | GraphQL list |
| Custom classes | Custom Object/Input types | |
| Enums | GraphQL Enums | |

---

For more examples and usage patterns, see the [Attributes Guide](attributes.md) and [Examples](examples.md).
