#nullable enable
using System;
using Oproto.Lambda.GraphQL.Attributes;

namespace Oproto.Lambda.GraphQL.Examples;

// Union type example
[GraphQLUnion("SearchResult", "Product", "User", "Order")]
public class SearchResult
{
    // This class serves as a marker for the union type
    // Actual resolution happens in Lambda functions
}

// Interface example with AWS scalars
[GraphQLType("Node", Kind = GraphQLTypeKind.Interface)]
public interface INode
{
    [GraphQLField(Description = "Unique identifier")]
    Guid Id { get; }
    
    [GraphQLField(Description = "Creation timestamp")]
    DateTime CreatedAt { get; }
}

// Object implementing interface with AWS scalars
[GraphQLType("User", Description = "A user in the system")]
[GraphQLAuthDirective(AuthMode.UserPools)]
public partial class User : INode
{
    [GraphQLField(Description = "Unique user identifier")]
    public Guid Id { get; set; }
    
    [GraphQLField(Description = "User creation timestamp")]
    public DateTime CreatedAt { get; set; }
    
    [GraphQLField(Description = "User's email address")]
    public System.Net.Mail.MailAddress Email { get; set; } = null!;
    
    [GraphQLField(Description = "User's profile URL")]
    public Uri? ProfileUrl { get; set; }
    
    [GraphQLField(Description = "User's IP address")]
    public System.Net.IPAddress? LastLoginIp { get; set; }
    
    [GraphQLField(Description = "User metadata as JSON")]
    public System.Text.Json.JsonElement? Metadata { get; set; }
    
    [GraphQLField(Description = "User's birth date")]
    public DateOnly? BirthDate { get; set; }
    
    [GraphQLField(Description = "Preferred notification time")]
    public TimeOnly? NotificationTime { get; set; }
    
    [GraphQLField(Description = "Account creation timestamp (Unix seconds)")]
    [GraphQLTimestamp]
    public long CreatedAtTimestamp { get; set; }
}

// Object implementing interface
[GraphQLType("Order", Description = "An order in the system")]
public partial class Order : INode
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

// Enum with AWS auth directive
[GraphQLType("OrderStatus", Description = "Order status enumeration")]
[GraphQLAuthDirective(AuthMode.UserPools)]
public enum OrderStatus
{
    [GraphQLEnumValue(Description = "Order is pending")]
    Pending,
    
    [GraphQLEnumValue(Description = "Order is being processed")]
    Processing,
    
    [GraphQLEnumValue(Description = "Order has been shipped")]
    Shipped,
    
    [GraphQLEnumValue(Description = "Order has been delivered")]
    Delivered,
    
    [GraphQLEnumValue(Description = "Order was cancelled")]
    Cancelled
}

// Input type for search
[GraphQLType("SearchInput", Kind = GraphQLTypeKind.Input)]
public class SearchInput
{
    [GraphQLField(Description = "Search term")]
    public string Term { get; set; } = string.Empty;
    
    [GraphQLField(Description = "Maximum results to return")]
    public int Limit { get; set; }
}

// Input type with AWS scalars
[GraphQLType("CreateUserInput", Kind = GraphQLTypeKind.Input)]
public class CreateUserInput
{
    [GraphQLField(Description = "User's email address")]
    public string Email { get; set; } = string.Empty;
    
    [GraphQLField(Description = "User's birth date")]
    public DateOnly? BirthDate { get; set; }
    
    [GraphQLField(Description = "User metadata as JSON")]
    public System.Text.Json.JsonElement? Metadata { get; set; }
}
