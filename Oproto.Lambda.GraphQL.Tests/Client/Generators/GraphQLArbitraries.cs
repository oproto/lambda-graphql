using FsCheck;

namespace Oproto.Lambda.GraphQL.Tests.Client.Generators;

/// <summary>
/// FsCheck Arbitrary instances for GraphQL client types.
/// </summary>
public static class GraphQLArbitraries
{
    /// <summary>
    /// Non-null, non-empty string generator that avoids problematic characters for JSON.
    /// </summary>
    private static Gen<string> SafeString =>
        from chars in Gen.ArrayOf(Gen.Elements(
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.".ToCharArray()))
        where chars.Length > 0
        select new string(chars);

    /// <summary>
    /// Generates a GraphQL operation string (query or mutation).
    /// </summary>
    private static Gen<string> GraphQLOperationString =>
        from fieldName in SafeString
        from opType in Gen.Elements("query", "mutation")
        select $"{opType} {{ {fieldName} {{ id }} }}";

    /// <summary>
    /// Generates a nullable variables dictionary (simulating JSON-serializable variables).
    /// </summary>
    private static Gen<Dictionary<string, string>?> NullableVariablesDict =>
        Gen.OneOf(
            Gen.Constant<Dictionary<string, string>?>(null),
            Gen.Choose(1, 4).SelectMany(count =>
                Gen.ArrayOf(count, SafeString.SelectMany(k => SafeString.Select(v => (k, v))))
                   .Select(pairs =>
                   {
                       var dict = new Dictionary<string, string>();
                       foreach (var (k, v) in pairs)
                           dict.TryAdd(k, v);
                       return (Dictionary<string, string>?)dict;
                   })));

    /// <summary>
    /// Generates a request body input tuple: (query, variables).
    /// Uses a tuple of public types so the test method signature stays public.
    /// </summary>
    public static Arbitrary<Tuple<string, Dictionary<string, string>?>> RequestBodyInputArb() =>
        Arb.From(
            from query in GraphQLOperationString
            from variables in NullableVariablesDict
            select Tuple.Create(query, variables));
}
