using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Oproto.Lambda.GraphQL.Attributes;
using System;
using System.Threading.Tasks;

namespace Oproto.Lambda.GraphQL.Examples;

/// <summary>
/// Example Lambda functions with GraphQL operations.
/// </summary>
public class ProductFunctions
{
    [LambdaFunction(MemorySize = 1024, Timeout = 30)]
    [GraphQLQuery("getProduct", Description = "Get a product by ID")]
    [GraphQLResolver]
    public Task<Product> GetProduct(string id)
    {
        // TODO: Implement product retrieval
        return Task.FromResult(new Product { Id = id, Name = "Sample Product", Price = 99.99m });
    }

    [LambdaFunction(MemorySize = 512, Timeout = 15)]
    [GraphQLMutation("createProduct", Description = "Create a new product")]
    [GraphQLResolver]
    public Task<Product> CreateProduct(CreateProductInput input)
    {
        // TODO: Implement product creation
        return Task.FromResult(new Product 
        { 
            Id = Guid.NewGuid().ToString(), 
            Name = input.Name, 
            Price = input.Price 
        });
    }
}
