using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Oproto.Lambda.GraphQL.Attributes;
using Oproto.Lambda.GraphQL.Runtime;
using System;
using System.Threading.Tasks;

namespace Oproto.Lambda.GraphQL.Examples;

/// <summary>
/// Example Lambda functions with GraphQL operations demonstrating field selection.
/// </summary>
public class ProductFunctions
{
    /// <summary>
    /// Gets a product by ID, using field selection to shape the response
    /// to only include the fields requested by the GraphQL client.
    /// </summary>
    [LambdaFunction(MemorySize = 1024, Timeout = 30)]
    [GraphQLQuery("getProduct", Description = "Get a product by ID")]
    [GraphQLResolver]
    public Task<string> GetProduct(AppSyncResolverContext<GetProductArguments> ctx)
    {
        var id = ctx.Arguments?.Id ?? string.Empty;

        // Extract field selection from the resolver context, mapping GraphQL field names
        // (e.g., "displayName") to C# property names (e.g., "Name") using the
        // source-generated GraphQLFieldMap on the Product class.
        var selection = ctx.GetFieldSelection(Product.GraphQLFieldMap);

        // Fetch the full product (you might skip expensive work based on selection)
        var product = new Product { Id = id, Name = "Sample Product", Price = 99.99m };

        // Shape the response to only include requested fields
        return Task.FromResult(ResponseShaper.ShapeResponse(product, selection));
    }

    [LambdaFunction(MemorySize = 512, Timeout = 15)]
    [GraphQLMutation("createProduct", Description = "Create a new product")]
    [GraphQLResolver]
    public Task<Product> CreateProduct(CreateProductInput input)
    {
        return Task.FromResult(new Product
        {
            Id = Guid.NewGuid().ToString(),
            Name = input.Name,
            Price = input.Price
        });
    }
}

/// <summary>
/// Arguments for the getProduct query.
/// </summary>
public class GetProductArguments
{
    public string Id { get; set; } = string.Empty;
}
