namespace Oproto.Lambda.GraphQL.Runtime;

/// <summary>
/// Represents the set of fields requested by a GraphQL client, using C# property names.
/// Immutable and safe to pass across layers.
/// </summary>
public sealed class FieldSelection
{
    private readonly HashSet<string> _fields;
    private readonly Dictionary<string, List<string>> _nestedPaths;
    private readonly bool _isAll;

    private static readonly FieldSelection _all = new(
        new HashSet<string>(),
        isAll: true,
        new Dictionary<string, List<string>>());

    private FieldSelection(HashSet<string> fields, bool isAll, Dictionary<string, List<string>> nestedPaths)
    {
        _fields = fields;
        _isAll = isAll;
        _nestedPaths = nestedPaths;
    }

    /// <summary>
    /// The set of top-level C# property names requested. Empty when <see cref="IsAll"/> is true.
    /// </summary>
    public IReadOnlySet<string> Fields => _fields;

    /// <summary>
    /// True when all fields are requested (no filtering). Created via <see cref="All"/>
    /// or when the selection set is null/empty.
    /// </summary>
    public bool IsAll => _isAll;

    /// <summary>
    /// Creates a FieldSelection representing "all fields requested" (no filtering).
    /// </summary>
    public static FieldSelection All() => _all;

    /// <summary>
    /// Creates a FieldSelection from explicit field names.
    /// </summary>
    public static FieldSelection Of(params string[] fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        return new FieldSelection(
            new HashSet<string>(fields),
            isAll: false,
            new Dictionary<string, List<string>>());
    }

    /// <summary>
    /// Parses an AppSync selectionSetList into a FieldSelection using top-level field names.
    /// Returns <see cref="All()"/> when the list is null or empty.
    /// </summary>
    public static FieldSelection FromSelectionSet(List<string>? selectionSetList)
    {
        if (selectionSetList == null || selectionSetList.Count == 0)
            return All();

        return ParseSelectionSet(selectionSetList, map: null);
    }

    /// <summary>
    /// Parses an AppSync selectionSetList and maps field names through the provided map.
    /// Returns <see cref="All()"/> when the list is null or empty.
    /// </summary>
    public static FieldSelection FromSelectionSet(List<string>? selectionSetList, FieldNameMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        if (selectionSetList == null || selectionSetList.Count == 0)
            return All();

        return ParseSelectionSet(selectionSetList, map);
    }

    /// <summary>
    /// Returns true if the named field was requested, or if <see cref="IsAll"/> is true.
    /// </summary>
    public bool IsRequested(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        return _isAll || _fields.Contains(propertyName);
    }

    /// <summary>
    /// Extracts the sub-selection for a nested object field.
    /// Returns <see cref="All()"/> if the field was requested without specific sub-fields,
    /// or if this FieldSelection <see cref="IsAll"/>.
    /// </summary>
    public FieldSelection ForNestedType(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        if (_isAll)
            return All();

        if (_nestedPaths.TryGetValue(propertyName, out var subPaths))
            return ParseSelectionSet(subPaths, map: null);

        // Field in selection but no sub-paths, or field not in selection at all
        return All();
    }

    /// <summary>
    /// Returns a new FieldSelection with all field names translated through the map.
    /// </summary>
    public FieldSelection MapWith(FieldNameMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        if (_isAll)
            return All();

        var mappedFields = new HashSet<string>();
        var mappedNestedPaths = new Dictionary<string, List<string>>();

        foreach (var field in _fields)
        {
            var mapped = map.MapName(field);
            mappedFields.Add(mapped);

            if (_nestedPaths.TryGetValue(field, out var subPaths))
                mappedNestedPaths[mapped] = subPaths;
        }

        return new FieldSelection(mappedFields, isAll: false, mappedNestedPaths);
    }

    private static FieldSelection ParseSelectionSet(List<string> paths, FieldNameMap? map)
    {
        var fields = new HashSet<string>();
        var nestedPaths = new Dictionary<string, List<string>>();

        foreach (var path in paths)
        {
            var slashIndex = path.IndexOf('/');

            if (slashIndex < 0)
            {
                // Simple top-level field
                var topLevel = map != null ? map.MapName(path) : path;
                fields.Add(topLevel);
            }
            else
            {
                // Nested path: map only the first segment
                var firstSegment = path.Substring(0, slashIndex);
                var remainder = path.Substring(slashIndex + 1);

                var topLevel = map != null ? map.MapName(firstSegment) : firstSegment;
                fields.Add(topLevel);

                if (!nestedPaths.TryGetValue(topLevel, out var subPaths))
                {
                    subPaths = new List<string>();
                    nestedPaths[topLevel] = subPaths;
                }
                subPaths.Add(remainder);
            }
        }

        return new FieldSelection(fields, isAll: false, nestedPaths);
    }
}
