using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Oproto.Lambda.GraphQL.Runtime;
using Oproto.Lambda.GraphQL.Tests.Runtime.Generators;

namespace Oproto.Lambda.GraphQL.Tests.Runtime;

public class FieldSelectionPropertyTests
{
    // Feature: field-selection-abstraction, Property 1: FromSelectionSet parses top-level field names correctly with optional mapping
    // For any list of paths and any map, Fields contains exactly the unique mapped top-level segments.
    // Validates: Requirements 1.3, 4.3
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(FieldSelectionArbitraries) })]
    public Property FromSelectionSet_ParsesTopLevelFieldNames_WithOptionalMapping(FieldNameMap map)
    {
        return Prop.ForAll(FieldSelectionArbitraries.SelectionSetList.ToArbitrary(), paths =>
        {
            var selection = FieldSelection.FromSelectionSet(paths, map);

            // Compute expected top-level fields: map the first segment of each path
            var expectedFields = paths
                .Select(p =>
                {
                    var slashIndex = p.IndexOf('/');
                    var firstSegment = slashIndex < 0 ? p : p.Substring(0, slashIndex);
                    return map.MapName(firstSegment);
                })
                .Distinct()
                .ToHashSet();

            selection.Fields.Should().BeEquivalentTo(expectedFields);
            selection.IsAll.Should().BeFalse();
        });
    }

    // Feature: field-selection-abstraction, Property 2: Of() creates a FieldSelection containing exactly the given fields
    // For any array of distinct strings, Of(fields).Fields contains exactly those strings with correct count.
    // Validates: Requirements 1.4
    [Property(MaxTest = 100)]
    public Property Of_CreatesFieldSelectionWithExactFields()
    {
        var distinctFieldsGen = Gen.Choose(1, 15)
            .SelectMany(count => Gen.ArrayOf(count, FieldSelectionArbitraries.SafeString))
            .Select(arr => arr.Distinct().ToArray())
            .Where(arr => arr.Length > 0);

        return Prop.ForAll(distinctFieldsGen.ToArbitrary(), fields =>
        {
            var selection = FieldSelection.Of(fields);

            selection.Fields.Should().HaveCount(fields.Length);
            selection.Fields.Should().BeEquivalentTo(fields);
            selection.IsAll.Should().BeFalse();
        });
    }

    // Feature: field-selection-abstraction, Property 3: IsRequested correctness
    // For any selection and name, IsRequested(name) is true iff IsAll or name in Fields.
    // Validates: Requirements 2.1, 2.5
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(FieldSelectionArbitraries) })]
    public Property IsRequested_CorrectForAnySelection(FieldSelection selection)
    {
        return Prop.ForAll(FieldSelectionArbitraries.SafeString.ToArbitrary(), name =>
        {
            var result = selection.IsRequested(name);
            var expected = selection.IsAll || selection.Fields.Contains(name);

            result.Should().Be(expected);
        });
    }

    // Feature: field-selection-abstraction, Property 4: ForNestedType multi-level extraction
    // For paths of depth N, chaining ForNestedType N-1 times yields the leaf field.
    // Validates: Requirements 2.2, 8.3
    [Property(MaxTest = 100)]
    public Property ForNestedType_MultiLevelExtraction()
    {
        // Generate paths with 2-4 segments to test multi-level nesting
        var multiLevelPathGen = Gen.Choose(2, 4)
            .SelectMany(depth => Gen.ArrayOf(depth, FieldSelectionArbitraries.SafeString))
            .Where(segments => segments.All(s => s.Length > 0));

        return Prop.ForAll(multiLevelPathGen.ToArbitrary(), segments =>
        {
            var path = string.Join("/", segments);
            var selection = FieldSelection.FromSelectionSet(new List<string> { path });

            // Chain ForNestedType for each level except the last
            var current = selection;
            for (var i = 0; i < segments.Length - 1; i++)
            {
                current = current.ForNestedType(segments[i]);
            }

            // The leaf field should be in the final selection
            current.Fields.Should().Contain(segments[segments.Length - 1]);
        });
    }

    // Feature: field-selection-abstraction, Property 8: MapWith(Identity) is identity
    // For any non-All selection, MapWith(Identity).Fields equals original Fields.
    // Validates: Requirements 3.7, 10.6
    [Property(MaxTest = 100)]
    public Property MapWith_Identity_IsIdentity()
    {
        // Generate non-All selections only
        var nonAllSelectionGen = Gen.Choose(1, 10)
            .SelectMany(count => Gen.ArrayOf(count, FieldSelectionArbitraries.SafeString))
            .Select(fields => FieldSelection.Of(fields.Distinct().ToArray()));

        return Prop.ForAll(nonAllSelectionGen.ToArbitrary(), selection =>
        {
            var mapped = selection.MapWith(FieldNameMap.Identity);

            mapped.Fields.Should().BeEquivalentTo(selection.Fields);
            mapped.IsAll.Should().Be(selection.IsAll);
        });
    }
}
