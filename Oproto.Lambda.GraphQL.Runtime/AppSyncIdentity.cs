using System.Text.Json.Serialization;

namespace Oproto.Lambda.GraphQL.Runtime;

/// <summary>
/// Base class for AppSync identity models. The concrete subtype depends on the
/// authentication mode configured for the AppSync API.
/// </summary>
[JsonConverter(typeof(AppSyncIdentityConverter))]
public class AppSyncIdentity
{
}
