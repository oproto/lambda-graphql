using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Oproto.Lambda.GraphQL.SourceGenerator.Models;

namespace Oproto.Lambda.GraphQL.SourceGenerator;

/// <summary>
/// Roslyn source generator that creates GraphQL schemas from C# types and Lambda functions.
/// </summary>
[Generator(LanguageNames.CSharp)]
public partial class GraphQLSchemaGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Find classes/enums with GraphQL attributes
        var typeDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => IsGraphQLType(s),
                transform: (ctx, _) => ExtractTypeInfoWithDiagnostics(ctx))
            .Where(t => t.result != null);

        // 2. Find Lambda functions with GraphQL operation attributes
        var operationDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => IsGraphQLOperation(s),
                transform: (ctx, _) => ExtractOperationInfoWithDiagnostics(ctx))
            .Where(o => o.result != null);

        // 3. Combine types, operations, and compilation
        var combined = typeDeclarations.Collect()
            .Combine(operationDeclarations.Collect())
            .Combine(context.CompilationProvider);

        // 4. Generate schema
        context.RegisterSourceOutput(combined, GenerateSchema);
    }

    private static bool IsGraphQLType(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax and not EnumDeclarationSyntax)
            return false;

        // Check if the class/enum has any GraphQL attributes
        var hasGraphQLAttribute = node.DescendantNodes()
            .OfType<AttributeSyntax>()
            .Any(attr => attr.Name.ToString().Contains("GraphQLType") || 
                        attr.Name.ToString().Contains("GraphQLUnion"));

        return hasGraphQLAttribute;
    }
    private static (object? result, System.Collections.Generic.IEnumerable<Diagnostic> diagnostics) ExtractTypeInfoWithDiagnostics(GeneratorSyntaxContext context)
    {
        try
        {
            var semanticModel = context.SemanticModel;
            INamedTypeSymbol? typeSymbol = null;

            if (context.Node is ClassDeclarationSyntax classDecl)
            {
                typeSymbol = semanticModel.GetDeclaredSymbol(classDecl);
            }
            else if (context.Node is EnumDeclarationSyntax enumDecl)
            {
                typeSymbol = semanticModel.GetDeclaredSymbol(enumDecl);
            }

            if (typeSymbol == null)
                return (null, System.Linq.Enumerable.Empty<Diagnostic>());

            var graphqlTypeAttr = typeSymbol.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "GraphQLTypeAttribute");
            
            var graphqlUnionAttr = typeSymbol.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "GraphQLUnionAttribute");

            if (graphqlTypeAttr == null && graphqlUnionAttr == null)
                return (null, System.Linq.Enumerable.Empty<Diagnostic>());

            // At least one attribute is non-null at this point
            var attributeForName = graphqlTypeAttr ?? graphqlUnionAttr!;
            
            var typeInfo = new Models.TypeInfo
            {
                Name = GetAttributeStringValue(attributeForName, 0) ?? typeSymbol.Name,
                Description = GetAttributePropertyValue(attributeForName, "Description"),
                IsInterface = typeSymbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Interface,
                IsEnum = typeSymbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum
            };

            // Handle union types
            if (graphqlUnionAttr != null)
            {
                typeInfo.Kind = Models.TypeKind.Union;
                ExtractUnionMembers(graphqlUnionAttr, typeInfo);
            }
            // Set Kind based on type
            else if (typeInfo.IsEnum)
            {
                typeInfo.Kind = Models.TypeKind.Enum;
                ExtractEnumValues(typeSymbol, typeInfo);
            }
            else if (typeInfo.IsInterface)
            {
                typeInfo.Kind = Models.TypeKind.Interface;
            }
            else
            {
                // Check Kind property from attribute
                var kindArg = graphqlTypeAttr?.NamedArguments
                    .FirstOrDefault(arg => arg.Key == "Kind");
                
                if (kindArg?.Value.IsNull == false && kindArg.Value.Value.Value is int kindInt)
                {
                    // GraphQLTypeKind enum: 0=Object, 1=Input, 2=Interface, 3=Enum, 4=Union
                    typeInfo.Kind = kindInt switch
                    {
                        1 => Models.TypeKind.Input,
                        2 => Models.TypeKind.Interface,
                        3 => Models.TypeKind.Enum,
                        4 => Models.TypeKind.Union,
                        _ => Models.TypeKind.Object
                    };
                }
                else
                {
                    typeInfo.Kind = Models.TypeKind.Object;
                }
            }

            // Extract auth directives
            ExtractAuthDirectives(typeSymbol.GetAttributes(), typeInfo.Directives);

            // Extract fields for non-enum types
            if (!typeInfo.IsEnum)
            {
                ExtractFields(typeSymbol, typeInfo);
            }

            return (typeInfo, System.Linq.Enumerable.Empty<Diagnostic>());
        }
        catch (ArgumentException ex)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.TypeExtractionError,
                context.Node.GetLocation(),
                context.Node.ToString(),
                ex.Message);
            return (null, new[] { diagnostic });
        }
        catch (InvalidOperationException ex)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.TypeExtractionError,
                context.Node.GetLocation(),
                context.Node.ToString(),
                ex.Message);
            return (null, new[] { diagnostic });
        }
        catch (System.Exception ex)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.TypeExtractionError,
                context.Node.GetLocation(),
                context.Node.ToString(),
                $"Unexpected error: {ex.GetType().Name} - {ex.Message}");
            return (null, new[] { diagnostic });
        }
    }

    private static object? ExtractTypeInfo(GeneratorSyntaxContext context)
    {
        var (result, _) = ExtractTypeInfoWithDiagnostics(context);
        return result;
    }

    private static string? GetAttributeStringValue(AttributeData? attribute, int index)
    {
        if (attribute?.ConstructorArguments.Length > index)
        {
            var arg = attribute.ConstructorArguments[index];
            return arg.Value?.ToString();
        }
        return null;
    }

    private static string? GetAttributePropertyValue(AttributeData? attribute, string propertyName)
    {
        if (attribute == null) return null;
        var namedArg = attribute.NamedArguments.FirstOrDefault(arg => arg.Key == propertyName);
        return namedArg.Value.Value?.ToString();
    }

    private static bool GetAttributeBooleanValue(AttributeData? attribute, string propertyName)
    {
        if (attribute == null) return false;
        var namedArg = attribute.NamedArguments.FirstOrDefault(arg => arg.Key == propertyName);
        if (namedArg.Value.Value is bool boolValue) return boolValue;
        return false;
    }

    /// <summary>
    /// Formats an explicit return type, handling list wrapping based on the actual method return type.
    /// </summary>
    private static string FormatExplicitReturnType(string explicitType, IMethodSymbol methodSymbol)
    {
        // Check if the method returns a collection type
        var returnType = methodSymbol.ReturnType;
        
        // Unwrap Task<T>/ValueTask<T>
        if (returnType is INamedTypeSymbol namedReturnType && namedReturnType.IsGenericType)
        {
            var constructedFrom = namedReturnType.ConstructedFrom.ToDisplayString();
            if (constructedFrom.StartsWith("System.Threading.Tasks.Task<") ||
                constructedFrom.StartsWith("System.Threading.Tasks.ValueTask<"))
            {
                if (namedReturnType.TypeArguments.Length > 0)
                {
                    returnType = namedReturnType.TypeArguments[0];
                }
            }
        }

        // Check if it's a list/array type
        bool isList = false;
        if (returnType is INamedTypeSymbol listType && listType.IsGenericType)
        {
            var constructedFrom = listType.ConstructedFrom.ToDisplayString();
            isList = constructedFrom.StartsWith("System.Collections.Generic.List<") ||
                     constructedFrom.StartsWith("System.Collections.Generic.IList<") ||
                     constructedFrom.StartsWith("System.Collections.Generic.IEnumerable<");
        }
        else if (returnType is IArrayTypeSymbol)
        {
            isList = true;
        }

        // Format the type with list wrapper if needed
        var formattedType = isList ? $"[{explicitType}]!" : $"{explicitType}!";
        return formattedType;
    }

    private static void ExtractEnumValues(INamedTypeSymbol enumSymbol, Models.TypeInfo typeInfo)
    {
        foreach (var member in enumSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (member.IsStatic && member.HasConstantValue)
            {
                var enumValueAttr = member.GetAttributes()
                    .FirstOrDefault(attr => attr.AttributeClass?.Name == "GraphQLEnumValueAttribute");

                var enumValue = new EnumValueInfo
                {
                    Name = GetAttributeStringValue(enumValueAttr, 0) ?? member.Name,
                    Description = GetAttributePropertyValue(enumValueAttr, "Description"),
                    IsDeprecated = GetAttributeBooleanValue(enumValueAttr, "Deprecated"),
                    DeprecationReason = GetAttributePropertyValue(enumValueAttr, "DeprecationReason")
                };

                typeInfo.EnumValues.Add(enumValue);
            }
        }
    }

    private static void ExtractUnionMembers(AttributeData unionAttr, Models.TypeInfo typeInfo)
    {
        // Extract member types from the MemberTypes parameter
        var memberTypesArg = unionAttr.ConstructorArguments.Skip(1).FirstOrDefault();
        
        // Validate that we have a valid array argument
        if (memberTypesArg.IsNull || memberTypesArg.Kind != TypedConstantKind.Array)
            return;
            
        foreach (var memberType in memberTypesArg.Values)
        {
            if (memberType.Value is string memberTypeName)
            {
                typeInfo.UnionMembers.Add(memberTypeName);
            }
        }
    }

    private static void ExtractFields(INamedTypeSymbol typeSymbol, Models.TypeInfo typeInfo)
    {
        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is IPropertySymbol property)
            {
                // Skip if marked with GraphQLIgnore
                var hasIgnore = property.GetAttributes()
                    .Any(attr => attr.AttributeClass?.Name == "GraphQLIgnoreAttribute");
                if (hasIgnore) continue;

                var fieldAttr = property.GetAttributes()
                    .FirstOrDefault(attr => attr.AttributeClass?.Name == "GraphQLFieldAttribute");

                var fieldInfo = new FieldInfo
                {
                    Name = GetAttributeStringValue(fieldAttr, 0) ?? property.Name,
                    Description = GetAttributePropertyValue(fieldAttr, "Description"),
                    Type = TypeMapper.MapType(property.Type),
                    IsNullable = !TypeMapper.IsNonNull(property.Type),
                    IsDeprecated = GetAttributeBooleanValue(fieldAttr, "Deprecated"),
                    DeprecationReason = GetAttributePropertyValue(fieldAttr, "DeprecationReason")
                };

                // Check for GraphQLTimestamp override
                var hasTimestamp = property.GetAttributes()
                    .Any(attr => attr.AttributeClass?.Name == "GraphQLTimestampAttribute");
                if (hasTimestamp)
                {
                    fieldInfo.Type = "AWSTimestamp";
                }

                // Check for GraphQLNonNull override
                var hasNonNull = property.GetAttributes()
                    .Any(attr => attr.AttributeClass?.Name == "GraphQLNonNullAttribute");
                if (hasNonNull)
                {
                    fieldInfo.IsNullable = false;
                }

                // Extract auth directives for the field
                ExtractAuthDirectives(property.GetAttributes(), fieldInfo.Directives);

                typeInfo.Fields.Add(fieldInfo);
            }
        }
    }

    private static void ExtractAuthDirectives(System.Collections.Immutable.ImmutableArray<AttributeData> attributes, System.Collections.Generic.List<Models.AppliedDirectiveInfo> directives)
    {
        foreach (var attr in attributes)
        {
            if (attr.AttributeClass?.Name != "GraphQLAuthDirectiveAttribute")
                continue;

            // Get AuthMode from constructor argument
            if (attr.ConstructorArguments.Length == 0)
                continue;

            var authModeValue = attr.ConstructorArguments[0].Value;
            if (authModeValue == null)
                continue;

            // AuthMode enum: 0=ApiKey, 1=UserPools, 2=IAM, 3=OpenIDConnect, 4=Lambda
            var directiveName = (int)authModeValue switch
            {
                0 => "aws_api_key",
                1 => "aws_cognito_user_pools",
                2 => "aws_iam",
                3 => "aws_oidc",
                4 => "aws_lambda",
                _ => null
            };

            if (directiveName == null)
                continue;

            var directive = new Models.AppliedDirectiveInfo { Name = directiveName };

            // Extract CognitoGroups if present
            var cognitoGroups = GetAttributePropertyValue(attr, "CognitoGroups");
            if (!string.IsNullOrEmpty(cognitoGroups))
            {
                directive.Arguments["cognito_groups"] = cognitoGroups!;
            }

            directives.Add(directive);
        }
    }
    private static bool IsGraphQLOperation(SyntaxNode node)
    {
        if (node is not MethodDeclarationSyntax method)
            return false;

        var attributes = method.AttributeLists
            .SelectMany(list => list.Attributes)
            .Select(attr => attr.Name.ToString())
            .ToList();

        var hasLambdaFunction = attributes.Any(name => name.Contains("LambdaFunction"));
        var hasGraphQLOperation = attributes.Any(name => 
            name.Contains("GraphQLQuery") || 
            name.Contains("GraphQLMutation") || 
            name.Contains("GraphQLSubscription"));

        return hasLambdaFunction && hasGraphQLOperation;
    }
    private static (object? result, System.Collections.Generic.IEnumerable<Diagnostic> diagnostics) ExtractOperationInfoWithDiagnostics(GeneratorSyntaxContext context)
    {
        try
        {
            if (context.Node is not MethodDeclarationSyntax method)
                return (null, System.Linq.Enumerable.Empty<Diagnostic>());

            var semanticModel = context.SemanticModel;
            var methodSymbol = semanticModel.GetDeclaredSymbol(method);
            if (methodSymbol == null)
                return (null, System.Linq.Enumerable.Empty<Diagnostic>());

            // Find GraphQL operation attribute
            var graphqlOpAttr = methodSymbol.GetAttributes()
                .FirstOrDefault(attr => 
                    attr.AttributeClass?.Name == "GraphQLQueryAttribute" ||
                    attr.AttributeClass?.Name == "GraphQLMutationAttribute" ||
                    attr.AttributeClass?.Name == "GraphQLSubscriptionAttribute");

            if (graphqlOpAttr == null)
                return (null, System.Linq.Enumerable.Empty<Diagnostic>());

            // Find resolver attribute
            var resolverAttr = methodSymbol.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "GraphQLResolverAttribute");

            // Find Lambda function attribute
            var lambdaAttr = methodSymbol.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "LambdaFunctionAttribute");

            var operationName = GetAttributeStringValue(graphqlOpAttr, 0) ?? methodSymbol.Name;
            var operationDescription = GetAttributePropertyValue(graphqlOpAttr, "Description");
            var explicitReturnType = GetAttributePropertyValue(graphqlOpAttr, "ReturnType");
            var typeName = graphqlOpAttr.AttributeClass?.Name switch
            {
                "GraphQLQueryAttribute" => "Query",
                "GraphQLMutationAttribute" => "Mutation",
                "GraphQLSubscriptionAttribute" => "Subscription",
                _ => "Query"
            };

            var lambdaFunctionName = methodSymbol.Name;
            var lambdaFunctionLogicalId = $"{methodSymbol.Name}Function";
            var dataSourceName = GetAttributePropertyValue(resolverAttr, "DataSource");
            
            // If no DataSource specified, auto-generate from Lambda function name
            if (string.IsNullOrEmpty(dataSourceName))
            {
                dataSourceName = $"{lambdaFunctionName}DataSource";
            }

            var resolverInfo = new ResolverInfo
            {
                TypeName = typeName,
                FieldName = operationName,
                Description = operationDescription,
                Kind = ResolverKind.Unit,
                DataSource = dataSourceName,
                LambdaFunctionName = lambdaFunctionName,
                LambdaFunctionLogicalId = lambdaFunctionLogicalId,
                RequestMapping = GetAttributePropertyValue(resolverAttr, "RequestMapping"),
                ResponseMapping = GetAttributePropertyValue(resolverAttr, "ResponseMapping"),
                ReturnType = !string.IsNullOrEmpty(explicitReturnType) 
                    ? FormatExplicitReturnType(explicitReturnType!, methodSymbol)
                    : ReturnTypeExtractor.GetFormattedReturnType(methodSymbol)
            };

            // Extract Lambda Annotations configuration if present
            if (lambdaAttr != null)
            {
                resolverInfo.ResourceName = GetAttributePropertyValue(lambdaAttr, "ResourceName");
                
                var memorySize = GetAttributePropertyValue(lambdaAttr, "MemorySize");
                if (!string.IsNullOrEmpty(memorySize) && int.TryParse(memorySize, out var memory))
                {
                    resolverInfo.MemorySize = memory;
                }
                
                var timeout = GetAttributePropertyValue(lambdaAttr, "Timeout");
                if (!string.IsNullOrEmpty(timeout) && int.TryParse(timeout, out var timeoutValue))
                {
                    resolverInfo.Timeout = timeoutValue;
                }
                
                resolverInfo.Role = GetAttributePropertyValue(lambdaAttr, "Role");
                
                // Extract policies array if present
                var policiesValue = GetAttributePropertyValue(lambdaAttr, "Policies");
                if (!string.IsNullOrEmpty(policiesValue))
                {
                    // Policies is typically a string array in Lambda Annotations
                    resolverInfo.Policies.Add(policiesValue!);
                }
            }

            // Extract auth directives for the operation
            ExtractAuthDirectives(methodSymbol.GetAttributes(), resolverInfo.Directives);

            // Extract method parameters as GraphQL arguments
            foreach (var parameter in methodSymbol.Parameters)
            {
                // Check for special parameters
                var paramType = parameter.Type.ToDisplayString();
                if (paramType.Contains("ILambdaContext"))
                {
                    continue;
                }

                var argAttr = parameter.GetAttributes()
                    .FirstOrDefault(attr => attr.AttributeClass?.Name == "GraphQLArgumentAttribute");

                var argInfo = new Models.ArgumentInfo
                {
                    Name = GetAttributeStringValue(argAttr, 0) ?? parameter.Name,
                    Description = GetAttributePropertyValue(argAttr, "Description"),
                    Type = TypeMapper.MapType(parameter.Type),
                    IsNullable = !TypeMapper.IsNonNull(parameter.Type)
                };

                resolverInfo.Arguments.Add(argInfo);
            }

            // Check if it's a pipeline resolver
            var kindValue = GetAttributePropertyValue(resolverAttr, "Kind");
            if (kindValue == "Pipeline")
            {
                resolverInfo.Kind = ResolverKind.Pipeline;
            }

            // Extract pipeline functions array
            if (resolverAttr != null)
            {
                var functionsArg = resolverAttr.NamedArguments
                    .FirstOrDefault(arg => arg.Key == "Functions");
                
                if (!functionsArg.Value.IsNull && functionsArg.Value.Kind == TypedConstantKind.Array)
                {
                    resolverInfo.Kind = ResolverKind.Pipeline;
                    foreach (var funcValue in functionsArg.Value.Values)
                    {
                        if (funcValue.Value is string funcName)
                        {
                            resolverInfo.Functions.Add(funcName);
                        }
                    }
                }
            }

            return (resolverInfo, System.Linq.Enumerable.Empty<Diagnostic>());
        }
        catch (ArgumentException ex)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.OperationExtractionError,
                context.Node.GetLocation(),
                context.Node.ToString(),
                ex.Message);
            return (null, new[] { diagnostic });
        }
        catch (InvalidOperationException ex)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.OperationExtractionError,
                context.Node.GetLocation(),
                context.Node.ToString(),
                ex.Message);
            return (null, new[] { diagnostic });
        }
        catch (System.Exception ex)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.OperationExtractionError,
                context.Node.GetLocation(),
                context.Node.ToString(),
                $"Unexpected error: {ex.GetType().Name} - {ex.Message}");
            return (null, new[] { diagnostic });
        }
    }

    private static object? ExtractOperationInfo(GeneratorSyntaxContext context)
    {
        var (result, _) = ExtractOperationInfoWithDiagnostics(context);
        return result;
    }
    
    private static void GenerateSchema(SourceProductionContext context, 
        ((ImmutableArray<(object? result, System.Collections.Generic.IEnumerable<Diagnostic> diagnostics)> Left, ImmutableArray<(object? result, System.Collections.Generic.IEnumerable<Diagnostic> diagnostics)> Right) Left, Compilation Right) combined) 
    { 
        try
        {
            if (combined.Left.Left == null || combined.Left.Right == null || combined.Right == null)
                return;

            var (typeAndOperationData, compilation) = combined;
            var (typeData, operationData) = typeAndOperationData;

            // Collect and report all diagnostics
            foreach (var (_, diagnostics) in typeData)
            {
                foreach (var diagnostic in diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }
            
            foreach (var (_, diagnostics) in operationData)
            {
                foreach (var diagnostic in diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }

            // Extract types and operations from the collected data
            var types = typeData.Where(t => t.result != null).Select(t => t.result).OfType<Models.TypeInfo>().ToList();
            var operations = operationData.Where(o => o.result != null).Select(o => o.result).OfType<ResolverInfo>().ToList();

            // Validate data source names - each data source must map to exactly one Lambda function
            var dataSourceToLambda = new Dictionary<string, string>();
            foreach (var operation in operations)
            {
                if (!string.IsNullOrEmpty(operation.DataSource) && !string.IsNullOrEmpty(operation.LambdaFunctionName))
                {
                    if (dataSourceToLambda.TryGetValue(operation.DataSource!, out var existingLambda))
                    {
                        if (existingLambda != operation.LambdaFunctionName)
                        {
                            var diagnostic = Diagnostic.Create(
                                DiagnosticDescriptors.OperationExtractionError,
                                Location.None,
                                operation.FieldName,
                                $"Data source '{operation.DataSource}' is used by multiple Lambda functions: '{existingLambda}' and '{operation.LambdaFunctionName}'. Each Lambda function must have a unique data source name.");
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                    else
                    {
                        dataSourceToLambda[operation.DataSource!] = operation.LambdaFunctionName!;
                    }
                }
            }

            // Generate debug info about what was found
            var debugInfo = $"// Found {types.Count} types and {operations.Count} operations";

            if (!types.Any() && !operations.Any())
            {
                // Generate a placeholder schema with debug info
                var placeholderSource = $@"// Generated by Oproto.Lambda.GraphQL Source Generator
{debugInfo}
// No GraphQL types or operations were detected.
// Make sure your types have [GraphQLType] attribute and operations have [GraphQLQuery]/[GraphQLMutation] attributes.

using System.Reflection;

[assembly: AssemblyMetadata(""GraphQL.Schema"", ""# No schema generated - no types found"")]
[assembly: AssemblyMetadata(""GraphQL.ResolverManifest"", ""{{\""resolvers\"": []}}"")]
";
                context.AddSource("GraphQLAssemblyMetadata.g.cs", placeholderSource);
                return;
            }

            // Find schema attribute for metadata
            string? schemaName = null;
            string? schemaDescription = null;

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var root = syntaxTree.GetRoot();
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                
                // Look for assembly attributes
                var assemblyAttrs = root.DescendantNodes()
                    .OfType<AttributeSyntax>()
                    .Where(attr => attr.Name.ToString().Contains("GraphQLSchema"));

                foreach (var attr in assemblyAttrs)
                {
                    // Extract schema name and description - simplified
                    schemaName = "GeneratedSchema";
                    schemaDescription = "Generated GraphQL schema from Lambda functions";
                    break;
                }
            }

            // Generate GraphQL SDL
            var sdl = SdlGenerator.GenerateSchema(types, operations, schemaName, schemaDescription);
            
            // Generate resolver manifest
            var resolverManifest = ResolverManifestGenerator.GenerateManifest(operations);

            // Emit SDL as embedded resource
            var sdlSource = $@"
using System.Reflection;

[assembly: System.Reflection.AssemblyMetadata(""GraphQL.Schema"", @""{EscapeString(sdl)}"")]
[assembly: System.Reflection.AssemblyMetadata(""GraphQL.ResolverManifest"", @""{EscapeString(resolverManifest)}"")]
";

            context.AddSource("GraphQLSchema.g.cs", sdlSource);
        }
        catch (System.Exception ex)
        {
            // Add diagnostic for debugging
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.SchemaGenerationError,
                Location.None,
                ex.Message);
            
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static string EscapeString(string input)
    {
        var sb = new StringBuilder(input.Length + 20);
        foreach (char c in input)
        {
            switch (c)
            {
                case '"': sb.Append("\"\""); break;
                case '\r': sb.Append("\\r"); break;
                case '\n': sb.Append("\\n"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}
