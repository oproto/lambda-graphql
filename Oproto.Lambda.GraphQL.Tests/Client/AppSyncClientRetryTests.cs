using System.Net;
using System.Text;
using FluentAssertions;
using Oproto.Lambda.GraphQL.Client;

namespace Oproto.Lambda.GraphQL.Tests.Client;

public class AppSyncClientRetryTests
{
    private static AppSyncClient CreateClient(int maxRetries)
    {
        // HttpClient is required but won't be used — we call ExecuteWithRetryAsync directly
        var handler = new NoOpHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        return new AppSyncClient(new AppSyncClientOptions
        {
            Endpoint = "https://test.appsync-api.us-east-1.amazonaws.com/graphql",
            AuthMode = AuthMode.ApiKey,
            ApiKey = "da2-fakeapikey123",
            MaxRetries = maxRetries,
            HttpClient = httpClient
        });
    }

    private class NoOpHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    [Fact]
    public async Task RetryOn5xx_ThenSuccess_RetriesAndReturnsSuccessfulResponse()
    {
        // Arrange
        using var client = CreateClient(maxRetries: 1);
        var callCount = 0;

        // Act
        var response = await client.ExecuteWithRetryAsync(() =>
        {
            callCount++;
            return Task.FromResult(callCount switch
            {
                1 => new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(
                        "{\"errors\":[{\"message\":\"Server error\"}]}",
                        Encoding.UTF8, "application/json")
                },
                _ => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"data\":{\"id\":\"ok\"}}",
                        Encoding.UTF8, "application/json")
                }
            });
        }, CancellationToken.None);

        // Assert
        callCount.Should().Be(2);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NoRetryOn4xx_ReturnsImmediately()
    {
        // Arrange
        using var client = CreateClient(maxRetries: 3);
        var callCount = 0;

        // Act
        var response = await client.ExecuteWithRetryAsync(() =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    "{\"errors\":[{\"message\":\"Bad request\"}]}",
                    Encoding.UTF8, "application/json")
            });
        }, CancellationToken.None);

        // Assert
        callCount.Should().Be(1);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RetryExhaustion_ReturnsLastError()
    {
        // Arrange
        using var client = CreateClient(maxRetries: 2);
        var callCount = 0;

        // Act
        var response = await client.ExecuteWithRetryAsync(() =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(
                    "{\"errors\":[{\"message\":\"Server error\"}]}",
                    Encoding.UTF8, "application/json")
            });
        }, CancellationToken.None);

        // Assert
        callCount.Should().Be(3); // 1 initial + 2 retries
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}
