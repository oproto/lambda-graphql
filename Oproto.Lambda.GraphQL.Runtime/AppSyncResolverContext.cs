using System.Text.Json;

namespace Oproto.Lambda.GraphQL.Runtime;

/// <summary>
/// Typed model for the full AppSync resolver context payload.
/// Use this as your Lambda function parameter type to receive the complete
/// AppSync context instead of just the arguments.
/// </summary>
/// <typeparam name="TArguments">The type to deserialize the GraphQL arguments into.</typeparam>
public class AppSyncResolverContext<TArguments>
{
    /// <summary>
    /// The GraphQL arguments passed to the resolver field.
    /// </summary>
    public TArguments? Arguments { get; set; }

    /// <summary>
    /// The parent object for nested/field resolvers. Null for root Query/Mutation resolvers.
    /// </summary>
    public JsonElement? Source { get; set; }

    /// <summary>
    /// The caller identity based on the AppSync authentication mode.
    /// The concrete type depends on the auth mode (Cognito, IAM, OIDC, or Lambda authorizer).
    /// </summary>
    public AppSyncIdentity? Identity { get; set; }

    /// <summary>
    /// GraphQL field info including the field name, parent type, and selection sets.
    /// </summary>
    public AppSyncInfo? Info { get; set; }

    /// <summary>
    /// HTTP request metadata including client headers.
    /// </summary>
    public AppSyncRequest? Request { get; set; }

    /// <summary>
    /// Pipeline resolver stash data. Shared across pipeline functions.
    /// </summary>
    public JsonElement? Stash { get; set; }

    /// <summary>
    /// The result from the previous pipeline function.
    /// </summary>
    public JsonElement? Prev { get; set; }
}
