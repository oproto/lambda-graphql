namespace Oproto.Lambda.GraphQL.Runtime;

/// <summary>
/// GraphQL field resolution info from the AppSync context.
/// </summary>
public class AppSyncInfo
{
    /// <summary>
    /// The name of the GraphQL field being resolved.
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// The parent type name (e.g., "Query", "Mutation", or a custom type).
    /// </summary>
    public string? ParentTypeName { get; set; }

    /// <summary>
    /// Flattened list of requested fields (e.g., ["id", "name", "category/name"]).
    /// </summary>
    public List<string>? SelectionSetList { get; set; }

    /// <summary>
    /// Raw GraphQL selection set text (e.g., "{ id name category { name } }").
    /// </summary>
    public string? SelectionSetGraphQL { get; set; }
}
