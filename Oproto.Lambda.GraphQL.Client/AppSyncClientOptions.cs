using System.Text.Json.Serialization;

namespace Oproto.Lambda.GraphQL.Client;

public class AppSyncClientOptions
{
    public required string Endpoint { get; set; }
    public AuthMode AuthMode { get; set; } = AuthMode.Iam;
    public string? ApiKey { get; set; }
    public string? Region { get; set; }
    public int MaxRetries { get; set; } = 3;
    public HttpClient? HttpClient { get; set; }
    public JsonSerializerContext? JsonSerializerContext { get; set; }
}
