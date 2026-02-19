using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Oproto.Lambda.GraphQL.Build;

/// <summary>
/// MSBuild task that extracts GraphQL schema from compiled assemblies.
/// </summary>
public class ExtractGraphQLSchemaTask : Task
{
    /// <summary>
    /// Path to the assembly to extract schema from.
    /// </summary>
    [Required]
    public string? AssemblyPath { get; set; }

    /// <summary>
    /// Output directory for generated schema files.
    /// </summary>
    [Required]
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Optional path to the intermediate output directory where generated files are stored.
    /// Used for AOT-compatible extraction from source files.
    /// </summary>
    public string? IntermediateOutputPath { get; set; }

    /// <summary>
    /// Optional path set by CompilerGeneratedFilesOutputPath MSBuild property.
    /// When EmitCompilerGeneratedFiles=true, this is where generated files are written.
    /// </summary>
    public string? CompilerGeneratedFilesOutputPath { get; set; }

    /// <summary>
    /// Name of the generated schema file.
    /// </summary>
    public string SchemaFileName { get; set; } = "schema.graphql";

    /// <summary>
    /// Name of the generated resolver manifest file.
    /// </summary>
    public string ResolverFileName { get; set; } = "resolvers.json";

    public override bool Execute()
    {
        try
        {
            if (string.IsNullOrEmpty(AssemblyPath) || string.IsNullOrEmpty(OutputDirectory))
            {
                Log.LogError("AssemblyPath and OutputDirectory are required.");
                return false;
            }

            if (!File.Exists(AssemblyPath))
            {
                Log.LogWarning($"Assembly not found: {AssemblyPath}. Skipping schema extraction.");
                return true;
            }

            Log.LogMessage(MessageImportance.High, $"Extracting GraphQL schema from {AssemblyPath}");

            Directory.CreateDirectory(OutputDirectory);

            // Strategy 1: Try parsing generated source file (AOT-compatible)
            var (sdl, resolverManifest) = TryExtractFromSourceFile();

            // Strategy 2: Fall back to MetadataLoadContext
            if (sdl == null && resolverManifest == null)
            {
                (sdl, resolverManifest) = TryExtractViaMetadataLoadContext();
            }

            if (!string.IsNullOrEmpty(sdl))
            {
                var schemaPath = Path.Combine(OutputDirectory, SchemaFileName);
                File.WriteAllText(schemaPath, sdl);
                Log.LogMessage(MessageImportance.High, $"Generated GraphQL schema: {schemaPath}");
            }

            if (!string.IsNullOrEmpty(resolverManifest))
            {
                var resolverPath = Path.Combine(OutputDirectory, ResolverFileName);
                File.WriteAllText(resolverPath, resolverManifest);
                Log.LogMessage(MessageImportance.High, $"Generated resolver manifest: {resolverPath}");
            }

            if (string.IsNullOrEmpty(sdl) && string.IsNullOrEmpty(resolverManifest))
            {
                Log.LogMessage(MessageImportance.Normal, 
                    "No GraphQL schema found. For AOT builds, ensure EmitCompilerGeneratedFiles=true is set.");
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.LogError($"Error extracting GraphQL schema: {ex.Message}");
            Log.LogMessage(MessageImportance.High, $"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Attempts to extract the schema from the generated source file.
    /// This method is AOT-compatible as it doesn't require loading the assembly.
    /// </summary>
    private (string? sdl, string? resolverManifest) TryExtractFromSourceFile()
    {
        var sourceFilePath = FindGeneratedSourceFile();
        if (sourceFilePath == null || !File.Exists(sourceFilePath))
        {
            Log.LogMessage(MessageImportance.Low,
                "Generated source file not found. Will try MetadataLoadContext fallback.");
            return (null, null);
        }

        Log.LogMessage(MessageImportance.Normal,
            $"Found generated source file: {sourceFilePath}");

        return ExtractFromSourceFile(sourceFilePath);
    }

    /// <summary>
    /// Finds the generated GraphQLSchema.g.cs file in the intermediate output directory.
    /// </summary>
    private string? FindGeneratedSourceFile()
    {
        // If CompilerGeneratedFilesOutputPath is set, check there first
        if (!string.IsNullOrEmpty(CompilerGeneratedFilesOutputPath))
        {
            var customPaths = new[]
            {
                Path.Combine(CompilerGeneratedFilesOutputPath,
                    "Oproto.Lambda.GraphQL.SourceGenerator",
                    "Oproto.Lambda.GraphQL.SourceGenerator.GraphQLSchemaGenerator",
                    "GraphQLSchema.g.cs"),
                Path.Combine(CompilerGeneratedFilesOutputPath,
                    "Oproto.Lambda.GraphQL.SourceGenerator",
                    "GraphQLSchemaGenerator",
                    "GraphQLSchema.g.cs"),
            };

            foreach (var path in customPaths)
            {
                Log.LogMessage(MessageImportance.Low, $"Looking for generated file at: {path}");
                if (File.Exists(path))
                {
                    return path;
                }
            }
        }

        // Build possible paths to the generated file
        var possiblePaths = new[]
        {
            // Standard path when EmitCompilerGeneratedFiles=true
            GetGeneratedFilePath("Oproto.Lambda.GraphQL.SourceGenerator",
                "Oproto.Lambda.GraphQL.SourceGenerator.GraphQLSchemaGenerator",
                "GraphQLSchema.g.cs"),
            // Alternative path structure
            GetGeneratedFilePath("Oproto.Lambda.GraphQL.SourceGenerator",
                "GraphQLSchemaGenerator",
                "GraphQLSchema.g.cs"),
        };

        foreach (var path in possiblePaths)
        {
            if (path != null && File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private string? GetGeneratedFilePath(string generatorName, string generatorTypeName, string fileName)
    {
        // Validate inputs to prevent path traversal
        if (string.IsNullOrEmpty(generatorName) || generatorName.Contains("..") ||
            string.IsNullOrEmpty(generatorTypeName) || generatorTypeName.Contains("..") ||
            string.IsNullOrEmpty(fileName) || fileName.Contains(".."))
        {
            return null;
        }

        string? objDir;

        if (!string.IsNullOrEmpty(IntermediateOutputPath))
        {
            // IntermediateOutputPath is typically obj/Debug/net8.0/ - go up to obj/
            var intermediateDir = IntermediateOutputPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            objDir = Path.GetDirectoryName(Path.GetDirectoryName(intermediateDir));
            if (objDir == null)
            {
                // Fallback: just use obj relative to IntermediateOutputPath parent
                objDir = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(intermediateDir))) ?? "", "obj");
            }
        }
        else
        {
            // Try to infer from AssemblyPath (bin/Debug/net8.0/assembly.dll)
            var assemblyDir = Path.GetDirectoryName(AssemblyPath);
            if (assemblyDir == null) return null;

            // Go up from bin/Debug/net8.0 to project root, then into obj
            var projectDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(assemblyDir)));
            if (projectDir == null) return null;

            objDir = Path.Combine(projectDir, "obj");
        }

        // Generated files can be in obj/Generated/ or obj/GeneratedFiles/ depending on SDK version
        var pathGenerated = Path.GetFullPath(Path.Combine(objDir, "Generated", generatorName, generatorTypeName, fileName));
        var pathGeneratedFiles = Path.GetFullPath(Path.Combine(objDir, "GeneratedFiles", generatorName, generatorTypeName, fileName));

        // Validate that paths are within expected directories
        var objDirFull = Path.GetFullPath(objDir);
        if (!pathGenerated.StartsWith(objDirFull) || !pathGeneratedFiles.StartsWith(objDirFull))
        {
            return null;
        }

        Log.LogMessage(MessageImportance.Low, $"Looking for generated file at: {pathGenerated}");
        Log.LogMessage(MessageImportance.Low, $"Looking for generated file at: {pathGeneratedFiles}");

        // Return whichever exists
        if (File.Exists(pathGenerated)) return pathGenerated;
        return pathGeneratedFiles;
    }

    /// <summary>
    /// Extracts the schema and resolver manifest from the generated source file content.
    /// The file contains: [assembly: AssemblyMetadata("GraphQL.Schema", @"...")]
    /// </summary>
    private (string? sdl, string? resolverManifest) ExtractFromSourceFile(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);

            var sdl = ExtractMetadataFromSource(content, "GraphQL.Schema");
            var resolverManifest = ExtractMetadataFromSource(content, "GraphQL.ResolverManifest");

            if (sdl != null || resolverManifest != null)
            {
                Log.LogMessage(MessageImportance.Normal,
                    "Successfully extracted schema from generated source file.");
            }

            return (sdl, resolverManifest);
        }
        catch (Exception ex)
        {
            Log.LogMessage(MessageImportance.Low,
                $"Error reading generated source file: {ex.Message}");
            return (null, null);
        }
    }

    /// <summary>
    /// Extracts a metadata value from source file content.
    /// Matches: [assembly: AssemblyMetadata("key", @"value")]
    /// </summary>
    private string? ExtractMetadataFromSource(string content, string key)
    {
        // Match the AssemblyMetadata attribute with verbatim string
        // Pattern: [assembly: AssemblyMetadata("key", @"...")]
        // In verbatim strings, quotes are escaped as "" so we need to match that pattern
        var pattern = $@"\[assembly:\s*(?:System\.Reflection\.)?AssemblyMetadata\s*\(\s*""{Regex.Escape(key)}""\s*,\s*@""((?:[^""]|"""")*)""\s*\)\]";
        
        try
        {
            var match = Regex.Match(content, pattern, RegexOptions.Singleline, TimeSpan.FromSeconds(1));

            if (match.Success)
            {
                // Unescape the verbatim string (double quotes become single quotes)
                var value = match.Groups[1].Value.Replace("\"\"", "\"");
                // Also unescape newlines that were escaped by the source generator
                value = UnescapeString(value);
                return value;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            Log.LogMessage(MessageImportance.Low, $"Regex timeout while extracting metadata '{key}'");
        }

        return null;
    }

    private string UnescapeString(string input)
    {
        return input
            .Replace("\\r\\n", "\r\n")
            .Replace("\\n", "\n")
            .Replace("\\t", "\t");
    }

    /// <summary>
    /// Attempts to extract the schema via MetadataLoadContext.
    /// This is the fallback method for non-AOT scenarios.
    /// </summary>
    private (string? sdl, string? resolverManifest) TryExtractViaMetadataLoadContext()
    {
        try
        {
            var assemblyDir = Path.GetDirectoryName(AssemblyPath) ?? ".";
            Log.LogMessage(MessageImportance.High, $"Trying MetadataLoadContext extraction from: {assemblyDir}");

            // Collect all DLLs for the resolver
            var assemblyPaths = new List<string> { AssemblyPath! };
            
            // Add assemblies from the same directory as the target
            assemblyPaths.AddRange(Directory.GetFiles(assemblyDir, "*.dll"));

            // Add core library path
            var coreAssemblyPath = typeof(object).Assembly.Location;
            var coreDir = Path.GetDirectoryName(coreAssemblyPath);
            if (coreDir != null)
            {
                assemblyPaths.AddRange(Directory.GetFiles(coreDir, "*.dll"));
            }

            var resolver = new PathAssemblyResolver(assemblyPaths.Distinct());
            using var mlc = new MetadataLoadContext(resolver);

            var assembly = mlc.LoadFromAssemblyPath(AssemblyPath!);

            var sdl = ExtractMetadataValueFromMetadataContext(assembly, "GraphQL.Schema");
            var resolverManifest = ExtractMetadataValueFromMetadataContext(assembly, "GraphQL.ResolverManifest");

            if (sdl != null)
            {
                sdl = UnescapeString(sdl);
            }
            if (resolverManifest != null)
            {
                resolverManifest = UnescapeString(resolverManifest);
            }

            if (sdl != null || resolverManifest != null)
            {
                Log.LogMessage(MessageImportance.High,
                    "Successfully extracted schema via MetadataLoadContext.");
            }

            return (sdl, resolverManifest);
        }
        catch (Exception ex)
        {
            Log.LogMessage(MessageImportance.High,
                $"MetadataLoadContext extraction failed: {ex.Message}");
            Log.LogMessage(MessageImportance.High,
                $"Stack trace: {ex.StackTrace}");
            return (null, null);
        }
    }

    private string? ExtractMetadataValueFromMetadataContext(Assembly assembly, string key)
    {
        try
        {
            var customAttributes = assembly.GetCustomAttributesData();
            foreach (var attr in customAttributes)
            {
                if (attr.AttributeType.FullName == "System.Reflection.AssemblyMetadataAttribute")
                {
                    var args = attr.ConstructorArguments;
                    if (args.Count >= 2 && args[0].Value?.ToString() == key)
                    {
                        return args[1].Value?.ToString();
                    }
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            Log.LogMessage(MessageImportance.Low, $"Could not extract metadata '{key}': {ex.Message}");
            return null;
        }
    }
}
