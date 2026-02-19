using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Oproto.Lambda.GraphQL.Runtime;

namespace Oproto.Lambda.GraphQL.Tests.Runtime;

public class ResponseShaperTests
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    #region Test Types

    public record TestCategory(string Name, string Description);

    public record TestProduct(string Id, string Name, decimal Price, TestCategory? Category);

    public record TestProductWithJsonAttr
    {
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("display_name")]
        public string Name { get; init; } = string.Empty;

        public decimal Price { get; init; }
    }

    #endregion

    [Fact]
    public void ShapeResponse_WithAll_ReturnsFullJson()
    {
        var product = new TestProduct("1", "Widget", 9.99m, null);

        var result = ResponseShaper.ShapeResponse(product, FieldSelection.All(), CamelCaseOptions);

        var expected = JsonSerializer.Serialize(product, CamelCaseOptions);
        result.Should().Be(expected);
    }

    [Fact]
    public void ShapeResponse_WithSelectedFields_FiltersOutUnselected()
    {
        var product = new TestProduct("1", "Widget", 9.99m, null);
        var selection = FieldSelection.Of("Id", "Name");

        var result = ResponseShaper.ShapeResponse(product, selection, CamelCaseOptions);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        root.TryGetProperty("id", out _).Should().BeTrue();
        root.TryGetProperty("name", out _).Should().BeTrue();
        root.TryGetProperty("price", out _).Should().BeFalse();
        root.TryGetProperty("category", out _).Should().BeFalse();
    }

    [Fact]
    public void ShapeResponse_WithNestedFieldSelection_FiltersNestedProperties()
    {
        var product = new TestProduct("1", "Widget", 9.99m,
            new TestCategory("Electronics", "Electronic devices"));

        // Request id, and only category/name (not category/description)
        var selectionSet = new List<string> { "id", "category/name" };

        // We need to map camelCase JSON names to PascalCase C# names
        // FromSelectionSet uses the raw names, so we use PascalCase C# names
        var selection = FieldSelection.FromSelectionSet(
            new List<string> { "Id", "Category/Name" });

        var result = ResponseShaper.ShapeResponse(product, selection, CamelCaseOptions);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        root.TryGetProperty("id", out _).Should().BeTrue();
        root.TryGetProperty("category", out var category).Should().BeTrue();
        category.TryGetProperty("name", out _).Should().BeTrue();
        category.TryGetProperty("description", out _).Should().BeFalse();
        root.TryGetProperty("name", out _).Should().BeFalse();
        root.TryGetProperty("price", out _).Should().BeFalse();
    }

    [Fact]
    public void ShapeResponse_NullValue_ReturnsNullLiteral()
    {
        TestProduct? product = null;
        var result = ResponseShaper.ShapeResponse(product, FieldSelection.All(), CamelCaseOptions);

        result.Should().Be("null");
    }

    [Fact]
    public void ShapeResponse_CamelCaseNamingPolicy_MatchesCSharpNames()
    {
        var product = new TestProduct("1", "Widget", 9.99m, null);
        var selection = FieldSelection.Of("Name");

        var result = ResponseShaper.ShapeResponse(product, selection, CamelCaseOptions);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        // "Name" in FieldSelection should match "name" in camelCase JSON
        root.TryGetProperty("name", out var nameVal).Should().BeTrue();
        nameVal.GetString().Should().Be("Widget");
        root.TryGetProperty("id", out _).Should().BeFalse();
        root.TryGetProperty("price", out _).Should().BeFalse();
    }

    [Fact]
    public void ShapeResponse_WithJsonPropertyNameAttribute_MatchesCSharpName()
    {
        var product = new TestProductWithJsonAttr { Id = "1", Name = "Widget", Price = 9.99m };
        var selection = FieldSelection.Of("Id", "Name");

        var result = ResponseShaper.ShapeResponse(product, selection, CamelCaseOptions);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        root.TryGetProperty("id", out _).Should().BeTrue();
        // "Name" C# property serializes as "display_name" due to [JsonPropertyName]
        root.TryGetProperty("display_name", out var displayName).Should().BeTrue();
        displayName.GetString().Should().Be("Widget");
        root.TryGetProperty("price", out _).Should().BeFalse();
    }

    [Fact]
    public void ShapeResponse_NullSelection_ThrowsArgumentNullException()
    {
        var product = new TestProduct("1", "Widget", 9.99m, null);

        var act = () => ResponseShaper.ShapeResponse(product, null!, CamelCaseOptions);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ShapeResponse_NullOptions_ThrowsArgumentNullException()
    {
        var product = new TestProduct("1", "Widget", 9.99m, null);

        var act = () => ResponseShaper.ShapeResponse(product, FieldSelection.All(), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ShapeResponse_DefaultOverload_UsesCamelCase()
    {
        var product = new TestProduct("1", "Widget", 9.99m, null);
        var selection = FieldSelection.Of("Id");

        var result = ResponseShaper.ShapeResponse(product, selection);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        root.TryGetProperty("id", out var idVal).Should().BeTrue();
        idVal.GetString().Should().Be("1");
        root.TryGetProperty("name", out _).Should().BeFalse();
    }

    [Fact]
    public void ShapeResponse_NoNamingPolicy_UsesOriginalPropertyNames()
    {
        var product = new TestProduct("1", "Widget", 9.99m, null);
        var selection = FieldSelection.Of("Id", "Name");
        var options = new JsonSerializerOptions(); // No naming policy

        var result = ResponseShaper.ShapeResponse(product, selection, options);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        root.TryGetProperty("Id", out _).Should().BeTrue();
        root.TryGetProperty("Name", out _).Should().BeTrue();
        root.TryGetProperty("Price", out _).Should().BeFalse();
    }
}
