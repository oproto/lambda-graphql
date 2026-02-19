using FluentAssertions;
using Oproto.Lambda.GraphQL.SourceGenerator;
using Oproto.Lambda.GraphQL.SourceGenerator.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace Oproto.Lambda.GraphQL.Tests;

public class ResolverManifestTests
{
    private const string TestDataSource = "ProductsLambda";
    private const string TestFunctionName = "GetProduct";
    private const string TestFunctionLogicalId = "GetProductFunction";

    [Fact]
    public void GenerateManifest_ShouldCreateValidJson()
    {
        // Arrange
        var resolvers = new List<ResolverInfo>
        {
            new ResolverInfo
            {
                TypeName = "Query",
                FieldName = "getProduct",
                Kind = ResolverKind.Unit,
                DataSource = TestDataSource,
                LambdaFunctionName = TestFunctionName,
                LambdaFunctionLogicalId = TestFunctionLogicalId
            }
        };

        // Act
        var json = ResolverManifestGenerator.GenerateManifest(resolvers);

        // Assert
        json.Should().Contain("\"$schema\"");
        json.Should().Contain("\"version\"");
        json.Should().Contain("\"resolvers\"");
        
        // Should be valid JSON
        var action = () => JsonDocument.Parse(json);
        action.Should().NotThrow();
    }

    [Fact]
    public void GenerateManifest_ShouldIncludeResolverDetails()
    {
        // Arrange
        var resolvers = new List<ResolverInfo>
        {
            new ResolverInfo
            {
                TypeName = "Query",
                FieldName = "getProduct",
                Kind = ResolverKind.Unit,
                DataSource = TestDataSource,
                LambdaFunctionName = TestFunctionName,
                LambdaFunctionLogicalId = TestFunctionLogicalId
            }
        };

        // Act
        var json = ResolverManifestGenerator.GenerateManifest(resolvers);

        // Assert
        json.Should().Contain("\"typeName\": \"Query\"");
        json.Should().Contain("\"fieldName\": \"getProduct\"");
        json.Should().Contain("\"kind\": \"UNIT\"");
        json.Should().Contain($"\"dataSource\": \"{TestDataSource}\"");
        json.Should().Contain($"\"lambdaFunctionName\": \"{TestFunctionName}\"");
        json.Should().Contain($"\"lambdaFunctionLogicalId\": \"{TestFunctionLogicalId}\"");
        json.Should().Contain("\"resolverCode\":");
    }

    [Fact]
    public void GenerateManifest_ShouldHandlePipelineResolvers()
    {
        // Arrange
        var resolvers = new List<ResolverInfo>
        {
            new ResolverInfo
            {
                TypeName = "Mutation",
                FieldName = "createOrder",
                Kind = ResolverKind.Pipeline,
                Functions = new List<string> { "ValidateOrder", "CreateOrder", "NotifyCustomer" }
            }
        };

        // Act
        var json = ResolverManifestGenerator.GenerateManifest(resolvers);

        // Assert
        json.Should().Contain("\"kind\": \"PIPELINE\"");
        json.Should().Contain("\"functions\"");
        json.Should().Contain("ValidateOrder");
        json.Should().Contain("CreateOrder");
        json.Should().Contain("NotifyCustomer");
    }

    [Fact]
    public void GenerateManifest_ShouldIncludeDataSources()
    {
        // Arrange
        var resolvers = new List<ResolverInfo>
        {
            new ResolverInfo
            {
                TypeName = "Query",
                FieldName = "getProduct",
                DataSource = "ProductsLambda",
                LambdaFunctionName = "GetProduct",
                LambdaFunctionLogicalId = "GetProductFunction"
            }
        };

        // Act
        var json = ResolverManifestGenerator.GenerateManifest(resolvers);

        // Assert
        json.Should().Contain("\"dataSources\"");
        json.Should().Contain("\"name\": \"ProductsLambda\"");
        json.Should().Contain("\"type\": \"AWS_LAMBDA\"");
    }

    [Fact]
    public void GenerateManifest_ShouldHandleMultipleResolvers()
    {
        // Arrange
        var resolvers = new List<ResolverInfo>
        {
            new ResolverInfo
            {
                TypeName = "Query",
                FieldName = "getProduct",
                DataSource = "ProductsLambda",
                LambdaFunctionName = "GetProduct"
            },
            new ResolverInfo
            {
                TypeName = "Mutation",
                FieldName = "createProduct",
                DataSource = "ProductsLambda",
                LambdaFunctionName = "CreateProduct"
            }
        };

        // Act
        var json = ResolverManifestGenerator.GenerateManifest(resolvers);
        var doc = JsonDocument.Parse(json);
        var resolversArray = doc.RootElement.GetProperty("resolvers");

        // Assert
        resolversArray.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public void GenerateManifest_ShouldHandleEmptyResolvers()
    {
        // Arrange
        var resolvers = new List<ResolverInfo>();

        // Act
        var json = ResolverManifestGenerator.GenerateManifest(resolvers);
        var doc = JsonDocument.Parse(json);

        // Assert
        doc.RootElement.GetProperty("resolvers").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public void GenerateManifest_ResolverCode_ShouldContainAllSevenContextFields()
    {
        // Arrange
        var resolvers = new List<ResolverInfo>
        {
            new ResolverInfo
            {
                TypeName = "Query",
                FieldName = "getProduct",
                Kind = ResolverKind.Unit,
                DataSource = TestDataSource,
                LambdaFunctionName = TestFunctionName,
                LambdaFunctionLogicalId = TestFunctionLogicalId
            }
        };

        // Act
        var json = ResolverManifestGenerator.GenerateManifest(resolvers);
        var doc = JsonDocument.Parse(json);
        var resolver = doc.RootElement.GetProperty("resolvers")[0];
        var resolverCode = resolver.GetProperty("resolverCode").GetString();

        // Assert - all seven context fields must be present
        resolverCode.Should().Contain("ctx.arguments");
        resolverCode.Should().Contain("ctx.source");
        resolverCode.Should().Contain("ctx.identity");
        resolverCode.Should().Contain("ctx.info");
        resolverCode.Should().Contain("ctx.request");
        resolverCode.Should().Contain("ctx.stash");
        resolverCode.Should().Contain("ctx.prev");
        // Response handler should propagate errors and return result
        resolverCode.Should().Contain("ctx.error");
        resolverCode.Should().Contain("util.error");
        resolverCode.Should().Contain("ctx.result");
    }

    [Fact]
    public void GenerateManifest_ShouldNotContainUsesLambdaContext()
    {
        // Arrange
        var resolvers = new List<ResolverInfo>
        {
            new ResolverInfo
            {
                TypeName = "Query",
                FieldName = "getProduct",
                Kind = ResolverKind.Unit,
                DataSource = TestDataSource,
                LambdaFunctionName = TestFunctionName,
                LambdaFunctionLogicalId = TestFunctionLogicalId
            },
            new ResolverInfo
            {
                TypeName = "Mutation",
                FieldName = "createOrder",
                Kind = ResolverKind.Pipeline,
                Functions = new List<string> { "ValidateOrder", "CreateOrder" }
            }
        };

        // Act
        var json = ResolverManifestGenerator.GenerateManifest(resolvers);

        // Assert - usesLambdaContext should not appear anywhere in the manifest
        json.Should().NotContain("usesLambdaContext");
    }
}

public class ResolverManifestPropertyTests
{
    private static readonly string[] RequiredContextFields = 
        { "arguments", "source", "identity", "info", "request", "stash", "prev" };

    private static ResolverInfo CreateRandomUnitResolver(string typeName, string fieldName)
    {
        return new ResolverInfo
        {
            TypeName = typeName,
            FieldName = fieldName,
            Kind = ResolverKind.Unit,
            DataSource = $"{fieldName}DataSource",
            LambdaFunctionName = fieldName,
            LambdaFunctionLogicalId = $"{fieldName}Function"
        };
    }

    private static ResolverInfo CreateRandomPipelineResolver(string typeName, string fieldName)
    {
        return new ResolverInfo
        {
            TypeName = typeName,
            FieldName = fieldName,
            Kind = ResolverKind.Pipeline,
            Functions = new List<string> { $"{fieldName}Func1", $"{fieldName}Func2" }
        };
    }

    // Feature: runtime-core-appsync-context, Property 5: All unit resolvers emit full-context resolverCode
    // **Validates: Requirements 7.1, 7.2, 7.3**
    [FsCheck.Xunit.Property(MaxTest = 100)]
    public void AllUnitResolvers_ContainFullContextResolverCode(FsCheck.NonEmptyArray<bool> isUnitFlags)
    {
        // Build a resolver list ensuring at least one unit resolver
        var flags = isUnitFlags.Get;
        var resolvers = new List<ResolverInfo>();
        bool hasUnit = false;

        for (int i = 0; i < flags.Length; i++)
        {
            var typeName = i % 2 == 0 ? "Query" : "Mutation";
            var fieldName = $"field{i}";

            if (flags[i])
            {
                resolvers.Add(CreateRandomUnitResolver(typeName, fieldName));
                hasUnit = true;
            }
            else
            {
                resolvers.Add(CreateRandomPipelineResolver(typeName, fieldName));
            }
        }

        // Ensure at least one unit resolver exists
        if (!hasUnit)
        {
            resolvers.Add(CreateRandomUnitResolver("Query", "ensuredUnit"));
        }

        // Act
        var json = ResolverManifestGenerator.GenerateManifest(resolvers);
        var doc = JsonDocument.Parse(json);
        var resolversArray = doc.RootElement.GetProperty("resolvers");

        // Assert
        foreach (var resolver in resolversArray.EnumerateArray())
        {
            var kind = resolver.GetProperty("kind").GetString();

            // No resolver should have usesLambdaContext
            resolver.TryGetProperty("usesLambdaContext", out _).Should().BeFalse(
                "usesLambdaContext has been removed from the manifest format");

            if (kind == "UNIT")
            {
                // Every unit resolver must have resolverCode
                resolver.TryGetProperty("resolverCode", out var resolverCodeElement).Should().BeTrue(
                    "every unit resolver must have a resolverCode property");

                var resolverCode = resolverCodeElement.GetString();
                resolverCode.Should().NotBeNullOrEmpty();

                // resolverCode must contain all seven context fields
                foreach (var field in RequiredContextFields)
                {
                    resolverCode.Should().Contain($"ctx.{field}",
                        $"resolverCode must include ctx.{field} in the payload");
                }
            }
        }
    }
}
