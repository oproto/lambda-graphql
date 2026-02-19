using FluentAssertions;
using Oproto.Lambda.GraphQL.Runtime;

namespace Oproto.Lambda.GraphQL.Tests.Runtime;

public class FieldSelectionTests
{
    [Fact]
    public void All_ReturnsIsAllTrue_EmptyFields()
    {
        var selection = FieldSelection.All();

        selection.IsAll.Should().BeTrue();
        selection.Fields.Should().BeEmpty();
    }

    [Fact]
    public void Of_ReturnsCorrectFieldSet()
    {
        var selection = FieldSelection.Of("Id", "Name");

        selection.IsAll.Should().BeFalse();
        selection.Fields.Should().BeEquivalentTo(new[] { "Id", "Name" });
    }

    [Fact]
    public void Of_NoArguments_ReturnsEmptyNonAllSelection()
    {
        var selection = FieldSelection.Of();

        selection.IsAll.Should().BeFalse();
        selection.Fields.Should().BeEmpty();
    }

    [Fact]
    public void FromSelectionSet_WithNestedPaths_IncludesTopLevelFields()
    {
        var selection = FieldSelection.FromSelectionSet(
            new List<string> { "id", "category/name", "category/description" });

        selection.IsAll.Should().BeFalse();
        selection.Fields.Should().BeEquivalentTo(new[] { "id", "category" });
    }

    [Fact]
    public void ForNestedType_ExtractsSubSelection()
    {
        var selection = FieldSelection.FromSelectionSet(
            new List<string> { "id", "category/name", "category/description" });

        var nested = selection.ForNestedType("category");

        nested.IsAll.Should().BeFalse();
        nested.Fields.Should().BeEquivalentTo(new[] { "name", "description" });
    }

    [Fact]
    public void ForNestedType_MultiLevelNesting()
    {
        var selection = FieldSelection.FromSelectionSet(
            new List<string> { "id", "category/subcategory/name" });

        var nested = selection.ForNestedType("category").ForNestedType("subcategory");

        nested.IsAll.Should().BeFalse();
        nested.Fields.Should().Contain("name");
    }

    [Fact]
    public void ForNestedType_FieldWithoutSubPaths_ReturnsAll()
    {
        var selection = FieldSelection.FromSelectionSet(
            new List<string> { "id", "category" });

        var nested = selection.ForNestedType("category");

        nested.IsAll.Should().BeTrue();
    }

    [Fact]
    public void ForNestedType_FieldNotInSelection_ReturnsAll()
    {
        var selection = FieldSelection.Of("Id", "Name");

        var nested = selection.ForNestedType("Missing");

        nested.IsAll.Should().BeTrue();
    }

    [Fact]
    public void FromSelectionSet_Null_ReturnsAll()
    {
        var selection = FieldSelection.FromSelectionSet(null);

        selection.IsAll.Should().BeTrue();
    }

    [Fact]
    public void FromSelectionSet_EmptyList_ReturnsAll()
    {
        var selection = FieldSelection.FromSelectionSet(new List<string>());

        selection.IsAll.Should().BeTrue();
    }

    [Fact]
    public void IsRequested_OnAll_ReturnsTrueForAnyName()
    {
        var selection = FieldSelection.All();

        selection.IsRequested("anything").Should().BeTrue();
        selection.IsRequested("Name").Should().BeTrue();
    }

    [Fact]
    public void IsRequested_ReturnsFalseForAbsentFields()
    {
        var selection = FieldSelection.Of("Id", "Name");

        selection.IsRequested("Price").Should().BeFalse();
    }

    [Fact]
    public void IsRequested_ReturnsTrueForPresentFields()
    {
        var selection = FieldSelection.Of("Id", "Name");

        selection.IsRequested("Id").Should().BeTrue();
        selection.IsRequested("Name").Should().BeTrue();
    }

    [Fact]
    public void FromSelectionSet_WithFieldNameMap_MapsTopLevelNames()
    {
        var map = FieldNameMap.Builder()
            .Map("displayName", "Name")
            .Build();

        var selection = FieldSelection.FromSelectionSet(
            new List<string> { "displayName", "price" }, map);

        selection.Fields.Should().BeEquivalentTo(new[] { "Name", "price" });
    }

    [Fact]
    public void FromSelectionSet_WithFieldNameMap_MapsOnlyTopLevelSegment()
    {
        var map = FieldNameMap.Builder()
            .Map("displayName", "Name")
            .Build();

        var selection = FieldSelection.FromSelectionSet(
            new List<string> { "id", "category/displayName" }, map);

        // Only top-level "category" is a field; "displayName" is a nested path not mapped
        selection.Fields.Should().BeEquivalentTo(new[] { "id", "category" });

        var nested = selection.ForNestedType("category");
        nested.Fields.Should().Contain("displayName");
    }

    [Fact]
    public void MapWith_Identity_PreservesFields()
    {
        var selection = FieldSelection.Of("Id", "Name", "Price");

        var mapped = selection.MapWith(FieldNameMap.Identity);

        mapped.Fields.Should().BeEquivalentTo(selection.Fields);
        mapped.IsAll.Should().BeFalse();
    }

    [Fact]
    public void MapWith_TranslatesFieldNames()
    {
        var selection = FieldSelection.Of("displayName", "price");
        var map = FieldNameMap.Builder()
            .Map("displayName", "Name")
            .Build();

        var mapped = selection.MapWith(map);

        mapped.Fields.Should().BeEquivalentTo(new[] { "Name", "price" });
    }

    [Fact]
    public void IsRequested_Null_ThrowsArgumentNullException()
    {
        var selection = FieldSelection.Of("Id");

        var act = () => selection.IsRequested(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ForNestedType_Null_ThrowsArgumentNullException()
    {
        var selection = FieldSelection.Of("Id");

        var act = () => selection.ForNestedType(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MapWith_Null_ThrowsArgumentNullException()
    {
        var selection = FieldSelection.Of("Id");

        var act = () => selection.MapWith(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ForNestedType_OnAll_ReturnsAll()
    {
        var selection = FieldSelection.All();

        var nested = selection.ForNestedType("anything");

        nested.IsAll.Should().BeTrue();
    }
}
