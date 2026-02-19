namespace Oproto.Lambda.GraphQL.Runtime;

/// <summary>
/// Identity model for AppSync APIs authenticated with an OpenID Connect (OIDC) provider.
/// Contains the OIDC token subject and claims.
/// </summary>
public class OidcIdentity : AppSyncIdentity
{
    /// <summary>
    /// The OIDC subject identifier.
    /// </summary>
    public string? Sub { get; set; }

    /// <summary>
    /// The OIDC issuer URL.
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// Claims from the OIDC token.
    /// </summary>
    public Dictionary<string, string>? Claims { get; set; }
}
