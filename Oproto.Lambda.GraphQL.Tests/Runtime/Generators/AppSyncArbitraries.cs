using System.Text.Json;
using FsCheck;
using Oproto.Lambda.GraphQL.Runtime;

namespace Oproto.Lambda.GraphQL.Tests.Runtime.Generators;

/// <summary>
/// FsCheck Arbitrary instances for AppSync runtime types.
/// </summary>
public static class AppSyncArbitraries
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
    /// Generates nullable strings (either null or a safe string).
    /// </summary>
    private static Gen<string?> NullableSafeString =>
        Gen.OneOf(Gen.Constant<string?>(null), SafeString.Select(s => (string?)s));

    /// <summary>
    /// Generates nullable dictionaries of string to string.
    /// </summary>
    private static Gen<Dictionary<string, string>?> NullableStringDict =>
        Gen.OneOf(
            Gen.Constant<Dictionary<string, string>?>(null),
            Gen.Choose(0, 5).SelectMany(count =>
                Gen.ArrayOf(count, SafeString.SelectMany(k => SafeString.Select(v => (k, v))))
                   .Select(pairs =>
                   {
                       var dict = new Dictionary<string, string>();
                       foreach (var (k, v) in pairs)
                           dict.TryAdd(k, v);
                       return (Dictionary<string, string>?)dict;
                   })));

    /// <summary>
    /// Generates nullable lists of strings.
    /// </summary>
    private static Gen<List<string>?> NullableStringList =>
        Gen.OneOf(
            Gen.Constant<List<string>?>(null),
            Gen.Choose(0, 5).SelectMany(count =>
                Gen.ArrayOf(count, SafeString).Select(arr => (List<string>?)arr.ToList())));

    /// <summary>
    /// Generates selection set lists including nested paths like "category/name".
    /// </summary>
    private static Gen<List<string>?> SelectionSetListGen =>
        Gen.OneOf(
            Gen.Constant<List<string>?>(null),
            Gen.Choose(1, 6).SelectMany(count =>
                Gen.ArrayOf(count, Gen.OneOf(
                    SafeString,
                    SafeString.SelectMany(parent =>
                        SafeString.Select(child => $"{parent}/{child}"))
                )).Select(arr => (List<string>?)arr.ToList())));

    public static Arbitrary<CognitoUserPoolsIdentity> CognitoUserPoolsIdentityArb() =>
        Arb.From(
            from sub in NullableSafeString
            from issuer in NullableSafeString
            from username in NullableSafeString
            from claims in NullableStringDict
            from defaultAuth in Gen.OneOf(
                Gen.Constant<string?>(null),
                Gen.Elements<string?>("ALLOW", "DENY"))
            from groups in NullableStringList
            // Ensure at least one discriminating property is present
            let hasDiscriminator = defaultAuth != null || groups != null
            let finalAuth = hasDiscriminator ? defaultAuth : "ALLOW"
            select new CognitoUserPoolsIdentity
            {
                Sub = sub,
                Issuer = issuer,
                Username = username,
                Claims = claims,
                DefaultAuthStrategy = finalAuth,
                Groups = groups
            });

    public static Arbitrary<IamIdentity> IamIdentityArb() =>
        Arb.From(
            from accountId in NullableSafeString
            from poolId in NullableSafeString
            from identityId in NullableSafeString
            from sourceIp in NullableStringList
            from username in NullableSafeString
            from userArn in NullableSafeString
            // Ensure at least one discriminating property is present
            let hasDiscriminator = poolId != null || userArn != null
            let finalArn = hasDiscriminator ? userArn : "arn:aws:iam::123456789012:user/test"
            select new IamIdentity
            {
                AccountId = accountId,
                CognitoIdentityPoolId = poolId,
                CognitoIdentityId = identityId,
                SourceIp = sourceIp,
                Username = username,
                UserArn = finalArn
            });

    public static Arbitrary<OidcIdentity> OidcIdentityArb() =>
        Arb.From(
            from sub in SafeString
            from issuer in SafeString
            from claims in NullableStringDict
            select new OidcIdentity
            {
                Sub = sub,
                Issuer = issuer,
                Claims = claims
            });

    public static Arbitrary<LambdaAuthorizerIdentity> LambdaAuthorizerIdentityArb() =>
        Arb.From(
            from ctx in NullableStringDict
            // Ensure resolverContext is present for discrimination
            let finalCtx = ctx ?? new Dictionary<string, string> { ["key"] = "value" }
            select new LambdaAuthorizerIdentity
            {
                ResolverContext = finalCtx
            });

    public static Arbitrary<AppSyncIdentity> AppSyncIdentityArb() =>
        Arb.From(Gen.OneOf(
            CognitoUserPoolsIdentityArb().Generator.Select(x => (AppSyncIdentity)x),
            IamIdentityArb().Generator.Select(x => (AppSyncIdentity)x),
            OidcIdentityArb().Generator.Select(x => (AppSyncIdentity)x),
            LambdaAuthorizerIdentityArb().Generator.Select(x => (AppSyncIdentity)x)));

    public static Arbitrary<AppSyncInfo> AppSyncInfoArb() =>
        Arb.From(
            from fieldName in NullableSafeString
            from parentTypeName in Gen.OneOf(
                Gen.Constant<string?>(null),
                Gen.Elements<string?>("Query", "Mutation", "Subscription"),
                SafeString.Select(s => (string?)s))
            from selectionSetList in SelectionSetListGen
            from selectionSetGraphQL in NullableSafeString
            select new AppSyncInfo
            {
                FieldName = fieldName,
                ParentTypeName = parentTypeName,
                SelectionSetList = selectionSetList,
                SelectionSetGraphQL = selectionSetGraphQL
            });

    public static Arbitrary<AppSyncRequest> AppSyncRequestArb() =>
        Arb.From(
            from headers in NullableStringDict
            select new AppSyncRequest { Headers = headers });

    /// <summary>
    /// Generates a nullable JsonElement from a random JSON object.
    /// </summary>
    private static Gen<JsonElement?> NullableJsonElementGen =>
        Gen.OneOf(
            Gen.Constant<JsonElement?>(null),
            NullableStringDict.Select(dict =>
            {
                var obj = dict ?? new Dictionary<string, string>();
                var json = JsonSerializer.Serialize(obj);
                return (JsonElement?)JsonDocument.Parse(json).RootElement.Clone();
            }));

    public static Arbitrary<AppSyncResolverContext<JsonElement>> ContextArb() =>
        Arb.From(
            from args in NullableStringDict.Select(dict =>
            {
                var obj = dict ?? new Dictionary<string, string>();
                var json = JsonSerializer.Serialize(obj);
                return JsonDocument.Parse(json).RootElement.Clone();
            })
            from source in NullableJsonElementGen
            from identity in Gen.OneOf(
                Gen.Constant<AppSyncIdentity?>(null),
                AppSyncIdentityArb().Generator.Select(x => (AppSyncIdentity?)x))
            from info in Gen.OneOf(
                Gen.Constant<AppSyncInfo?>(null),
                AppSyncInfoArb().Generator.Select(x => (AppSyncInfo?)x))
            from request in Gen.OneOf(
                Gen.Constant<AppSyncRequest?>(null),
                AppSyncRequestArb().Generator.Select(x => (AppSyncRequest?)x))
            from stash in NullableJsonElementGen
            from prev in NullableJsonElementGen
            select new AppSyncResolverContext<JsonElement>
            {
                Arguments = args,
                Source = source,
                Identity = identity,
                Info = info,
                Request = request,
                Stash = stash,
                Prev = prev
            });

    /// <summary>
    /// Registers all AppSync arbitraries with FsCheck.
    /// </summary>
    public static Arbitrary<T> For<T>()
    {
        if (typeof(T) == typeof(AppSyncResolverContext<JsonElement>))
            return (Arbitrary<T>)(object)ContextArb();
        if (typeof(T) == typeof(CognitoUserPoolsIdentity))
            return (Arbitrary<T>)(object)CognitoUserPoolsIdentityArb();
        if (typeof(T) == typeof(IamIdentity))
            return (Arbitrary<T>)(object)IamIdentityArb();
        if (typeof(T) == typeof(OidcIdentity))
            return (Arbitrary<T>)(object)OidcIdentityArb();
        if (typeof(T) == typeof(LambdaAuthorizerIdentity))
            return (Arbitrary<T>)(object)LambdaAuthorizerIdentityArb();
        if (typeof(T) == typeof(AppSyncIdentity))
            return (Arbitrary<T>)(object)AppSyncIdentityArb();
        if (typeof(T) == typeof(AppSyncInfo))
            return (Arbitrary<T>)(object)AppSyncInfoArb();
        if (typeof(T) == typeof(AppSyncRequest))
            return (Arbitrary<T>)(object)AppSyncRequestArb();
        throw new NotSupportedException($"No arbitrary registered for {typeof(T).Name}");
    }
}
