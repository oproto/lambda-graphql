namespace Oproto.Lambda.GraphQL.Runtime;

/// <summary>
/// Identity model for AppSync APIs authenticated with AWS IAM.
/// Contains the IAM principal details and optional Cognito federated identity information.
/// </summary>
public class IamIdentity : AppSyncIdentity
{
    /// <summary>
    /// The AWS account ID of the caller.
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>
    /// The Cognito identity pool ID when using federated identities.
    /// </summary>
    public string? CognitoIdentityPoolId { get; set; }

    /// <summary>
    /// The Cognito identity ID when using federated identities.
    /// </summary>
    public string? CognitoIdentityId { get; set; }

    /// <summary>
    /// The source IP addresses of the caller.
    /// </summary>
    public List<string>? SourceIp { get; set; }

    /// <summary>
    /// The IAM username or role session name.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// The full IAM ARN of the caller.
    /// </summary>
    public string? UserArn { get; set; }
}
