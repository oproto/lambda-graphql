using Oproto.Lambda.GraphQL.Attributes;
using System.Text.Json.Serialization;

namespace Oproto.Lambda.GraphQL.Examples;

/// <summary>
/// Example product type for GraphQL schema generation.
/// Must be partial to receive the source-generated GraphQLFieldMap property.
/// </summary>
[GraphQLType("Product", Description = "A product in the catalog")]
public partial class Product
{
    [GraphQLField(Description = "Unique product identifier")]
    public string Id { get; set; } = string.Empty;

    [GraphQLField("displayName", Description = "Product display name")]
    [JsonPropertyName("displayName")]
    public string Name { get; set; } = string.Empty;

    [GraphQLField(Description = "Product price in USD")]
    public decimal Price { get; set; }
}

/// <summary>
/// Example input type for creating products.
/// </summary>
[GraphQLType("CreateProductInput", Kind = GraphQLTypeKind.Input)]
public class CreateProductInput
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
