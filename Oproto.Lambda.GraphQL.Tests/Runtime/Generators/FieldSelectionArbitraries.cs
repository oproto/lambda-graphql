using System.Text.Json.Serialization;
using FsCheck;
using Oproto.Lambda.GraphQL.Runtime;

namespace Oproto.Lambda.GraphQL.Tests.Runtime.Generators;

/// <summary>
/// FsCheck Arbitrary instances for FieldSelection and FieldNameMap types.
/// </summary>
public static class FieldSelectionArbitraries
{
    /// <summary>
    /// Non-null, non-empty string generator using safe alphanumeric characters.
    /// </summary>
    internal static Gen<string> SafeString =>
        from chars in Gen.ArrayOf(Gen.Elements(
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray()))
        where chars.Length > 0
        select new string(chars);

    /// <summary>
    /// Generates a FieldNameMap with 0-10 random string→string mappings, avoiding self-mappings.
    /// </summary>
    public static Arbitrary<FieldNameMap> FieldNameMapArb() =>
        Arb.From(
            Gen.Choose(0, 10).SelectMany(count =>
                Gen.ArrayOf(count,
                    SafeString.SelectMany(source =>
                        SafeString.Where(target => target != source)
                            .Select(target => (source, target))))
                .Select(pairs =>
                {
                    var builder = FieldNameMap.Builder();
                    foreach (var (s, t) in pairs)
                        builder.Map(s, t);
                    return builder.Build();
                })));

    /// <summary>
    /// Generates a slash-separated path with 1-3 nesting levels using safe strings.
    /// </summary>
    internal static Gen<string> SelectionPath =>
        Gen.Choose(1, 3).SelectMany(depth =>
            Gen.ArrayOf(depth, SafeString)
                .Select(segments => string.Join("/", segments)));

    /// <summary>
    /// Generates a selection set list (1-10 paths).
    /// </summary>
    internal static Gen<List<string>> SelectionSetList =>
        Gen.Choose(1, 10).SelectMany(count =>
            Gen.ArrayOf(count, SelectionPath)
                .Select(paths => paths.ToList()));

    /// <summary>
    /// Generates a FieldSelection created via Of() with 1-10 distinct field names.
    /// </summary>
    public static Arbitrary<FieldSelection> FieldSelectionArb() =>
        Arb.From(
            Gen.OneOf(
                // All() selection
                Gen.Constant(FieldSelection.All()),
                // Of() with distinct fields
                Gen.Choose(1, 10).SelectMany(count =>
                    Gen.ArrayOf(count, SafeString)
                        .Select(fields => FieldSelection.Of(fields.Distinct().ToArray()))),
                // FromSelectionSet with paths
                SelectionSetList.Select(list => FieldSelection.FromSelectionSet(list))));

    /// <summary>
    /// Generates a non-null ShaperTestObject with random property values.
    /// </summary>
    public static Arbitrary<ShaperTestObject> ShaperTestObjectArb() =>
        Arb.From(
            from id in SafeString
            from name in SafeString
            from count in Gen.Choose(0, 10000)
            from price in Gen.Choose(0, 99999).Select(p => (decimal)p / 100m)
            from active in Arb.Generate<bool>()
            select new ShaperTestObject
            {
                Id = id,
                Name = name,
                Count = count,
                Price = price,
                Active = active
            });
}

/// <summary>
/// Test record type for ResponseShaper property tests.
/// Has known properties with predictable serialization behavior.
/// </summary>
public record ShaperTestObject
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal Price { get; init; }
    public bool Active { get; init; }

    /// <summary>
    /// The C# property names of this type, for use in property tests.
    /// </summary>
    public static readonly string[] PropertyNames = { "Id", "Name", "Count", "Price", "Active" };
}
