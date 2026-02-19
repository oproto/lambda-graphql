namespace Oproto.Lambda.GraphQL.Runtime;

/// <summary>
/// HTTP request metadata from the AppSync context.
/// </summary>
public class AppSyncRequest
{
    /// <summary>
    /// HTTP request headers from the client.
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }
}
