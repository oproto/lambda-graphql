using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Oproto.Lambda.GraphQL.Runtime;

/// <summary>
/// Convenience methods for deserializing <see cref="AppSyncResolverContext{TArguments}"/>
/// from JSON payloads.
/// </summary>
/// <remarks>
/// <para>
/// <b>Migration from arguments-only to full-context:</b>
/// </para>
/// <para>
/// Previously, Lambda functions received only the GraphQL arguments as the payload.
/// With the updated source generator, the JS resolver code now sends the full AppSync
/// context (arguments, source, identity, info, request, stash, prev). To migrate:
/// </para>
/// <list type="number">
///   <item>Change your function parameter type from <c>TArguments</c> to
///   <c>AppSyncResolverContext&lt;TArguments&gt;</c>.</item>
///   <item>Access arguments via <c>context.Arguments</c> instead of using the parameter directly.</item>
///   <item>Optionally access identity, info, request headers, and other context properties.</item>
/// </list>
/// <para>
/// For AOT scenarios, create a <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>
/// with <c>[JsonSerializable(typeof(AppSyncResolverContext&lt;YourArgs&gt;))]</c> and use the
/// <see cref="Deserialize{TArguments}(string, JsonTypeInfo{AppSyncResolverContext{TArguments}})"/> overload.
/// </para>
/// <para>
/// If Lambda Annotations does not support single complex parameter deserialization, use the
/// <see cref="Deserialize{TArguments}(Stream, JsonSerializerOptions?)"/> overload to deserialize
/// from the raw request stream.
/// </para>
/// </remarks>
public static class AppSyncResolverContextSerializer
{
    private static JsonSerializerOptions? _defaultOptions;

    /// <summary>
    /// Pre-configured <see cref="JsonSerializerOptions"/> with camelCase property naming
    /// and the <see cref="AppSyncIdentityConverter"/> registered.
    /// </summary>
    public static JsonSerializerOptions DefaultOptions => _defaultOptions ??= CreateDefaultOptions();

    /// <summary>
    /// Deserializes an <see cref="AppSyncResolverContext{TArguments}"/> from a JSON string.
    /// </summary>
    /// <typeparam name="TArguments">The type to deserialize the GraphQL arguments into.</typeparam>
    /// <param name="json">The JSON string representing the full AppSync resolver context.</param>
    /// <param name="options">
    /// Optional <see cref="JsonSerializerOptions"/>. When null, <see cref="DefaultOptions"/> is used.
    /// </param>
    /// <returns>The deserialized context, or null if the JSON represents a null value.</returns>
    public static AppSyncResolverContext<TArguments>? Deserialize<TArguments>(
        string json, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Deserialize<AppSyncResolverContext<TArguments>>(json, options ?? DefaultOptions);
    }

    /// <summary>
    /// Deserializes an <see cref="AppSyncResolverContext{TArguments}"/> from a <see cref="Stream"/>.
    /// Use this overload as a fallback when Lambda Annotations does not support single complex
    /// parameter deserialization — read from the raw Lambda input stream instead.
    /// </summary>
    /// <typeparam name="TArguments">The type to deserialize the GraphQL arguments into.</typeparam>
    /// <param name="stream">The stream containing the JSON payload.</param>
    /// <param name="options">
    /// Optional <see cref="JsonSerializerOptions"/>. When null, <see cref="DefaultOptions"/> is used.
    /// </param>
    /// <returns>The deserialized context, or null if the JSON represents a null value.</returns>
    public static AppSyncResolverContext<TArguments>? Deserialize<TArguments>(
        Stream stream, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Deserialize<AppSyncResolverContext<TArguments>>(stream, options ?? DefaultOptions);
    }

    /// <summary>
    /// Deserializes an <see cref="AppSyncResolverContext{TArguments}"/> from a JSON string
    /// using a specific <see cref="JsonTypeInfo{T}"/> for full AOT compatibility.
    /// </summary>
    /// <typeparam name="TArguments">The type to deserialize the GraphQL arguments into.</typeparam>
    /// <param name="json">The JSON string representing the full AppSync resolver context.</param>
    /// <param name="jsonTypeInfo">
    /// The source-generated type info for <see cref="AppSyncResolverContext{TArguments}"/>.
    /// Obtain this from your custom <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>.
    /// </param>
    /// <returns>The deserialized context, or null if the JSON represents a null value.</returns>
    public static AppSyncResolverContext<TArguments>? Deserialize<TArguments>(
        string json, JsonTypeInfo<AppSyncResolverContext<TArguments>> jsonTypeInfo)
    {
        return JsonSerializer.Deserialize(json, jsonTypeInfo);
    }

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        options.Converters.Add(new AppSyncIdentityConverter());
        return options;
    }
}
