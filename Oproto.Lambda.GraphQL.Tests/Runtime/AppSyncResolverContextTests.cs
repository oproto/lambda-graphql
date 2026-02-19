using System.Text.Json;
using FluentAssertions;
using Oproto.Lambda.GraphQL.Runtime;

namespace Oproto.Lambda.GraphQL.Tests.Runtime;

public class AppSyncResolverContextTests
{
    private static readonly JsonSerializerOptions Options = AppSyncResolverContextSerializer.DefaultOptions;

    [Fact]
    public void Deserialize_FullContext_AllPropertiesPopulated()
    {
        var json = """
        {
            "arguments": { "id": "prod-123", "limit": 10 },
            "source": { "parentId": "parent-1", "name": "Parent Item" },
            "identity": {
                "sub": "d498a09e-7263-4e6f-b9a1-f5e3b8b2c1d4",
                "issuer": "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_ABC123",
                "username": "john.doe",
                "claims": { "email": "john@example.com" },
                "defaultAuthStrategy": "ALLOW",
                "groups": ["admin"]
            },
            "info": {
                "fieldName": "getProduct",
                "parentTypeName": "Query",
                "selectionSetList": ["id", "name", "category/name", "category/description"],
                "selectionSetGraphQL": "{ id name category { name description } }"
            },
            "request": {
                "headers": {
                    "authorization": "Bearer eyJhbGciOiJSUzI1NiJ9",
                    "x-custom-header": "custom-value"
                }
            },
            "stash": { "cachedValue": "abc" },
            "prev": { "result": "previousResult" }
        }
        """;

        var ctx = JsonSerializer.Deserialize<AppSyncResolverContext<JsonElement>>(json, Options);

        ctx.Should().NotBeNull();

        // Arguments
        ctx!.Arguments.GetProperty("id").GetString().Should().Be("prod-123");
        ctx.Arguments.GetProperty("limit").GetInt32().Should().Be(10);

        // Source
        ctx.Source.Should().NotBeNull();
        ctx.Source!.Value.GetProperty("parentId").GetString().Should().Be("parent-1");

        // Identity
        ctx.Identity.Should().BeOfType<CognitoUserPoolsIdentity>();
        var cognito = (CognitoUserPoolsIdentity)ctx.Identity!;
        cognito.Sub.Should().Be("d498a09e-7263-4e6f-b9a1-f5e3b8b2c1d4");
        cognito.Username.Should().Be("john.doe");
        cognito.DefaultAuthStrategy.Should().Be("ALLOW");
        cognito.Groups.Should().ContainSingle().Which.Should().Be("admin");

        // Info
        ctx.Info.Should().NotBeNull();
        ctx.Info!.FieldName.Should().Be("getProduct");
        ctx.Info.ParentTypeName.Should().Be("Query");
        ctx.Info.SelectionSetList.Should().HaveCount(4);
        ctx.Info.SelectionSetGraphQL.Should().Be("{ id name category { name description } }");

        // Request
        ctx.Request.Should().NotBeNull();
        ctx.Request!.Headers.Should().HaveCount(2);
        ctx.Request.Headers!["authorization"].Should().Be("Bearer eyJhbGciOiJSUzI1NiJ9");

        // Stash
        ctx.Stash.Should().NotBeNull();
        ctx.Stash!.Value.GetProperty("cachedValue").GetString().Should().Be("abc");

        // Prev
        ctx.Prev.Should().NotBeNull();
        ctx.Prev!.Value.GetProperty("result").GetString().Should().Be("previousResult");
    }

    [Fact]
    public void Deserialize_MissingOptionalProperties_DefaultsToNull()
    {
        var json = """
        {
            "arguments": { "id": "123" }
        }
        """;

        var ctx = JsonSerializer.Deserialize<AppSyncResolverContext<JsonElement>>(json, Options);

        ctx.Should().NotBeNull();
        ctx!.Arguments.GetProperty("id").GetString().Should().Be("123");
        ctx.Source.Should().BeNull();
        ctx.Identity.Should().BeNull();
        ctx.Info.Should().BeNull();
        ctx.Request.Should().BeNull();
        ctx.Stash.Should().BeNull();
        ctx.Prev.Should().BeNull();
    }

    [Fact]
    public void Deserialize_MissingSourceAndIdentity_OtherPropertiesPopulated()
    {
        var json = """
        {
            "arguments": { "name": "test" },
            "info": {
                "fieldName": "createItem",
                "parentTypeName": "Mutation"
            },
            "request": {
                "headers": { "content-type": "application/json" }
            }
        }
        """;

        var ctx = JsonSerializer.Deserialize<AppSyncResolverContext<JsonElement>>(json, Options);

        ctx.Should().NotBeNull();
        ctx!.Source.Should().BeNull();
        ctx.Identity.Should().BeNull();
        ctx.Stash.Should().BeNull();
        ctx.Prev.Should().BeNull();
        ctx.Info.Should().NotBeNull();
        ctx.Info!.FieldName.Should().Be("createItem");
        ctx.Request.Should().NotBeNull();
        ctx.Request!.Headers.Should().ContainKey("content-type");
    }

    [Fact]
    public void Deserialize_InfoWithNestedSelectionSetPaths()
    {
        var json = """
        {
            "arguments": {},
            "info": {
                "fieldName": "listProducts",
                "parentTypeName": "Query",
                "selectionSetList": [
                    "id",
                    "name",
                    "category/name",
                    "category/parent/name",
                    "reviews/author/name",
                    "reviews/rating"
                ],
                "selectionSetGraphQL": "{ id name category { name parent { name } } reviews { author { name } rating } }"
            }
        }
        """;

        var ctx = JsonSerializer.Deserialize<AppSyncResolverContext<JsonElement>>(json, Options);

        ctx.Should().NotBeNull();
        ctx!.Info.Should().NotBeNull();
        ctx.Info!.SelectionSetList.Should().HaveCount(6);
        ctx.Info.SelectionSetList.Should().Contain("category/name");
        ctx.Info.SelectionSetList.Should().Contain("category/parent/name");
        ctx.Info.SelectionSetList.Should().Contain("reviews/author/name");
        ctx.Info.SelectionSetList.Should().Contain("reviews/rating");
    }

    [Fact]
    public void Deserialize_RequestWithPopulatedAndEmptyHeaders()
    {
        // Populated headers
        var jsonPopulated = """
        {
            "arguments": {},
            "request": {
                "headers": {
                    "authorization": "Bearer token",
                    "x-forwarded-for": "203.0.113.42",
                    "accept": "application/json"
                }
            }
        }
        """;

        var ctx1 = JsonSerializer.Deserialize<AppSyncResolverContext<JsonElement>>(jsonPopulated, Options);
        ctx1!.Request.Should().NotBeNull();
        ctx1.Request!.Headers.Should().HaveCount(3);
        ctx1.Request.Headers!["x-forwarded-for"].Should().Be("203.0.113.42");

        // Empty headers
        var jsonEmpty = """
        {
            "arguments": {},
            "request": {
                "headers": {}
            }
        }
        """;

        var ctx2 = JsonSerializer.Deserialize<AppSyncResolverContext<JsonElement>>(jsonEmpty, Options);
        ctx2!.Request.Should().NotBeNull();
        ctx2.Request!.Headers.Should().NotBeNull();
        ctx2.Request.Headers.Should().BeEmpty();
    }

    [Fact]
    public void Deserialize_AotCompatible_UsingJsonSerializerContext()
    {
        var json = """
        {
            "arguments": { "key": "value" },
            "identity": {
                "resolverContext": { "tenantId": "t-1", "role": "admin" }
            },
            "info": {
                "fieldName": "getData",
                "parentTypeName": "Query",
                "selectionSetList": ["id", "name"]
            },
            "request": {
                "headers": { "authorization": "Bearer abc" }
            }
        }
        """;

        var ctx = JsonSerializer.Deserialize(
            json,
            AppSyncResolverContextJsonSerializerContext.Default.AppSyncResolverContextJsonElement);

        ctx.Should().NotBeNull();
        ctx!.Arguments.GetProperty("key").GetString().Should().Be("value");

        ctx.Identity.Should().BeOfType<LambdaAuthorizerIdentity>();
        var lambda = (LambdaAuthorizerIdentity)ctx.Identity!;
        lambda.ResolverContext!["tenantId"].Should().Be("t-1");

        ctx.Info.Should().NotBeNull();
        ctx.Info!.FieldName.Should().Be("getData");
        ctx.Info.SelectionSetList.Should().BeEquivalentTo(new[] { "id", "name" });

        ctx.Request.Should().NotBeNull();
        ctx.Request!.Headers!["authorization"].Should().Be("Bearer abc");
    }
}
