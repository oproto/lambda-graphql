using System.Text.Json;
using System.Text.Json.Serialization;

namespace Oproto.Lambda.GraphQL.Client.Serialization;

internal class GraphQLRequestBody
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("variables")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Variables { get; set; }
}

internal class AppSyncResponseEnvelope
{
    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<GraphQLError>? Errors { get; set; }
}

[JsonSerializable(typeof(GraphQLRequestBody))]
[JsonSerializable(typeof(AppSyncResponseEnvelope))]
[JsonSerializable(typeof(GraphQLError))]
[JsonSerializable(typeof(GraphQLErrorLocation))]
[JsonSerializable(typeof(List<GraphQLError>))]
[JsonSerializable(typeof(JsonElement))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class AppSyncClientJsonContext : JsonSerializerContext
{
}
