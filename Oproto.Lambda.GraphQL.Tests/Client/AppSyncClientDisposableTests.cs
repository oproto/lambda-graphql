using System.Net;
using FluentAssertions;
using Oproto.Lambda.GraphQL.Client;

namespace Oproto.Lambda.GraphQL.Tests.Client;

public class AppSyncClientDisposableTests
{
    private class TrackingHttpMessageHandler : HttpMessageHandler
    {
        public bool IsDisposed { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    [Fact]
    public void Dispose_InternallyCreatedHttpClient_IsDisposed()
    {
        // Arrange — no HttpClient supplied, so AppSyncClient creates one internally
        var client = new AppSyncClient(new AppSyncClientOptions
        {
            Endpoint = "https://test.appsync-api.us-east-1.amazonaws.com/graphql",
            AuthMode = AuthMode.ApiKey,
            ApiKey = "da2-fakeapikey123"
        });

        // Act
        client.Dispose();

        // Assert — sending a request after disposal should throw because the internal HttpClient is disposed
        var act = async () => await client.SendAsync<object>("{ test }");
        act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task Dispose_CallerSuppliedHttpClient_IsNotDisposed()
    {
        // Arrange
        var handler = new TrackingHttpMessageHandler();
        var callerHttpClient = new HttpClient(handler);

        var client = new AppSyncClient(new AppSyncClientOptions
        {
            Endpoint = "https://test.appsync-api.us-east-1.amazonaws.com/graphql",
            AuthMode = AuthMode.ApiKey,
            ApiKey = "da2-fakeapikey123",
            HttpClient = callerHttpClient
        });

        // Act — dispose the AppSyncClient
        client.Dispose();

        // Assert — caller's HttpClient should still be usable
        handler.IsDisposed.Should().BeFalse();

        var response = await callerHttpClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://example.com"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
