using Amazon.Lambda.Annotations;
using Oproto.Lambda.GraphQL.Attributes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Oproto.Lambda.GraphQL.Examples;

/// <summary>
/// Lambda functions demonstrating advanced GraphQL features.
/// </summary>
public class AdvancedFunctions
{
    [LambdaFunction]
    [GraphQLQuery("search", Description = "Search for products, users, or orders", ReturnType = "SearchResult")]
    [GraphQLResolver]
    public async Task<List<object>> Search(SearchInput input)
    {
        await Task.Delay(1);
        return new List<object>();
    }

    [LambdaFunction]
    [GraphQLQuery("getUser", Description = "Get a user by ID")]
    [GraphQLResolver]
    [GraphQLAuthDirective(AuthMode.UserPools)]
    public async Task<User> GetUser(
        [GraphQLArgument(Description = "User ID")] Guid id)
    {
        await Task.Delay(1);
        return new User
        {
            Id = id,
            CreatedAt = DateTime.UtcNow,
            Email = new System.Net.Mail.MailAddress("user@example.com"),
            CreatedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    [LambdaFunction]
    [GraphQLMutation("createUser", Description = "Create a new user")]
    [GraphQLResolver]
    [GraphQLAuthDirective(AuthMode.UserPools, CognitoGroups = "admin")]
    public async Task<User> CreateUser(
        [GraphQLArgument(Description = "User creation input")] CreateUserInput input)
    {
        await Task.Delay(1);
        return new User
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Email = new System.Net.Mail.MailAddress(input.Email),
            CreatedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    [LambdaFunction]
    [GraphQLSubscription("orderStatusChanged", Description = "Subscribe to order status changes")]
    [GraphQLResolver]
    [GraphQLAuthDirective(AuthMode.UserPools)]
    public async Task<Order> OrderStatusChanged(
        [GraphQLArgument(Description = "Order ID to watch")] Guid orderId)
    {
        await Task.Delay(1);
        return new Order { Id = orderId, CreatedAt = DateTime.UtcNow, Total = 0, Status = OrderStatus.Pending };
    }
}
