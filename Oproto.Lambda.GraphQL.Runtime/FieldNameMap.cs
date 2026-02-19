namespace Oproto.Lambda.GraphQL.Runtime;

/// <summary>
/// Immutable mapping from source names to target names.
/// Composable via <see cref="Then"/> for multi-layer architectures.
/// </summary>
public sealed class FieldNameMap
{
    private readonly IReadOnlyDictionary<string, string> _mappings;

    private static readonly FieldNameMap _identity = new(new Dictionary<string, string>());

    private FieldNameMap(IReadOnlyDictionary<string, string> mappings)
    {
        _mappings = mappings;
    }

    /// <summary>
    /// Identity map — every name maps to itself.
    /// </summary>
    public static FieldNameMap Identity => _identity;

    /// <summary>
    /// Creates a new builder for constructing a FieldNameMap.
    /// </summary>
    public static FieldNameMapBuilder Builder() => new();

    /// <summary>
    /// Maps a source name to its target name. Returns the original name if no mapping exists.
    /// </summary>
    public string MapName(string sourceName)
    {
        ArgumentNullException.ThrowIfNull(sourceName);
        return _mappings.TryGetValue(sourceName, out var target) ? target : sourceName;
    }

    /// <summary>
    /// Composes this map with another: source → (this) → intermediate → (next) → target.
    /// </summary>
    public FieldNameMap Then(FieldNameMap next)
    {
        ArgumentNullException.ThrowIfNull(next);

        var composed = new Dictionary<string, string>();

        // For each entry in this map, chain through next
        foreach (var (source, intermediate) in _mappings)
        {
            composed[source] = next.MapName(intermediate);
        }

        // For each entry in next that isn't already covered as an intermediate
        var intermediateValues = new HashSet<string>(_mappings.Values);
        foreach (var (source2, target2) in next._mappings)
        {
            if (!intermediateValues.Contains(source2) && !composed.ContainsKey(source2))
            {
                composed[source2] = target2;
            }
        }

        return new FieldNameMap(composed);
    }

    /// <summary>
    /// Gets the explicit mappings (internal, for testing/composition).
    /// </summary>
    internal IReadOnlyDictionary<string, string> Mappings => _mappings;

    internal static FieldNameMap FromDictionary(Dictionary<string, string> mappings) =>
        new(new Dictionary<string, string>(mappings));
}

/// <summary>
/// Fluent builder for <see cref="FieldNameMap"/>.
/// </summary>
public sealed class FieldNameMapBuilder
{
    private readonly Dictionary<string, string> _mappings = new();

    /// <summary>
    /// Adds a mapping from sourceName to targetName. If the source already exists, the last mapping wins.
    /// </summary>
    public FieldNameMapBuilder Map(string sourceName, string targetName)
    {
        ArgumentNullException.ThrowIfNull(sourceName);
        ArgumentNullException.ThrowIfNull(targetName);
        _mappings[sourceName] = targetName;
        return this;
    }

    /// <summary>
    /// Builds the immutable FieldNameMap.
    /// </summary>
    public FieldNameMap Build() => FieldNameMap.FromDictionary(_mappings);

    internal FieldNameMapBuilder() { }
}
