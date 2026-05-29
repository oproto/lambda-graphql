using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Oproto.Lambda.GraphQL.Client.Serialization;
using Oproto.Lambda.GraphQL.Tests.Client.Generators;

namespace Oproto.Lambda.GraphQL.Tests.Client;

public class AppSyncResponsePropertyTests
{
    // Property 1: Request body round-trip consistency
    // For any valid GraphQLRequestBody, serialize then deserialize SHALL produce an equivalent object
    // **Validates: Requirements 3.7, 11.9**
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(GraphQLArbitraries) })]
    public Property RoundTrip_SerializeDeserialize_ProducesEquivalentRequestBody(
        Tuple<string, Dictionary<string, string>?> input)
    {
        var (query, variables) = input;

        // Build the request body. Variables is object? — in real usage, the client
        // serializes variables separately then embeds the JsonElement into the body.
        // We simulate that here: serialize variables to JsonElement first, then embed.
        JsonElement? variablesElement = variables != null
            ? JsonDocument.Parse(JsonSerializer.Serialize(variables)).RootElement.Clone()
            : null;

        var body = new GraphQLRequestBody
        {
            Query = query,
            Variables = variablesElement
        };

        var json = JsonSerializer.Serialize(body, AppSyncClientJsonContext.Default.GraphQLRequestBody);
        var deserialized = JsonSerializer.Deserialize(json, AppSyncClientJsonContext.Default.GraphQLRequestBody);

        return (deserialized != null).Label("deserialized should not be null")
            .And(() => (deserialized!.Query == body.Query).Label("Query should round-trip"))
            .And(() => AssertVariablesEquivalent(variablesElement, deserialized!.Variables, json));
    }

    private static Property AssertVariablesEquivalent(JsonElement? original, object? deserialized, string json)
    {
        if (original == null)
        {
            // When variables is null, the field is omitted from JSON, so deserialized should also be null
            return (deserialized == null)
                .Label($"Variables should be null when original is null. JSON: {json}");
        }

        if (deserialized == null)
            return false.Label("Deserialized variables should not be null when original is not null");

        var deserializedElement = (JsonElement)deserialized;
        var match = deserializedElement.GetRawText() == original.Value.GetRawText();
        return match.Label(
            $"Variables JSON should match. Expected: {original.Value.GetRawText()}, Actual: {deserializedElement.GetRawText()}");
    }
}
