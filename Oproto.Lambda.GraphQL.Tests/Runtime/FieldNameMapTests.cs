using FluentAssertions;
using Oproto.Lambda.GraphQL.Runtime;

namespace Oproto.Lambda.GraphQL.Tests.Runtime;

public class FieldNameMapTests
{
    [Fact]
    public void Builder_FluentApi_CreatesCorrectMappings()
    {
        var map = FieldNameMap.Builder()
            .Map("displayName", "Name")
            .Map("price", "Price")
            .Build();

        map.MapName("displayName").Should().Be("Name");
        map.MapName("price").Should().Be("Price");
    }

    [Fact]
    public void Identity_MapName_ReturnsSameName()
    {
        FieldNameMap.Identity.MapName("anything").Should().Be("anything");
        FieldNameMap.Identity.MapName("Name").Should().Be("Name");
    }

    [Fact]
    public void MapName_UnmappedName_ReturnsOriginal()
    {
        var map = FieldNameMap.Builder()
            .Map("displayName", "Name")
            .Build();

        map.MapName("unmapped").Should().Be("unmapped");
    }

    [Fact]
    public void Then_ComposesLeftToRight()
    {
        var map1 = FieldNameMap.Builder().Map("displayName", "Name").Build();
        var map2 = FieldNameMap.Builder().Map("Name", "name").Build();

        var composed = map1.Then(map2);

        composed.MapName("displayName").Should().Be("name");
    }

    [Fact]
    public void Then_PreservesPassThroughFromSecondMap()
    {
        var map1 = FieldNameMap.Builder().Map("displayName", "Name").Build();
        var map2 = FieldNameMap.Builder().Map("Name", "name").Map("id", "Id").Build();

        var composed = map1.Then(map2);

        // "id" → "Id" comes from map2 pass-through
        composed.MapName("id").Should().Be("Id");
        // "displayName" → "Name" → "name" via composition
        composed.MapName("displayName").Should().Be("name");
    }

    [Fact]
    public void MapName_Null_ThrowsArgumentNullException()
    {
        var map = FieldNameMap.Builder().Build();
        var act = () => map.MapName(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Then_Null_ThrowsArgumentNullException()
    {
        var map = FieldNameMap.Builder().Build();
        var act = () => map.Then(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Builder_Map_NullSource_ThrowsArgumentNullException()
    {
        var act = () => FieldNameMap.Builder().Map(null!, "target");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Builder_Map_NullTarget_ThrowsArgumentNullException()
    {
        var act = () => FieldNameMap.Builder().Map("source", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Builder_DuplicateSource_LastMappingWins()
    {
        var map = FieldNameMap.Builder()
            .Map("displayName", "FirstValue")
            .Map("displayName", "SecondValue")
            .Build();

        map.MapName("displayName").Should().Be("SecondValue");
    }

    [Fact]
    public void Identity_IsSingleton()
    {
        FieldNameMap.Identity.Should().BeSameAs(FieldNameMap.Identity);
    }

    [Fact]
    public void Then_UnmappedInBoth_PassesThrough()
    {
        var map1 = FieldNameMap.Builder().Map("a", "b").Build();
        var map2 = FieldNameMap.Builder().Map("c", "d").Build();

        var composed = map1.Then(map2);

        composed.MapName("unknown").Should().Be("unknown");
    }
}
