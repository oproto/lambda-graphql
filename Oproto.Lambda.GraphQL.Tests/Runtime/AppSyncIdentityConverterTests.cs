using System.Text.Json;
using FluentAssertions;
using Oproto.Lambda.GraphQL.Runtime;

namespace Oproto.Lambda.GraphQL.Tests.Runtime;

public class AppSyncIdentityConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void Deserialize_CognitoUserPoolsIdentity_WithAllProperties()
    {
        var json = """
        {
            "sub": "d498a09e-7263-4e6f-b9a1-f5e3b8b2c1d4",
            "issuer": "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_ABC123",
            "username": "john.doe",
            "claims": {
                "email": "john@example.com",
                "email_verified": "true"
            },
            "defaultAuthStrategy": "ALLOW",
            "groups": ["admin", "editors"]
        }
        """;

        var identity = JsonSerializer.Deserialize<AppSyncIdentity>(json, Options);

        identity.Should().BeOfType<CognitoUserPoolsIdentity>();
        var cognito = (CognitoUserPoolsIdentity)identity!;
        cognito.Sub.Should().Be("d498a09e-7263-4e6f-b9a1-f5e3b8b2c1d4");
        cognito.Issuer.Should().Be("https://cognito-idp.us-east-1.amazonaws.com/us-east-1_ABC123");
        cognito.Username.Should().Be("john.doe");
        cognito.Claims.Should().ContainKey("email").WhoseValue.Should().Be("john@example.com");
        cognito.DefaultAuthStrategy.Should().Be("ALLOW");
        cognito.Groups.Should().BeEquivalentTo(new[] { "admin", "editors" });
    }

    [Fact]
    public void Deserialize_IamIdentity_WithAllProperties()
    {
        var json = """
        {
            "accountId": "123456789012",
            "cognitoIdentityPoolId": "us-east-1:abcdef01-2345-6789-abcd-ef0123456789",
            "cognitoIdentityId": "us-east-1:fedcba98-7654-3210-fedc-ba9876543210",
            "sourceIp": ["203.0.113.42", "198.51.100.7"],
            "username": "AIDA1234567890EXAMPLE:session-name",
            "userArn": "arn:aws:iam::123456789012:user/test-user"
        }
        """;

        var identity = JsonSerializer.Deserialize<AppSyncIdentity>(json, Options);

        identity.Should().BeOfType<IamIdentity>();
        var iam = (IamIdentity)identity!;
        iam.AccountId.Should().Be("123456789012");
        iam.CognitoIdentityPoolId.Should().Be("us-east-1:abcdef01-2345-6789-abcd-ef0123456789");
        iam.CognitoIdentityId.Should().Be("us-east-1:fedcba98-7654-3210-fedc-ba9876543210");
        iam.SourceIp.Should().BeEquivalentTo(new[] { "203.0.113.42", "198.51.100.7" });
        iam.Username.Should().Be("AIDA1234567890EXAMPLE:session-name");
        iam.UserArn.Should().Be("arn:aws:iam::123456789012:user/test-user");
    }

    [Fact]
    public void Deserialize_OidcIdentity_WithAllProperties()
    {
        var json = """
        {
            "sub": "oidc-subject-12345",
            "issuer": "https://accounts.google.com",
            "claims": {
                "aud": "my-app-client-id",
                "email": "user@gmail.com"
            }
        }
        """;

        var identity = JsonSerializer.Deserialize<AppSyncIdentity>(json, Options);

        identity.Should().BeOfType<OidcIdentity>();
        var oidc = (OidcIdentity)identity!;
        oidc.Sub.Should().Be("oidc-subject-12345");
        oidc.Issuer.Should().Be("https://accounts.google.com");
        oidc.Claims.Should().ContainKey("aud").WhoseValue.Should().Be("my-app-client-id");
        oidc.Claims.Should().ContainKey("email").WhoseValue.Should().Be("user@gmail.com");
    }

    [Fact]
    public void Deserialize_LambdaAuthorizerIdentity_WithResolverContext()
    {
        var json = """
        {
            "resolverContext": {
                "tenantId": "tenant-abc",
                "role": "admin",
                "customKey": "customValue"
            }
        }
        """;

        var identity = JsonSerializer.Deserialize<AppSyncIdentity>(json, Options);

        identity.Should().BeOfType<LambdaAuthorizerIdentity>();
        var lambda = (LambdaAuthorizerIdentity)identity!;
        lambda.ResolverContext.Should().HaveCount(3);
        lambda.ResolverContext!["tenantId"].Should().Be("tenant-abc");
        lambda.ResolverContext["role"].Should().Be("admin");
        lambda.ResolverContext["customKey"].Should().Be("customValue");
    }

    [Fact]
    public void Deserialize_NullIdentity_ReturnsNull()
    {
        var json = "null";

        var identity = JsonSerializer.Deserialize<AppSyncIdentity>(json, Options);

        identity.Should().BeNull();
    }

    [Fact]
    public void Deserialize_EmptyObject_ReturnsBaseAppSyncIdentity()
    {
        var json = "{}";

        var identity = JsonSerializer.Deserialize<AppSyncIdentity>(json, Options);

        identity.Should().NotBeNull();
        identity.Should().BeOfType<AppSyncIdentity>();
    }

    [Fact]
    public void Deserialize_UnknownProperties_ReturnsBaseAppSyncIdentity()
    {
        var json = """
        {
            "someUnknownField": "value",
            "anotherField": 42
        }
        """;

        var identity = JsonSerializer.Deserialize<AppSyncIdentity>(json, Options);

        identity.Should().NotBeNull();
        identity.Should().BeOfType<AppSyncIdentity>();
    }

    [Fact]
    public void Deserialize_IamIdentity_WithOnlyUserArn()
    {
        var json = """
        {
            "userArn": "arn:aws:iam::111122223333:role/MyLambdaRole"
        }
        """;

        var identity = JsonSerializer.Deserialize<AppSyncIdentity>(json, Options);

        identity.Should().BeOfType<IamIdentity>();
        var iam = (IamIdentity)identity!;
        iam.UserArn.Should().Be("arn:aws:iam::111122223333:role/MyLambdaRole");
        iam.CognitoIdentityPoolId.Should().BeNull();
        iam.AccountId.Should().BeNull();
    }

    [Fact]
    public void Deserialize_CognitoUserPoolsIdentity_WithOnlyGroups()
    {
        var json = """
        {
            "groups": ["viewers", "contributors"]
        }
        """;

        var identity = JsonSerializer.Deserialize<AppSyncIdentity>(json, Options);

        identity.Should().BeOfType<CognitoUserPoolsIdentity>();
        var cognito = (CognitoUserPoolsIdentity)identity!;
        cognito.Groups.Should().BeEquivalentTo(new[] { "viewers", "contributors" });
        cognito.DefaultAuthStrategy.Should().BeNull();
        cognito.Sub.Should().BeNull();
    }
}
