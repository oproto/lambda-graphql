using Microsoft.CodeAnalysis;

namespace Oproto.Lambda.GraphQL.SourceGenerator;

/// <summary>
/// Diagnostic descriptors for Oproto.Lambda.GraphQL source generator errors and warnings.
/// </summary>
public static class DiagnosticDescriptors
{
    /// <summary>
    /// Error when type extraction fails due to compilation issues.
    /// </summary>
    public static readonly DiagnosticDescriptor TypeExtractionError = new(
        "LGQL001",
        "Type extraction failed",
        "Failed to extract GraphQL type information from '{0}': {1}",
        "Oproto.Lambda.GraphQL",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The source generator encountered an error while extracting type information from a class or enum.");

    /// <summary>
    /// Error when operation extraction fails due to compilation issues.
    /// </summary>
    public static readonly DiagnosticDescriptor OperationExtractionError = new(
        "LGQL002",
        "Operation extraction failed",
        "Failed to extract GraphQL operation information from method '{0}': {1}",
        "Oproto.Lambda.GraphQL",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The source generator encountered an error while extracting operation information from a Lambda function method.");

    /// <summary>
    /// Error when schema generation fails.
    /// </summary>
    public static readonly DiagnosticDescriptor SchemaGenerationError = new(
        "LGQL003",
        "Schema generation failed",
        "Failed to generate GraphQL schema: {0}",
        "Oproto.Lambda.GraphQL",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The source generator encountered a critical error during schema generation.");

    /// <summary>
    /// Warning when return type extraction fails for an operation.
    /// </summary>
    public static readonly DiagnosticDescriptor ReturnTypeExtractionWarning = new(
        "LGQL004",
        "Return type extraction failed",
        "Could not extract return type for operation '{0}', using fallback type: {1}",
        "Oproto.Lambda.GraphQL",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The source generator could not determine the exact return type for a GraphQL operation and used a fallback.");
}
