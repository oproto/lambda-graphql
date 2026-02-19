namespace Oproto.Lambda.GraphQL.Runtime;

/// <summary>
/// Extension methods for extracting <see cref="FieldSelection"/> from <see cref="AppSyncResolverContext{TArguments}"/>.
/// </summary>
public static class AppSyncResolverContextExtensions
{
    /// <summary>
    /// Extracts a <see cref="FieldSelection"/> from the resolver context's selectionSetList.
    /// Returns <see cref="FieldSelection.All()"/> if info or selectionSetList is null/empty.
    /// </summary>
    public static FieldSelection GetFieldSelection<TArguments>(
        this AppSyncResolverContext<TArguments> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var selectionSetList = context.Info?.SelectionSetList;
        return FieldSelection.FromSelectionSet(selectionSetList);
    }

    /// <summary>
    /// Extracts a <see cref="FieldSelection"/> from the resolver context's selectionSetList,
    /// mapping field names through the provided <see cref="FieldNameMap"/>.
    /// </summary>
    public static FieldSelection GetFieldSelection<TArguments>(
        this AppSyncResolverContext<TArguments> context, FieldNameMap map)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(map);

        var selectionSetList = context.Info?.SelectionSetList;
        return FieldSelection.FromSelectionSet(selectionSetList, map);
    }
}
