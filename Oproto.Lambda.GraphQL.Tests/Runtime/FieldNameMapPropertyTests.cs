using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Oproto.Lambda.GraphQL.Runtime;
using Oproto.Lambda.GraphQL.Tests.Runtime.Generators;

namespace Oproto.Lambda.GraphQL.Tests.Runtime;

public class FieldNameMapPropertyTests
{
    // Feature: field-selection-abstraction, Property 5: FieldNameMap.MapName pass-through
    // For any map built from known entries and any name NOT in those entries,
    // MapName returns the name unchanged; for names IN the map, returns the mapped target.
    // Validates: Requirements 3.3
    [Property(MaxTest = 100)]
    public Property MapName_PassThrough_And_MappedNames()
    {
        // Generate known entries and a probe name, then verify behavior
        var entryGen = Gen.Choose(0, 5).SelectMany(count =>
            Gen.ArrayOf(count,
                FieldSelectionArbitraries.SafeString.SelectMany(s =>
                    FieldSelectionArbitraries.SafeString.Where(t => t != s)
                        .Select(t => (source: s, target: t)))));

        return Prop.ForAll(entryGen.ToArbitrary(), FieldSelectionArbitraries.SafeString.ToArbitrary(),
            (entries, probeName) =>
            {
                var builder = FieldNameMap.Builder();
                var expectedMappings = new Dictionary<string, string>();
                foreach (var (s, t) in entries)
                {
                    builder.Map(s, t);
                    expectedMappings[s] = t; // last wins
                }
                var map = builder.Build();

                var result = map.MapName(probeName);
                if (expectedMappings.TryGetValue(probeName, out var expected))
                    result.Should().Be(expected);
                else
                    result.Should().Be(probeName);
            });
    }

    // Feature: field-selection-abstraction, Property 6: FieldNameMap.Identity maps every name to itself
    // For any string, Identity.MapName(name) returns name.
    // Validates: Requirements 3.6
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(FieldSelectionArbitraries) })]
    public bool Identity_MapsEveryNameToItself(NonEmptyString name)
    {
        return FieldNameMap.Identity.MapName(name.Get) == name.Get;
    }

    // Feature: field-selection-abstraction, Property 7: FieldNameMap composition equals sequential mapping
    // For any two maps and any name, map1.Then(map2).MapName(s) equals map2.MapName(map1.MapName(s)).
    // Validates: Requirements 3.4, 10.10
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(FieldSelectionArbitraries) })]
    public Property Composition_Equals_SequentialMapping(FieldNameMap map1, FieldNameMap map2)
    {
        return Prop.ForAll(FieldSelectionArbitraries.SafeString.ToArbitrary(), name =>
        {
            var composed = map1.Then(map2);
            var composedResult = composed.MapName(name);
            var sequentialResult = map2.MapName(map1.MapName(name));
            composedResult.Should().Be(sequentialResult);
        });
    }
}
