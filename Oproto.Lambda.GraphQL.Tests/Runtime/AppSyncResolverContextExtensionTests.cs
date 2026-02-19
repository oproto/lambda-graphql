using FluentAssertions;
using Oproto.Lambda.GraphQL.Runtime;

namespace Oproto.Lambda.GraphQL.Tests.Runtime;

public class AppSyncResolverContextExtensionTests
{
    [Fact]
    public void GetFieldSelection_WithValidSelectionSetList_ReturnsCorrectFieldSelection()
    {
        var context = new AppSyncResolverContext<object>
        {
            Info = new AppSyncInfo
            {
                SelectionSetList = new List<string> { "id", "name", "category/name" }
            }
        };

        var selection = context.GetFieldSelection();

        selection.IsAll.Should().BeFalse();
        selection.Fields.Should().BeEquivalentTo(new[] { "id", "name", "category" });
    }

    [Fact]
    public void GetFieldSelection_WithNullInfo_ReturnsAll()
    {
        var context = new AppSyncResolverContext<object>
        {
            Info = null
        };

        var selection = context.GetFieldSelection();

        selection.IsAll.Should().BeTrue();
    }

    [Fact]
    public void GetFieldSelection_WithNullSelectionSetList_ReturnsAll()
    {
        var context = new AppSyncResolverContext<object>
        {
            Info = new AppSyncInfo
            {
                SelectionSetList = null
            }
        };

        var selection = context.GetFieldSelection();

        selection.IsAll.Should().BeTrue();
    }

    [Fact]
    public void GetFieldSelection_WithEmptySelectionSetList_ReturnsAll()
    {
        var context = new AppSyncResolverContext<object>
        {
            Info = new AppSyncInfo
            {
                SelectionSetList = new List<string>()
            }
        };

        var selection = context.GetFieldSelection();

        selection.IsAll.Should().BeTrue();
    }

    [Fact]
    public void GetFieldSelection_WithMap_MapsFieldNamesCorrectly()
    {
        var context = new AppSyncResolverContext<object>
        {
            Info = new AppSyncInfo
            {
                SelectionSetList = new List<string> { "displayName", "price" }
            }
        };
        var map = FieldNameMap.Builder()
            .Map("displayName", "Name")
            .Build();

        var selection = context.GetFieldSelection(map);

        selection.IsAll.Should().BeFalse();
        selection.Fields.Should().BeEquivalentTo(new[] { "Name", "price" });
    }

    [Fact]
    public void GetFieldSelection_WithMap_NullInfo_ReturnsAll()
    {
        var context = new AppSyncResolverContext<object>
        {
            Info = null
        };
        var map = FieldNameMap.Builder()
            .Map("displayName", "Name")
            .Build();

        var selection = context.GetFieldSelection(map);

        selection.IsAll.Should().BeTrue();
    }

    [Fact]
    public void GetFieldSelection_WithMap_EmptySelectionSetList_ReturnsAll()
    {
        var context = new AppSyncResolverContext<object>
        {
            Info = new AppSyncInfo
            {
                SelectionSetList = new List<string>()
            }
        };

        var selection = context.GetFieldSelection(FieldNameMap.Identity);

        selection.IsAll.Should().BeTrue();
    }

    [Fact]
    public void GetFieldSelection_NullContext_ThrowsArgumentNullException()
    {
        AppSyncResolverContext<object>? context = null;

        var act = () => context!.GetFieldSelection();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetFieldSelection_WithMap_NullContext_ThrowsArgumentNullException()
    {
        AppSyncResolverContext<object>? context = null;

        var act = () => context!.GetFieldSelection(FieldNameMap.Identity);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetFieldSelection_WithNullMap_ThrowsArgumentNullException()
    {
        var context = new AppSyncResolverContext<object>
        {
            Info = new AppSyncInfo
            {
                SelectionSetList = new List<string> { "id" }
            }
        };

        var act = () => context.GetFieldSelection(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
