using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oproto.Lambda.GraphQL.SourceGenerator.Models;

namespace Oproto.Lambda.GraphQL.SourceGenerator;

/// <summary>
/// Generates resolver manifest JSON for AppSync CDK integration.
/// </summary>
public static class ResolverManifestGenerator
{
    /// <summary>
    /// The full-context JS resolver code emitted for all unit resolvers.
    /// Sends the complete AppSync context as the Lambda payload.
    /// </summary>
    private const string FullContextResolverCode =
        "export function request(ctx) {\n" +
        "  return {\n" +
        "    operation: 'Invoke',\n" +
        "    payload: {\n" +
        "      arguments: ctx.arguments,\n" +
        "      source: ctx.source,\n" +
        "      identity: ctx.identity,\n" +
        "      info: ctx.info,\n" +
        "      request: ctx.request,\n" +
        "      stash: ctx.stash,\n" +
        "      prev: ctx.prev\n" +
        "    }\n" +
        "  };\n" +
        "}\n" +
        "export function response(ctx) {\n" +
        "  if (ctx.error) {\n" +
        "    util.error(ctx.error.message, ctx.error.type);\n" +
        "  }\n" +
        "  return ctx.result;\n" +
        "}";

    /// <summary>
    /// Generates a resolver manifest JSON from resolver information.
    /// </summary>
    public static string GenerateManifest(IEnumerable<ResolverInfo> resolvers)
    {
        var resolverList = resolvers.ToList();
        var dataSources = ExtractDataSources(resolverList);

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"$schema\": \"https://lambda-graphql.dev/schemas/resolvers.json\",");
        sb.AppendLine($"  \"version\": \"1.0.0\",");
        sb.AppendLine($"  \"generatedAt\": \"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\",");
        
        // Resolvers array
        sb.AppendLine("  \"resolvers\": [");
        for (int i = 0; i < resolverList.Count; i++)
        {
            var r = resolverList[i];
            sb.AppendLine("    {");
            sb.AppendLine($"      \"typeName\": \"{EscapeJson(r.TypeName)}\",");
            sb.AppendLine($"      \"fieldName\": \"{EscapeJson(r.FieldName)}\",");
            sb.AppendLine($"      \"kind\": \"{r.Kind.ToString().ToUpperInvariant()}\",");
            
            if (r.Kind == ResolverKind.Pipeline)
            {
                // Pipeline resolver - include functions array
                sb.AppendLine("      \"functions\": [");
                for (int j = 0; j < r.Functions.Count; j++)
                {
                    var comma = j < r.Functions.Count - 1 ? "," : "";
                    sb.AppendLine($"        \"{EscapeJson(r.Functions[j])}\"{comma}");
                }
                sb.AppendLine("      ]");
            }
            else
            {
                // Unit resolver - include data source and lambda info
                sb.AppendLine($"      \"dataSource\": \"{EscapeJson(r.DataSource ?? "")}\",");
                sb.AppendLine($"      \"lambdaFunctionName\": \"{EscapeJson(r.LambdaFunctionName ?? "")}\",");
                sb.AppendLine($"      \"lambdaFunctionLogicalId\": \"{EscapeJson(r.LambdaFunctionLogicalId ?? "")}\",");
                sb.Append($"      \"resolverCode\": \"{EscapeJson(FullContextResolverCode)}\"");
                
                // Include Lambda Annotations configuration if present
                if (!string.IsNullOrEmpty(r.ResourceName))
                {
                    sb.AppendLine(",");
                    sb.Append($"      \"resourceName\": \"{EscapeJson(r.ResourceName!)}\"");
                }
                
                if (r.MemorySize.HasValue)
                {
                    sb.AppendLine(",");
                    sb.Append($"      \"memorySize\": {r.MemorySize.Value}");
                }
                
                if (r.Timeout.HasValue)
                {
                    sb.AppendLine(",");
                    sb.Append($"      \"timeout\": {r.Timeout.Value}");
                }
                
                if (!string.IsNullOrEmpty(r.Role))
                {
                    sb.AppendLine(",");
                    sb.Append($"      \"role\": \"{EscapeJson(r.Role!)}\"");
                }
                
                if (r.Policies.Count > 0)
                {
                    sb.AppendLine(",");
                    sb.AppendLine("      \"policies\": [");
                    for (int j = 0; j < r.Policies.Count; j++)
                    {
                        var comma = j < r.Policies.Count - 1 ? "," : "";
                        sb.Append($"        \"{EscapeJson(r.Policies[j])}\"{comma}");
                        if (j < r.Policies.Count - 1) sb.AppendLine();
                    }
                    sb.AppendLine();
                    sb.Append("      ]");
                }
                
                sb.AppendLine();
            }
            
            sb.Append("    }");
            if (i < resolverList.Count - 1) sb.Append(",");
            sb.AppendLine();
        }
        sb.AppendLine("  ],");
        
        // DataSources array
        sb.AppendLine("  \"dataSources\": [");
        for (int i = 0; i < dataSources.Count; i++)
        {
            var ds = dataSources[i];
            sb.AppendLine("    {");
            sb.AppendLine($"      \"name\": \"{EscapeJson(ds.Name)}\",");
            sb.AppendLine($"      \"type\": \"AWS_LAMBDA\",");
            sb.AppendLine($"      \"serviceRoleArn\": \"${{LambdaDataSourceRole.Arn}}\",");
            sb.AppendLine("      \"lambdaConfig\": {");
            sb.AppendLine($"        \"functionArn\": \"${{{EscapeJson(ds.LogicalId)}.Arn}}\"");
            sb.AppendLine("      }");
            sb.Append("    }");
            if (i < dataSources.Count - 1) sb.Append(",");
            sb.AppendLine();
        }
        sb.AppendLine("  ],");
        
        sb.AppendLine("  \"functions\": []");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        
        var sb = new StringBuilder(value.Length + 10);
        foreach (char c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    private static List<DataSourceInfo> ExtractDataSources(List<ResolverInfo> resolvers)
    {
        var dataSources = new List<DataSourceInfo>();
        var seenDataSources = new HashSet<string>();

        foreach (var resolver in resolvers)
        {
            if (!string.IsNullOrEmpty(resolver.DataSource) && 
                !seenDataSources.Contains(resolver.DataSource!))
            {
                dataSources.Add(new DataSourceInfo
                {
                    Name = resolver.DataSource!,
                    LogicalId = resolver.LambdaFunctionLogicalId ?? $"{resolver.LambdaFunctionName}Function"
                });
                seenDataSources.Add(resolver.DataSource!);
            }
        }

        return dataSources;
    }

    private class DataSourceInfo
    {
        public string Name { get; set; } = string.Empty;
        public string LogicalId { get; set; } = string.Empty;
    }
}
