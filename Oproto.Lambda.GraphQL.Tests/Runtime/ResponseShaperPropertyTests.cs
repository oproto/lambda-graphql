using System.Text.Json;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Oproto.Lambda.GraphQL.Runtime;
using Oproto.Lambda.GraphQL.Tests.Runtime.Generators;

namespace Oproto.Lambda.GraphQL.Tests.Runtime;

public class ResponseShaperPropertyTests
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Feature: field-selection-abstraction, Property 9: ShapeResponse with All() returns full serialized JSON
    // For any non-null test object, ShapeResponse(value, All(), options) equals JsonSerializer.Serialize(value, options).
    // Validates: Requirements 7.3
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(FieldSelectionArbitraries) })]
    public void ShapeResponse_WithAll_ReturnsFullSerializedJson(ShaperTestObject value)
    {
        var expected = JsonSerializer.Serialize(value, CamelCaseOptions);

        var result = ResponseShaper.ShapeResponse(value, FieldSelection.All(), CamelCaseOptions);

        result.Should().Be(expected);
    }

    // Feature: field-selection-abstraction, Property 10: ShapeResponse filters to only selected fields
    // For any test object and any subset of property names, shaped JSON contains only properties
    // corresponding to the selection.
    // Validates: Requirements 7.5, 9.1
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(FieldSelectionArbitraries) })]
    public Property ShapeResponse_FiltersToOnlySelectedFields(ShaperTestObject value)
    {
        // Generate a non-empty subset of property names
        var subsetGen = Gen.SubListOf(ShaperTestObject.PropertyNames)
            .Where(subset => subset.Count > 0)
            .Select(subset => subset.ToArray());

        return Prop.ForAll(subsetGen.ToArbitrary(), selectedProps =>
        {
            var selection = FieldSelection.Of(selectedProps);

            var result = ResponseShaper.ShapeResponse(value, selection, CamelCaseOptions);

            using var doc = JsonDocument.Parse(result);
            var root = doc.RootElement;

            // Every property in the output should correspond to a selected C# property
            var outputProps = new HashSet<string>();
            foreach (var prop in root.EnumerateObject())
                outputProps.Add(prop.Name);

            // Map selected C# names to expected camelCase JSON names
            var expectedJsonNames = selectedProps
                .Select(p => CamelCaseOptions.PropertyNamingPolicy!.ConvertName(p))
                .ToHashSet();

            outputProps.Should().BeEquivalentTo(expectedJsonNames);
        });
    }
}
