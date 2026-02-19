using System;

namespace Oproto.Lambda.GraphQL.Attributes;

/// <summary>
/// Applies AWS authentication directives to GraphQL types or fields.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = true)]
public sealed class GraphQLAuthDirectiveAttribute : Attribute
{
    public GraphQLAuthDirectiveAttribute(AuthMode authMode)
    {
        AuthMode = authMode;
    }

    public AuthMode AuthMode { get; }
    public string? CognitoGroups { get; set; }
    public string? IamResource { get; set; }
}

/// <summary>
/// AWS AppSync authentication modes.
/// </summary>
public enum AuthMode
{
    ApiKey,
    UserPools,
    IAM,
    OpenIDConnect,
    Lambda
}
