using System;
using System.Collections.Generic;

namespace Oproto.Lambda.GraphQL.SourceGenerator;

/// <summary>
/// Maps C# types to AWS AppSync scalar types.
/// </summary>
public static class AwsScalarMapper
{
    private static readonly Dictionary<string, string> TypeMappings = new()
    {
        // DateTime types
        { "System.DateTime", "AWSDateTime" },
        { "System.DateTimeOffset", "AWSDateTime" },
        { "System.DateOnly", "AWSDate" },
        { "System.TimeOnly", "AWSTime" },
        
        // ID types
        { "System.Guid", "ID" },
        
        // JSON types
        { "System.Text.Json.JsonElement", "AWSJSON" },
        { "Newtonsoft.Json.Linq.JObject", "AWSJSON" },
        { "Newtonsoft.Json.Linq.JToken", "AWSJSON" },
        
        // Email and URL types
        { "System.Net.Mail.MailAddress", "AWSEmail" },
        { "System.Uri", "AWSURL" },
        
        // IP Address types
        { "System.Net.IPAddress", "AWSIPAddress" }
        
        // Note: Int64/long types are NOT automatically mapped to AWSTimestamp
        // Use [GraphQLTimestamp] attribute to explicitly mark timestamp fields
    };

    /// <summary>
    /// Gets the AWS scalar type for a given C# type name.
    /// </summary>
    /// <param name="csharpTypeName">The full C# type name</param>
    /// <returns>The corresponding AWS scalar type, or null if no mapping exists</returns>
    public static string? GetAwsScalarType(string csharpTypeName)
    {
        return TypeMappings.TryGetValue(csharpTypeName, out var awsType) ? awsType : null;
    }

    /// <summary>
    /// Checks if a C# type should be mapped to an AWS scalar.
    /// </summary>
    /// <param name="csharpTypeName">The full C# type name</param>
    /// <returns>True if the type has an AWS scalar mapping</returns>
    public static bool HasAwsScalarMapping(string csharpTypeName)
    {
        return TypeMappings.ContainsKey(csharpTypeName);
    }

    /// <summary>
    /// Gets all supported AWS scalar types.
    /// </summary>
    /// <returns>Collection of AWS scalar type names</returns>
    public static IEnumerable<string> GetSupportedAwsScalars()
    {
        return new[]
        {
            "AWSDate",
            "AWSTime", 
            "AWSDateTime",
            "AWSTimestamp",
            "AWSEmail",
            "AWSJSON",
            "AWSPhone",
            "AWSURL",
            "AWSIPAddress"
        };
    }
}
