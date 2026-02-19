using System.Text.Json;
using System.Text.Json.Serialization;

namespace Oproto.Lambda.GraphQL.Runtime;

/// <summary>
/// Custom JSON converter for <see cref="AppSyncIdentity"/> that performs polymorphic
/// deserialization by inspecting JSON property names to determine the correct identity subtype.
/// This approach is AOT-compatible as it uses no reflection.
/// </summary>
/// <remarks>
/// Discrimination priority order:
/// <list type="number">
///   <item><c>resolverContext</c> present → <see cref="LambdaAuthorizerIdentity"/></item>
///   <item><c>cognitoIdentityPoolId</c> or <c>userArn</c> present → <see cref="IamIdentity"/></item>
///   <item><c>defaultAuthStrategy</c> or <c>groups</c> present → <see cref="CognitoUserPoolsIdentity"/></item>
///   <item><c>sub</c> and <c>issuer</c> present (fallback) → <see cref="OidcIdentity"/></item>
///   <item>None match / empty object → base <see cref="AppSyncIdentity"/></item>
/// </list>
/// </remarks>
public sealed class AppSyncIdentityConverter : JsonConverter<AppSyncIdentity>
{
    /// <inheritdoc />
    public override AppSyncIdentity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            return new AppSyncIdentity();

        // Priority 1: resolverContext → LambdaAuthorizerIdentity
        if (root.TryGetProperty("resolverContext", out _))
            return Deserialize<LambdaAuthorizerIdentity>(root, options);

        // Priority 2: cognitoIdentityPoolId or userArn → IamIdentity
        if (root.TryGetProperty("cognitoIdentityPoolId", out _) || root.TryGetProperty("userArn", out _))
            return Deserialize<IamIdentity>(root, options);

        // Priority 3: defaultAuthStrategy or groups → CognitoUserPoolsIdentity
        if (root.TryGetProperty("defaultAuthStrategy", out _) || root.TryGetProperty("groups", out _))
            return Deserialize<CognitoUserPoolsIdentity>(root, options);

        // Priority 4: sub AND issuer → OidcIdentity
        if (root.TryGetProperty("sub", out _) && root.TryGetProperty("issuer", out _))
            return Deserialize<OidcIdentity>(root, options);

        // Priority 5: No match → base AppSyncIdentity
        return new AppSyncIdentity();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, AppSyncIdentity value, JsonSerializerOptions options)
    {
        // Serialize the concrete type directly. Since the subtypes don't have
        // [JsonConverter] on them, this won't recurse back into this converter.
        switch (value)
        {
            case LambdaAuthorizerIdentity lambda:
                JsonSerializer.Serialize(writer, lambda, options);
                break;
            case IamIdentity iam:
                JsonSerializer.Serialize(writer, iam, options);
                break;
            case CognitoUserPoolsIdentity cognito:
                JsonSerializer.Serialize(writer, cognito, options);
                break;
            case OidcIdentity oidc:
                JsonSerializer.Serialize(writer, oidc, options);
                break;
            default:
                writer.WriteStartObject();
                writer.WriteEndObject();
                break;
        }
    }

    private static T Deserialize<T>(JsonElement element, JsonSerializerOptions options) where T : AppSyncIdentity
    {
        // Deserializing as the concrete type T (not AppSyncIdentity) avoids infinite
        // recursion since this converter is only registered for the base type.
        return JsonSerializer.Deserialize<T>(element.GetRawText(), options)
               ?? throw new JsonException($"Failed to deserialize identity as {typeof(T).Name}");
    }
}
