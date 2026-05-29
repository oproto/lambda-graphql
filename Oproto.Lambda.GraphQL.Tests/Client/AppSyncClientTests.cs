using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Oproto.Lambda.GraphQL.Client;

namespace Oproto.Lambda.GraphQL.Tests.Client;

public class AppSyncClientTests
{
    private class TestResult
    {
        public string? Id { get; set; }
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        public HttpResponseMessage ResponseToReturn { get; set; } = new(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":null}", Encoding.UTF8, "application/json")
        };

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            return ResponseToReturn;
        }
    }

    private static (AppSyncClient client, MockHttpMessageHandler handler) CreateClient()
    {
        var handler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var client = new AppSyncClient(new AppSyncClientOptions
        {
            Endpoint = "https://test.appsync-api.us-east-1.amazonaws.com/graphql",
            AuthMode = AuthMode.ApiKey,
            ApiKey = "da2-fakeapikey123",
            MaxRetries = 0,
            HttpClient = httpClient
        });
        return (client, handler);
    }

    [Fact]
    public async Task SendAsync_WithVariables_BodyContainsQueryAndVariablesFields()
    {
        // Arrange
        var (client, handler) = CreateClient();
        var query = "mutation CreateItem($input: CreateItemInput!) { createItem(input: $input) { id } }";
        var variables = new { input = new { name = "test-item" } };

        // Act
        await client.SendAsync<TestResult>(query, variables);

        // Assert
        handler.LastRequestBody.Should().NotBeNullOrEmpty();
        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var root = doc.RootElement;

        root.TryGetProperty("query", out var queryProp).Should().BeTrue();
        queryProp.GetString().Should().Be(query);

        root.TryGetProperty("variables", out var varsProp).Should().BeTrue();
        varsProp.GetProperty("input").GetProperty("name").GetString().Should().Be("test-item");
    }

    [Fact]
    public async Task SendAsync_WithNullVariables_BodyOmitsVariablesField()
    {
        // Arrange
        var (client, handler) = CreateClient();
        var query = "{ listItems { id } }";

        // Act
        await client.SendAsync<TestResult>(query, variables: null);

        // Assert
        handler.LastRequestBody.Should().NotBeNullOrEmpty();
        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var root = doc.RootElement;

        root.TryGetProperty("query", out var queryProp).Should().BeTrue();
        queryProp.GetString().Should().Be(query);

        root.TryGetProperty("variables", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_SetsContentTypeToApplicationJson()
    {
        // Arrange
        var (client, handler) = CreateClient();

        // Act
        await client.SendAsync<TestResult>("{ listItems { id } }");

        // Assert
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task SendAsync_WithApiKeyAuth_IncludesApiKeyHeader()
    {
        // Arrange
        var (client, handler) = CreateClient();

        // Act
        await client.SendAsync<TestResult>("{ listItems { id } }");

        // Assert
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Headers.TryGetValues("x-api-key", out var values).Should().BeTrue();
        values.Should().ContainSingle().Which.Should().Be("da2-fakeapikey123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendAsync_WithApiKeyAuth_WhenApiKeyNullOrEmpty_ReturnsFailedResponse(string? apiKey)
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var client = new AppSyncClient(new AppSyncClientOptions
        {
            Endpoint = "https://test.appsync-api.us-east-1.amazonaws.com/graphql",
            AuthMode = AuthMode.ApiKey,
            ApiKey = apiKey,
            MaxRetries = 0,
            HttpClient = httpClient
        });

        // Act
        var response = await client.SendAsync<TestResult>("{ listItems { id } }");

        // Assert
        response.IsSuccess.Should().BeFalse();
        response.Data.Should().BeNull();
        response.Exception.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Contain("ApiKey must be provided");
    }

    [Fact]
    public async Task SendAsync_SuccessfulResponse_DeserializesIntoTypedResult()
    {
        // Arrange
        var (client, handler) = CreateClient();
        handler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"data":{"id":"item-123"}}""",
                Encoding.UTF8, "application/json")
        };

        // Act
        var response = await client.SendAsync<TestResult>("{ getItem { id } }");

        // Assert
        response.IsSuccess.Should().BeTrue();
        response.HasErrors.Should().BeFalse();
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be("item-123");
    }

    [Fact]
    public async Task SendAsync_ErrorResponse_DeserializesGraphQLErrors()
    {
        // Arrange
        var (client, handler) = CreateClient();
        handler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"data":null,"errors":[{"message":"Not found","errorType":"NotFound","path":["getItem"],"locations":[{"line":1,"column":1}]}]}""",
                Encoding.UTF8, "application/json")
        };

        // Act
        var response = await client.SendAsync<TestResult>("{ getItem { id } }");

        // Assert
        response.HasErrors.Should().BeTrue();
        response.Data.Should().BeNull();
        response.Errors.Should().HaveCount(1);
        response.Errors![0].Message.Should().Be("Not found");
        response.Errors[0].ErrorType.Should().Be("NotFound");
        response.Errors[0].Path.Should().ContainSingle().Which.Should().Be("getItem");
        response.Errors[0].Locations.Should().HaveCount(1);
        response.Errors[0].Locations![0].Line.Should().Be(1);
        response.Errors[0].Locations![0].Column.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_MixedResponse_PopulatesBothDataAndErrors()
    {
        // Arrange
        var (client, handler) = CreateClient();
        handler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"data":{"id":"partial"},"errors":[{"message":"Partial failure"}]}""",
                Encoding.UTF8, "application/json")
        };

        // Act
        var response = await client.SendAsync<TestResult>("{ getItem { id } }");

        // Assert
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be("partial");
        response.Errors.Should().HaveCount(1);
        response.Errors![0].Message.Should().Be("Partial failure");
        response.HasErrors.Should().BeTrue();
        response.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_NonJsonResponse_ReturnsRawBody()
    {
        // Arrange
        var (client, handler) = CreateClient();
        handler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Internal Server Error", Encoding.UTF8, "text/plain")
        };

        // Act
        var response = await client.SendAsync<TestResult>("{ getItem { id } }");

        // Assert
        response.Data.Should().BeNull();
        response.RawBody.Should().Be("Internal Server Error");
    }
}
