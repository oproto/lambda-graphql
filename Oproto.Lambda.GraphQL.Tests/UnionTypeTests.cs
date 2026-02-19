using FluentAssertions;
using Oproto.Lambda.GraphQL.SourceGenerator;
using Oproto.Lambda.GraphQL.SourceGenerator.Models;
using System.Collections.Generic;
using Xunit;

namespace Oproto.Lambda.GraphQL.Tests;

public class UnionTypeTests
{
    [Fact]
    public void GenerateSchema_ShouldRenderUnionTypes()
    {
        // Arrange
        var types = new List<TypeInfo>
        {
            new TypeInfo
            {
                Name = "SearchResult",
                Kind = TypeKind.Union,
                UnionMembers = new List<string> { "Product", "User", "Order" },
                Description = "Search result that can be a product, user, or order"
            }
        };

        // Act
        var sdl = SdlGenerator.GenerateSchema(types, new List<ResolverInfo>());

        // Assert
        sdl.Should().Contain("union SearchResult = Product | User | Order");
        sdl.Should().Contain("Search result that can be a product, user, or order");
    }

    [Fact]
    public void GenerateSchema_ShouldRenderUnionTypeWithoutMembers()
    {
        // Arrange
        var types = new List<TypeInfo>
        {
            new TypeInfo
            {
                Name = "EmptyUnion",
                Kind = TypeKind.Union,
                UnionMembers = new List<string>()
            }
        };

        // Act
        var sdl = SdlGenerator.GenerateSchema(types, new List<ResolverInfo>());

        // Assert
        sdl.Should().Contain("union EmptyUnion");
        sdl.Should().NotContain("union EmptyUnion =");
    }

    [Fact]
    public void GenerateSchema_ShouldRenderUnionTypeWithSingleMember()
    {
        // Arrange
        var types = new List<TypeInfo>
        {
            new TypeInfo
            {
                Name = "SingleMemberUnion",
                Kind = TypeKind.Union,
                UnionMembers = new List<string> { "Product" }
            }
        };

        // Act
        var sdl = SdlGenerator.GenerateSchema(types, new List<ResolverInfo>());

        // Assert
        sdl.Should().Contain("union SingleMemberUnion = Product");
    }
}
