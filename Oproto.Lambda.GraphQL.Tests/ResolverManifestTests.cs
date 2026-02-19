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
}
