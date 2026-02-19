using FluentAssertions;
using Oproto.Lambda.GraphQL.SourceGenerator;
using Oproto.Lambda.GraphQL.SourceGenerator.Models;
using System.Collections.Generic;

namespace Oproto.Lambda.GraphQL.Tests;

public class SdlGeneratorTests
{
    [Fact]
    public void GenerateSchema_ShouldCreateValidSDL()
    {
        // Arrange
        var types = new List<Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo>
        {
            new Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo
            {
                Name = "Product",
                Description = "A product",
                Kind = Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeKind.Object,
                Fields = new List<FieldInfo>
                {
                    new FieldInfo { Name = "id", Type = "ID", IsNullable = false },
                    new FieldInfo { Name = "name", Type = "String", IsNullable = false }
                }
            }
        };

        var operations = new List<ResolverInfo>
        {
            new ResolverInfo
            {
                TypeName = "Query",
                FieldName = "getProduct",
                DataSource = "ProductsLambda",
                ReturnType = "Product"
            }
        };

        // Act
        var sdl = SdlGenerator.GenerateSchema(types, operations, "TestAPI", "Test API");

        // Assert
        sdl.Should().Contain("schema {");
        sdl.Should().Contain("query: Query");
        sdl.Should().Contain("type Product {");
        sdl.Should().Contain("id: ID!");
        sdl.Should().Contain("name: String!");
        sdl.Should().Contain("type Query {");
        sdl.Should().Contain("getProduct: Product"); // Should use actual return type
    }

    [Fact]
    public void GenerateSchema_ShouldHandleEnumTypes()
    {
        // Arrange
        var types = new List<Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo>
        {
            new Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo
            {
                Name = "Status",
                Kind = Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeKind.Enum,
                IsEnum = true,
                EnumValues = new List<EnumValueInfo>
                {
                    new EnumValueInfo { Name = "ACTIVE", Description = "Active status" },
                    new EnumValueInfo { Name = "INACTIVE", IsDeprecated = true, DeprecationReason = "Use DISABLED" }
                }
            }
        };

        // Act
        var sdl = SdlGenerator.GenerateSchema(types, new List<ResolverInfo>());

        // Assert
        sdl.Should().Contain("enum Status {");
        sdl.Should().Contain("ACTIVE");
        sdl.Should().Contain("INACTIVE @deprecated(reason: \"Use DISABLED\")");
    }

    [Fact]
    public void GenerateSchema_ShouldRenderInputTypes()
    {
        // Arrange
        var types = new List<Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo>
        {
            new Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo
            {
                Name = "CreateProductInput",
                Kind = Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeKind.Input,
                Fields = new List<FieldInfo>
                {
                    new FieldInfo { Name = "name", Type = "String", IsNullable = false },
                    new FieldInfo { Name = "price", Type = "Float", IsNullable = false }
                }
            }
        };

        // Act
        var sdl = SdlGenerator.GenerateSchema(types, new List<ResolverInfo>());

        // Assert
        sdl.Should().Contain("input CreateProductInput {");
        sdl.Should().Contain("name: String!");
        sdl.Should().Contain("price: Float!");
        sdl.Should().NotContain("type CreateProductInput"); // Should NOT be 'type'
    }

    [Fact]
    public void GenerateSchema_ShouldRenderOperationArguments()
    {
        // Arrange
        var operations = new List<ResolverInfo>
        {
            new ResolverInfo
            {
                TypeName = "Query",
                FieldName = "getProduct",
                ReturnType = "Product",
                Arguments = new List<ArgumentInfo>
                {
                    new ArgumentInfo { Name = "id", Type = "ID", IsNullable = false }
                }
            }
        };

        // Act
        var sdl = SdlGenerator.GenerateSchema(new List<Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo>(), operations);

        // Assert
        sdl.Should().Contain("getProduct(id: ID!): Product");
    }

    [Fact]
    public void GenerateSchema_ShouldRenderMultipleArguments()
    {
        // Arrange
        var operations = new List<ResolverInfo>
        {
            new ResolverInfo
            {
                TypeName = "Query",
                FieldName = "listProducts",
                ReturnType = "[Product]!",
                Arguments = new List<ArgumentInfo>
                {
                    new ArgumentInfo { Name = "first", Type = "Int", IsNullable = true },
                    new ArgumentInfo { Name = "after", Type = "String", IsNullable = true }
                }
            }
        };

        // Act
        var sdl = SdlGenerator.GenerateSchema(new List<Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo>(), operations);

        // Assert
        sdl.Should().Contain("listProducts(");
        sdl.Should().Contain("first: Int");
        sdl.Should().Contain("after: String");
        sdl.Should().Contain("): [Product]!");
    }

    [Fact]
    public void GenerateSchema_ShouldRenderOperationDescriptions()
    {
        // Arrange
        var operations = new List<ResolverInfo>
        {
            new ResolverInfo
            {
                TypeName = "Query",
                FieldName = "getProduct",
                Description = "Get a product by ID",
                ReturnType = "Product"
            }
        };

        // Act
        var sdl = SdlGenerator.GenerateSchema(new List<Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo>(), operations);

        // Assert
        sdl.Should().Contain("\"\"\"");
        sdl.Should().Contain("Get a product by ID");
    }

    [Fact]
    public void GenerateSchema_ShouldRenderInterfaceTypes()
    {
        // Arrange
        var types = new List<Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo>
        {
            new Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo
            {
                Name = "Node",
                Kind = Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeKind.Interface,
                IsInterface = true,
                Fields = new List<FieldInfo>
                {
                    new FieldInfo { Name = "id", Type = "ID", IsNullable = false }
                }
            }
        };

        // Act
        var sdl = SdlGenerator.GenerateSchema(types, new List<ResolverInfo>());

        // Assert
        sdl.Should().Contain("interface Node {");
        sdl.Should().Contain("id: ID!");
    }

    [Fact]
    public void GenerateSchema_ShouldRenderAuthDirectivesOnTypes()
    {
        // Arrange
        var types = new List<Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo>
        {
            new Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo
            {
                Name = "User",
                Kind = Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeKind.Object,
                Fields = new List<FieldInfo>
                {
                    new FieldInfo { Name = "id", Type = "ID", IsNullable = false }
                },
                Directives = new List<AppliedDirectiveInfo>
                {
                    new AppliedDirectiveInfo { Name = "aws_cognito_user_pools" }
                }
            }
        };

        // Act
        var sdl = SdlGenerator.GenerateSchema(types, new List<ResolverInfo>());

        // Assert
        sdl.Should().Contain("type User @aws_cognito_user_pools {");
    }

    [Fact]
    public void GenerateSchema_ShouldRenderAuthDirectivesOnOperations()
    {
        // Arrange
        var operations = new List<ResolverInfo>
        {
            new ResolverInfo
            {
                TypeName = "Query",
                FieldName = "getUser",
                ReturnType = "User!",
                Directives = new List<AppliedDirectiveInfo>
                {
                    new AppliedDirectiveInfo { Name = "aws_iam" }
                }
            }
        };

        // Act
        var sdl = SdlGenerator.GenerateSchema(new List<Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo>(), operations);

        // Assert
        sdl.Should().Contain("getUser: User! @aws_iam");
    }

    [Fact]
    public void GenerateSchema_ShouldRenderAuthDirectivesWithCognitoGroups()
    {
        // Arrange
        var operations = new List<ResolverInfo>
        {
            new ResolverInfo
            {
                TypeName = "Mutation",
                FieldName = "createUser",
                ReturnType = "User!",
                Directives = new List<AppliedDirectiveInfo>
                {
                    new AppliedDirectiveInfo 
                    { 
                        Name = "aws_cognito_user_pools",
                        Arguments = new Dictionary<string, string> { { "cognito_groups", "admin" } }
                    }
                }
            }
        };

        // Act
        var sdl = SdlGenerator.GenerateSchema(new List<Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo>(), operations);

        // Assert
        sdl.Should().Contain("createUser: User! @aws_cognito_user_pools(cognito_groups: [\"admin\"])");
    }

    [Fact]
    public void GenerateSchema_ShouldNotRenderAuthDirectivesOnEnums()
    {
        // Arrange - AppSync does not support auth directives on enums
        var types = new List<Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo>
        {
            new Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo
            {
                Name = "OrderStatus",
                Kind = Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeKind.Enum,
                IsEnum = true,
                EnumValues = new List<EnumValueInfo>
                {
                    new EnumValueInfo { Name = "PENDING" },
                    new EnumValueInfo { Name = "COMPLETED" }
                },
                Directives = new List<AppliedDirectiveInfo>
                {
                    new AppliedDirectiveInfo { Name = "aws_api_key" }
                }
            }
        };

        // Act
        var sdl = SdlGenerator.GenerateSchema(types, new List<ResolverInfo>());

        // Assert - directive should be omitted
        sdl.Should().Contain("enum OrderStatus {");
        sdl.Should().NotContain("@aws_api_key");
    }

    [Fact]
    public void GenerateSchema_ShouldRenderUnionTypes()
    {
        // Arrange
        var types = new List<Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo>
        {
            new Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeInfo
            {
                Name = "SearchResult",
                Kind = Oproto.Lambda.GraphQL.SourceGenerator.Models.TypeKind.Union,
                UnionMembers = new List<string> { "Product", "User", "Order" }
            }
        };

        // Act
        var sdl = SdlGenerator.GenerateSchema(types, new List<ResolverInfo>());

        // Assert
        sdl.Should().Contain("union SearchResult = Product | User | Order");
    }
}
