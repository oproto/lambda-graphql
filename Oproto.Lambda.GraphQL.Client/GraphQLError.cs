using System.Text.Json.Serialization;

namespace Oproto.Lambda.GraphQL.Client;

public class GraphQLError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("errorType")]
    public string? ErrorType { get; set; }

    [JsonPropertyName("path")]
    public List<string>? Path { get; set; }

    [JsonPropertyName("locations")]
    public List<GraphQLErrorLocation>? Locations { get; set; }
}
