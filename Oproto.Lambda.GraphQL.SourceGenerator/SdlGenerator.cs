using System.Text;
using System.Linq;
using System.Collections.Generic;
using Oproto.Lambda.GraphQL.SourceGenerator.Models;

namespace Oproto.Lambda.GraphQL.SourceGenerator;

/// <summary>
/// Generates GraphQL Schema Definition Language (SDL) from type and operation information.
/// </summary>
public static class SdlGenerator
{
    /// <summary>
    /// Generates a complete GraphQL SDL schema from types and operations.
    /// </summary>
    public static string GenerateSchema(IEnumerable<Models.TypeInfo> types, IEnumerable<ResolverInfo> operations, string? schemaName = null, string? description = null)
    {
        var sb = new StringBuilder();

        // Add schema description if provided
        if (!string.IsNullOrEmpty(description))
        {
            sb.AppendLine($"\"\"\"\n{description}\n\"\"\"");
        }

        // Generate schema definition
        var hasQuery = operations.Any(op => op.TypeName == "Query");
        var hasMutation = operations.Any(op => op.TypeName == "Mutation");
        var hasSubscription = operations.Any(op => op.TypeName == "Subscription");

        if (hasQuery || hasMutation || hasSubscription)
        {
            sb.AppendLine("schema {");
            if (hasQuery) sb.AppendLine("  query: Query");
            if (hasMutation) sb.AppendLine("  mutation: Mutation");
            if (hasSubscription) sb.AppendLine("  subscription: Subscription");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        // Generate custom types
        foreach (var type in types.OrderBy(t => t.Name))
        {
            GenerateType(sb, type);
            sb.AppendLine();
        }

        // Generate root operation types
        if (hasQuery)
        {
            GenerateRootType(sb, "Query", operations.Where(op => op.TypeName == "Query"));
            sb.AppendLine();
        }

        if (hasMutation)
        {
            GenerateRootType(sb, "Mutation", operations.Where(op => op.TypeName == "Mutation"));
            sb.AppendLine();
        }

        if (hasSubscription)
        {
            GenerateRootType(sb, "Subscription", operations.Where(op => op.TypeName == "Subscription"));
            sb.AppendLine();
        }

        return sb.ToString().Trim();
    }

    private static void GenerateType(StringBuilder sb, Models.TypeInfo type)
    {
        // Add description if present
        if (!string.IsNullOrEmpty(type.Description))
        {
            sb.AppendLine($"\"\"\"\n{type.Description}\n\"\"\"");
        }

        switch (type.Kind)
        {
            case Models.TypeKind.Object:
                GenerateObjectType(sb, type);
                break;
            case Models.TypeKind.Input:
                GenerateInputType(sb, type);
                break;
            case Models.TypeKind.Interface:
                GenerateInterfaceType(sb, type);
                break;
            case Models.TypeKind.Enum:
                GenerateEnumType(sb, type);
                break;
            case Models.TypeKind.Union:
                GenerateUnionType(sb, type);
                break;
        }
    }

    private static void GenerateObjectType(StringBuilder sb, Models.TypeInfo type)
    {
        var directives = FormatDirectives(type.Directives);
        sb.AppendLine($"type {type.Name}{directives} {{");
        foreach (var field in type.Fields.OrderBy(f => f.Name))
        {
            GenerateField(sb, field, "  ");
        }
        sb.AppendLine("}");
    }

    private static void GenerateInputType(StringBuilder sb, Models.TypeInfo type)
    {
        var directives = FormatDirectives(type.Directives);
        sb.AppendLine($"input {type.Name}{directives} {{");
        foreach (var field in type.Fields.OrderBy(f => f.Name))
        {
            GenerateInputField(sb, field, "  ");
        }
        sb.AppendLine("}");
    }

    private static void GenerateInterfaceType(StringBuilder sb, Models.TypeInfo type)
    {
        var directives = FormatDirectives(type.Directives);
        sb.AppendLine($"interface {type.Name}{directives} {{");
        foreach (var field in type.Fields.OrderBy(f => f.Name))
        {
            GenerateField(sb, field, "  ");
        }
        sb.AppendLine("}");
    }

    private static void GenerateEnumType(StringBuilder sb, Models.TypeInfo type)
    {
        // Note: AppSync does not support auth directives on enums, only on types and fields
        sb.AppendLine($"enum {type.Name} {{");
        foreach (var enumValue in type.EnumValues.OrderBy(e => e.Name))
        {
            if (!string.IsNullOrEmpty(enumValue.Description))
            {
                sb.AppendLine($"  \"\"\"\n  {enumValue.Description}\n  \"\"\"");
            }

            var line = $"  {enumValue.Name}";
            if (enumValue.IsDeprecated)
            {
                var reason = !string.IsNullOrEmpty(enumValue.DeprecationReason) 
                    ? $"reason: \"{enumValue.DeprecationReason}\"" 
                    : "";
                line += $" @deprecated({reason})";
            }
            sb.AppendLine(line);
        }
        sb.AppendLine("}");
    }

    private static void GenerateUnionType(StringBuilder sb, Models.TypeInfo type)
    {
        // Note: Description is already generated by GenerateType method
        if (type.UnionMembers.Count > 0)
        {
            var members = string.Join(" | ", type.UnionMembers);
            sb.AppendLine($"union {type.Name} = {members}");
        }
        else
        {
            sb.AppendLine($"union {type.Name}");
        }
    }

    private static void GenerateField(StringBuilder sb, FieldInfo field, string indent)
    {
        if (!string.IsNullOrEmpty(field.Description))
        {
            sb.AppendLine($"{indent}\"\"\"\n{indent}{field.Description}\n{indent}\"\"\"");
        }

        var fieldType = field.IsNullable ? field.Type : $"{field.Type}!";
        var line = $"{indent}{field.Name}: {fieldType}";

        // Add field directives
        var directives = FormatDirectives(field.Directives);
        if (!string.IsNullOrEmpty(directives))
        {
            line += directives;
        }

        if (field.IsDeprecated)
        {
            var reason = !string.IsNullOrEmpty(field.DeprecationReason) 
                ? $"reason: \"{field.DeprecationReason}\"" 
                : "";
            line += $" @deprecated({reason})";
        }

        sb.AppendLine(line);
    }

    private static void GenerateInputField(StringBuilder sb, FieldInfo field, string indent)
    {
        if (!string.IsNullOrEmpty(field.Description))
        {
            sb.AppendLine($"{indent}\"\"\"\n{indent}{field.Description}\n{indent}\"\"\"");
        }

        var fieldType = field.IsNullable ? field.Type : $"{field.Type}!";
        sb.AppendLine($"{indent}{field.Name}: {fieldType}");
    }

    private static void GenerateRootType(StringBuilder sb, string typeName, IEnumerable<ResolverInfo> operations)
    {
        sb.AppendLine($"type {typeName} {{");
        foreach (var operation in operations.OrderBy(op => op.FieldName))
        {
            // Add description if present
            if (!string.IsNullOrEmpty(operation.Description))
            {
                sb.AppendLine($"  \"\"\"\n  {operation.Description}\n  \"\"\"");
            }

            // Build field with arguments
            var fieldDef = new StringBuilder();
            fieldDef.Append($"  {operation.FieldName}");

            if (operation.Arguments.Count > 0)
            {
                if (operation.Arguments.Count == 1)
                {
                    // Single argument on same line
                    var arg = operation.Arguments[0];
                    var argType = arg.IsNullable ? arg.Type : $"{arg.Type}!";
                    fieldDef.Append($"({arg.Name}: {argType})");
                }
                else
                {
                    // Multiple arguments on separate lines
                    fieldDef.AppendLine("(");
                    for (int i = 0; i < operation.Arguments.Count; i++)
                    {
                        var arg = operation.Arguments[i];
                        var argType = arg.IsNullable ? arg.Type : $"{arg.Type}!";
                        
                        if (!string.IsNullOrEmpty(arg.Description))
                        {
                            fieldDef.AppendLine($"    \"\"\"\n    {arg.Description}\n    \"\"\"");
                        }
                        
                        var comma = i < operation.Arguments.Count - 1 ? "," : "";
                        fieldDef.AppendLine($"    {arg.Name}: {argType}{comma}");
                    }
                    fieldDef.Append("  )");
                }
            }

            fieldDef.Append($": {operation.ReturnType}");

            // Add operation directives
            var directives = FormatDirectives(operation.Directives);
            if (!string.IsNullOrEmpty(directives))
            {
                fieldDef.Append(directives);
            }

            fieldDef.AppendLine();
            sb.Append(fieldDef);
        }
        sb.AppendLine("}");
    }

    /// <summary>
    /// Formats a list of directives into SDL syntax.
    /// </summary>
    private static string FormatDirectives(List<AppliedDirectiveInfo> directives)
    {
        if (directives == null || directives.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var directive in directives)
        {
            sb.Append($" @{directive.Name}");
            
            if (directive.Arguments.Count > 0)
            {
                var args = directive.Arguments.Select(kvp => 
                {
                    // Handle array values for cognito_groups
                    if (kvp.Key == "cognito_groups")
                    {
                        // Split by comma and format as array
                        var groups = kvp.Value.Split(',').Select(g => $"\"{g.Trim()}\"");
                        return $"{kvp.Key}: [{string.Join(", ", groups)}]";
                    }
                    return $"{kvp.Key}: \"{kvp.Value}\"";
                });
                sb.Append($"({string.Join(", ", args)})");
            }
        }
        return sb.ToString();
    }
}
