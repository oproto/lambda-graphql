using System.Text.Json;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Oproto.Lambda.GraphQL.Runtime;
using Oproto.Lambda.GraphQL.Tests.Runtime.Generators;

namespace Oproto.Lambda.GraphQL.Tests.Runtime;

public class AppSyncResolverContextPropertyTests
{
    private static readonly JsonSerializerOptions Options = AppSyncResolverContextSerializer.DefaultOptions;

    // Feature: runtime-core-appsync-context, Property 1: Context model round-trip serialization
    // For any valid AppSyncResolverContext<TArguments>, serialize then deserialize SHALL produce equivalent object
    // **Validates: Requirements 2.2, 2.3, 2.4, 2.5, 2.7, 4.2, 4.3**
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AppSyncArbitraries) })]
    public void RoundTrip_SerializeDeserialize_ProducesEquivalentObject(AppSyncResolverContext<JsonElement> context)
    {
        var json = JsonSerializer.Serialize(context, Options);
        var deserialized = JsonSerializer.Deserialize<AppSyncResolverContext<JsonElement>>(json, Options);

        deserialized.Should().NotBeNull();

        // Arguments round-trip
        deserialized!.Arguments.GetRawText().Should().Be(context.Arguments.GetRawText());

        // Source round-trip
        AssertJsonElementEqual(deserialized.Source, context.Source);

        // Identity round-trip
        if (context.Identity == null)
        {
            deserialized.Identity.Should().BeNull();
        }
        else
        {
            deserialized.Identity.Should().NotBeNull();
            deserialized.Identity!.GetType().Should().Be(context.Identity.GetType());
            AssertIdentityEqual(deserialized.Identity, context.Identity);
        }

        // Info round-trip
        if (context.Info == null)
        {
            deserialized.Info.Should().BeNull();
        }
        else
        {
            deserialized.Info.Should().NotBeNull();
            deserialized.Info!.FieldName.Should().Be(context.Info.FieldName);
            deserialized.Info.ParentTypeName.Should().Be(context.Info.ParentTypeName);
            deserialized.Info.SelectionSetGraphQL.Should().Be(context.Info.SelectionSetGraphQL);
            if (context.Info.SelectionSetList == null)
                deserialized.Info.SelectionSetList.Should().BeNull();
            else
                deserialized.Info.SelectionSetList.Should().BeEquivalentTo(context.Info.SelectionSetList);
        }

        // Request round-trip
        if (context.Request == null)
        {
            deserialized.Request.Should().BeNull();
        }
        else
        {
            deserialized.Request.Should().NotBeNull();
            if (context.Request.Headers == null)
                deserialized.Request!.Headers.Should().BeNull();
            else
                deserialized.Request!.Headers.Should().BeEquivalentTo(context.Request.Headers);
        }

        // Stash and Prev round-trip
        AssertJsonElementEqual(deserialized.Stash, context.Stash);
        AssertJsonElementEqual(deserialized.Prev, context.Prev);
    }

    // Feature: runtime-core-appsync-context, Property 2: Missing optional properties default to null
    // For any subset of optional properties omitted from JSON, deserialization SHALL produce null for omitted properties
    // **Validates: Requirements 2.6**
    [Property(MaxTest = 100)]
    public void MissingOptionalProperties_DefaultToNull(
        bool includeSource, bool includeIdentity, bool includeInfo,
        bool includeRequest, bool includeStash, bool includePrev)
    {
        // Build a JSON object with only arguments and the selected optional properties
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();

        writer.WritePropertyName("arguments");
        writer.WriteStartObject();
        writer.WriteString("id", "test-123");
        writer.WriteEndObject();

        if (includeSource)
        {
            writer.WritePropertyName("source");
            writer.WriteStartObject();
            writer.WriteString("parentField", "value");
            writer.WriteEndObject();
        }

        if (includeIdentity)
        {
            writer.WritePropertyName("identity");
            writer.WriteStartObject();
            writer.WriteString("sub", "user-1");
            writer.WriteString("issuer", "https://example.com");
            writer.WriteEndObject();
        }

        if (includeInfo)
        {
            writer.WritePropertyName("info");
            writer.WriteStartObject();
            writer.WriteString("fieldName", "getItem");
            writer.WriteString("parentTypeName", "Query");
            writer.WriteEndObject();
        }

        if (includeRequest)
        {
            writer.WritePropertyName("request");
            writer.WriteStartObject();
            writer.WritePropertyName("headers");
            writer.WriteStartObject();
            writer.WriteString("authorization", "Bearer token");
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        if (includeStash)
        {
            writer.WritePropertyName("stash");
            writer.WriteStartObject();
            writer.WriteEndObject();
        }

        if (includePrev)
        {
            writer.WritePropertyName("prev");
            writer.WriteStartObject();
            writer.WriteString("result", "ok");
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.Flush();

        var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        var result = JsonSerializer.Deserialize<AppSyncResolverContext<JsonElement>>(json, Options);

        result.Should().NotBeNull();
        result!.Arguments.ValueKind.Should().Be(JsonValueKind.Object);

        if (!includeSource) result.Source.Should().BeNull(); else result.Source.Should().NotBeNull();
        if (!includeIdentity) result.Identity.Should().BeNull(); else result.Identity.Should().NotBeNull();
        if (!includeInfo) result.Info.Should().BeNull(); else result.Info.Should().NotBeNull();
        if (!includeRequest) result.Request.Should().BeNull(); else result.Request.Should().NotBeNull();
        if (!includeStash) result.Stash.Should().BeNull(); else result.Stash.Should().NotBeNull();
        if (!includePrev) result.Prev.Should().BeNull(); else result.Prev.Should().NotBeNull();
    }

    // Feature: runtime-core-appsync-context, Property 3: Identity polymorphic deserialization preserves concrete type
    // For any identity subtype, serialize then deserialize through AppSyncIdentityConverter SHALL preserve concrete type
    // **Validates: Requirements 3.6, 3.7, 3.8, 3.9**
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AppSyncArbitraries) })]
    public void IdentityRoundTrip_PreservesConcreteType(AppSyncIdentity identity)
    {
        var json = JsonSerializer.Serialize(identity, Options);
        var deserialized = JsonSerializer.Deserialize<AppSyncIdentity>(json, Options);

        deserialized.Should().NotBeNull();
        deserialized!.GetType().Should().Be(identity.GetType());
        AssertIdentityEqual(deserialized, identity);
    }

    // Feature: runtime-core-appsync-context, Property 4: Serialized JSON uses camelCase property names
    // For any valid AppSyncResolverContext<TArguments>, serialized JSON SHALL have all property names in camelCase
    // **Validates: Requirements 6.3**
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AppSyncArbitraries) })]
    public void Serialization_ProducesCamelCasePropertyNames(AppSyncResolverContext<JsonElement> context)
    {
        var json = JsonSerializer.Serialize(context, Options);
        using var doc = JsonDocument.Parse(json);

        AssertAllPropertiesCamelCase(doc.RootElement);
    }

    // Properties whose values are objects with user-controlled keys (not model properties)
    private static readonly HashSet<string> UserDataProperties = new()
    {
        "arguments", "source", "stash", "prev",
        "claims", "headers", "resolverContext"
    };

    private static void AssertAllPropertiesCamelCase(JsonElement element, bool insideDictionary = false)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;

        foreach (var property in element.EnumerateObject())
        {
            // Only check model-controlled property names, not user dictionary keys
            if (!insideDictionary && property.Name.Length > 0)
            {
                char.IsLower(property.Name[0]).Should().BeTrue(
                    $"property '{property.Name}' should start with a lowercase letter (camelCase)");
            }

            bool childIsUserData = UserDataProperties.Contains(property.Name);

            // Recurse into nested objects
            if (property.Value.ValueKind == JsonValueKind.Object)
                AssertAllPropertiesCamelCase(property.Value, childIsUserData);

            // Recurse into arrays
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in property.Value.EnumerateArray())
                    AssertAllPropertiesCamelCase(item);
            }
        }
    }

    private static void AssertJsonElementEqual(JsonElement? actual, JsonElement? expected)
    {
        if (expected == null)
        {
            actual.Should().BeNull();
            return;
        }

        actual.Should().NotBeNull();
        actual!.Value.GetRawText().Should().Be(expected.Value.GetRawText());
    }

    private static void AssertIdentityEqual(AppSyncIdentity actual, AppSyncIdentity expected)
    {
        switch (expected)
        {
            case CognitoUserPoolsIdentity expectedCognito:
                var actualCognito = (CognitoUserPoolsIdentity)actual;
                actualCognito.Sub.Should().Be(expectedCognito.Sub);
                actualCognito.Issuer.Should().Be(expectedCognito.Issuer);
                actualCognito.Username.Should().Be(expectedCognito.Username);
                actualCognito.DefaultAuthStrategy.Should().Be(expectedCognito.DefaultAuthStrategy);
                AssertDictEqual(actualCognito.Claims, expectedCognito.Claims);
                AssertListEqual(actualCognito.Groups, expectedCognito.Groups);
                break;

            case IamIdentity expectedIam:
                var actualIam = (IamIdentity)actual;
                actualIam.AccountId.Should().Be(expectedIam.AccountId);
                actualIam.CognitoIdentityPoolId.Should().Be(expectedIam.CognitoIdentityPoolId);
                actualIam.CognitoIdentityId.Should().Be(expectedIam.CognitoIdentityId);
                actualIam.Username.Should().Be(expectedIam.Username);
                actualIam.UserArn.Should().Be(expectedIam.UserArn);
                AssertListEqual(actualIam.SourceIp, expectedIam.SourceIp);
                break;

            case OidcIdentity expectedOidc:
                var actualOidc = (OidcIdentity)actual;
                actualOidc.Sub.Should().Be(expectedOidc.Sub);
                actualOidc.Issuer.Should().Be(expectedOidc.Issuer);
                AssertDictEqual(actualOidc.Claims, expectedOidc.Claims);
                break;

            case LambdaAuthorizerIdentity expectedLambda:
                var actualLambda = (LambdaAuthorizerIdentity)actual;
                AssertDictEqual(actualLambda.ResolverContext, expectedLambda.ResolverContext);
                break;
        }
    }

    private static void AssertDictEqual(Dictionary<string, string>? actual, Dictionary<string, string>? expected)
    {
        if (expected == null) { actual.Should().BeNull(); return; }
        actual.Should().BeEquivalentTo(expected);
    }

    private static void AssertListEqual(List<string>? actual, List<string>? expected)
    {
        if (expected == null) { actual.Should().BeNull(); return; }
        actual.Should().BeEquivalentTo(expected);
    }
}
