using System.Text.Json.Serialization;

namespace Oproto.Lambda.GraphQL.Client;

public class AppSyncResponse<TResult>
{
    [JsonPropertyName("data")]
    public TResult? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<GraphQLError>? Errors { get; set; }

    [JsonIgnore]
    public bool HasErrors => Errors is { Count: > 0 };

    [JsonIgnore]
    public bool IsSuccess => Data is not null && !HasErrors;

    [JsonIgnore]
    public int? StatusCode { get; set; }

    [JsonIgnore]
    public string? RawBody { get; set; }

    [JsonIgnore]
    public Exception? Exception { get; set; }
}
