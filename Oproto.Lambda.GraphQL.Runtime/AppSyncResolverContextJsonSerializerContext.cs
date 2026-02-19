using System.Text.Json;
using System.Text.Json.Serialization;

namespace Oproto.Lambda.GraphQL.Runtime;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for AOT-compatible serialization
/// of all AppSync resolver context model types.
/// </summary>
/// <remarks>
/// This context handles the built-in model types. For user-specific <c>TArguments</c> types,
/// create your own <see cref="JsonSerializerContext"/> with
/// <c>[JsonSerializable(typeof(AppSyncResolverContext&lt;MyArgs&gt;))]</c> and pass it
/// to <see cref="AppSyncResolverContextSerializer.Deserialize{TArguments}(string, System.Text.Json.Serialization.Metadata.JsonTypeInfo{AppSyncResolverContext{TArguments}})"/>.
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AppSyncResolverContext<JsonElement>))]
[JsonSerializable(typeof(AppSyncIdentity))]
[JsonSerializable(typeof(CognitoUserPoolsIdentity))]
[JsonSerializable(typeof(IamIdentity))]
[JsonSerializable(typeof(OidcIdentity))]
[JsonSerializable(typeof(LambdaAuthorizerIdentity))]
[JsonSerializable(typeof(AppSyncInfo))]
[JsonSerializable(typeof(AppSyncRequest))]
public partial class AppSyncResolverContextJsonSerializerContext : JsonSerializerContext
{
}
