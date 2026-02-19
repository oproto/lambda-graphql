namespace Oproto.Lambda.GraphQL.Runtime;

/// <summary>
/// Identity model for AppSync APIs authenticated with a Lambda authorizer.
/// Contains the key-value pairs returned by the authorizer function's resolver context.
/// </summary>
public class LambdaAuthorizerIdentity : AppSyncIdentity
{
    /// <summary>
    /// Key-value pairs from the Lambda authorizer response's resolver context.
    /// </summary>
    public Dictionary<string, string>? ResolverContext { get; set; }
}
