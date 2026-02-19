using FluentAssertions;
using Oproto.Lambda.GraphQL.Attributes;

namespace Oproto.Lambda.GraphQL.Tests;

public class AttributeTests
{
    [Fact]
    public void GraphQLTypeAttribute_ShouldSetNameAndDescription()
    {
        // Arrange & Act
        var attribute = new GraphQLTypeAttribute("Product")
        {
            Description = "A product type",
            Kind = GraphQLTypeKind.Object
        };

        // Assert
        attribute.Name.Should().Be("Product");
        attribute.Description.Should().Be("A product type");
        attribute.Kind.Should().Be(GraphQLTypeKind.Object);
    }

    [Fact]
    public void GraphQLTypeAttribute_ShouldSupportInputKind()
    {
        // Arrange & Act
        var attribute = new GraphQLTypeAttribute("CreateProductInput")
        {
            Kind = GraphQLTypeKind.Input
        };

        // Assert
        attribute.Kind.Should().Be(GraphQLTypeKind.Input);
    }

    [Fact]
    public void GraphQLFieldAttribute_ShouldSetNameAndDescription()
    {
        // Arrange & Act
        var attribute = new GraphQLFieldAttribute("productName")
        {
            Description = "The name of the product"
        };

        // Assert
        attribute.Name.Should().Be("productName");
        attribute.Description.Should().Be("The name of the product");
    }

    [Fact]
    public void GraphQLQueryAttribute_ShouldSetNameAndDescription()
    {
        // Arrange & Act
        var attribute = new GraphQLQueryAttribute("getProduct")
        {
            Description = "Get a product by ID"
        };

        // Assert
        attribute.Name.Should().Be("getProduct");
        attribute.Description.Should().Be("Get a product by ID");
    }

    [Fact]
    public void GraphQLMutationAttribute_ShouldSetNameAndDescription()
    {
        // Arrange & Act
        var attribute = new GraphQLMutationAttribute("createProduct")
        {
            Description = "Create a new product"
        };

        // Assert
        attribute.Name.Should().Be("createProduct");
        attribute.Description.Should().Be("Create a new product");
    }

    [Fact]
    public void GraphQLSchemaAttribute_ShouldSetNameAndDescription()
    {
        // Arrange & Act
        var attribute = new GraphQLSchemaAttribute("TestAPI")
        {
            Description = "Test GraphQL API",
            Version = "1.0.0"
        };

        // Assert
        attribute.Name.Should().Be("TestAPI");
        attribute.Description.Should().Be("Test GraphQL API");
        attribute.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void GraphQLArgumentAttribute_ShouldSetNameAndDescription()
    {
        // Arrange & Act
        var attribute = new GraphQLArgumentAttribute("id")
        {
            Description = "Product identifier"
        };

        // Assert
        attribute.Name.Should().Be("id");
        attribute.Description.Should().Be("Product identifier");
    }

    [Fact]
    public void GraphQLIgnoreAttribute_ShouldBeMarkerAttribute()
    {
        // Arrange & Act
        var attribute = new GraphQLIgnoreAttribute();

        // Assert
        attribute.Should().NotBeNull();
    }

    [Fact]
    public void GraphQLNonNullAttribute_ShouldBeMarkerAttribute()
    {
        // Arrange & Act
        var attribute = new GraphQLNonNullAttribute();

        // Assert
        attribute.Should().NotBeNull();
    }

    [Fact]
    public void GraphQLEnumValueAttribute_ShouldSetNameAndDescription()
    {
        // Arrange & Act
        var attribute = new GraphQLEnumValueAttribute("ACTIVE")
        {
            Description = "Active status",
            Deprecated = true,
            DeprecationReason = "Use ENABLED instead"
        };

        // Assert
        attribute.Name.Should().Be("ACTIVE");
        attribute.Description.Should().Be("Active status");
        attribute.Deprecated.Should().BeTrue();
        attribute.DeprecationReason.Should().Be("Use ENABLED instead");
    }

    [Fact]
    public void GraphQLResolverAttribute_ShouldSetUnitResolverProperties()
    {
        // Arrange & Act
        var attribute = new GraphQLResolverAttribute
        {
            DataSource = "ProductsLambda",
            Kind = ResolverKind.Unit,
            RequestMapping = "request.vtl",
            ResponseMapping = "response.vtl"
        };

        // Assert
        attribute.DataSource.Should().Be("ProductsLambda");
        attribute.Kind.Should().Be(ResolverKind.Unit);
        attribute.RequestMapping.Should().Be("request.vtl");
        attribute.ResponseMapping.Should().Be("response.vtl");
    }

    [Fact]
    public void GraphQLResolverAttribute_ShouldSetPipelineResolverProperties()
    {
        // Arrange & Act
        var attribute = new GraphQLResolverAttribute
        {
            Kind = ResolverKind.Pipeline,
            Functions = new[] { "ValidateInput", "ProcessOrder", "SendNotification" }
        };

        // Assert
        attribute.Kind.Should().Be(ResolverKind.Pipeline);
        attribute.Functions.Should().HaveCount(3);
        attribute.Functions.Should().Contain("ValidateInput");
        attribute.Functions.Should().Contain("ProcessOrder");
        attribute.Functions.Should().Contain("SendNotification");
    }

    [Fact]
    public void GraphQLResolverAttribute_ShouldDefaultToUnitKind()
    {
        // Arrange & Act
        var attribute = new GraphQLResolverAttribute();

        // Assert
        attribute.Kind.Should().Be(ResolverKind.Unit);
    }
}
