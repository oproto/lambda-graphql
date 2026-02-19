namespace Oproto.Lambda.GraphQL.Runtime;

/// <summary>
/// Identity model for AppSync APIs authenticated with Amazon Cognito User Pools.
/// Contains the JWT claims and group membership from the Cognito token.
/// </summary>
public class CognitoUserPoolsIdentity : AppSyncIdentity
{
    /// <summary>
    /// The Cognito user pool subject UUID.
    /// </summary>
    public string? Sub { get; set; }

    /// <summary>
    /// The token issuer URL (Cognito user pool endpoint).
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// The Cognito username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// JWT claims from the Cognito token.
    /// </summary>
    public Dictionary<string, string>? Claims { get; set; }

    /// <summary>
    /// The default authorization strategy (ALLOW or DENY).
    /// </summary>
    public string? DefaultAuthStrategy { get; set; }

    /// <summary>
    /// The Cognito user pool groups the user belongs to.
    /// </summary>
    public List<string>? Groups { get; set; }
}
