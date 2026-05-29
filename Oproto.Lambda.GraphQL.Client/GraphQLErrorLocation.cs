using System.Text.Json.Serialization;

namespace Oproto.Lambda.GraphQL.Client;

public class GraphQLErrorLocation
{
    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("column")]
    public int Column { get; set; }
}
