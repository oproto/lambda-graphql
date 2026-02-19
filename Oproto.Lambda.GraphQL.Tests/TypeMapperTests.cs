using FluentAssertions;
using Oproto.Lambda.GraphQL.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Oproto.Lambda.GraphQL.Tests;

public class TypeMapperTests
{
    [Fact]
    public void MapType_ShouldMapBasicTypes()
    {
        // Arrange
        var compilation = CreateCompilation("public class Test { public string Name { get; set; } }");
        var stringType = compilation.GetSpecialType(SpecialType.System_String);
        var intType = compilation.GetSpecialType(SpecialType.System_Int32);
        var boolType = compilation.GetSpecialType(SpecialType.System_Boolean);

        // Act & Assert
        TypeMapper.MapType(stringType).Should().Be("String");
        TypeMapper.MapType(intType).Should().Be("Int");
        TypeMapper.MapType(boolType).Should().Be("Boolean");
    }

    [Fact]
    public void IsNonNull_ShouldHandleValueTypes()
    {
        // Arrange
        var compilation = CreateCompilation("public class Test { public int Id { get; set; } }");
        var intType = compilation.GetSpecialType(SpecialType.System_Int32);

        // Act & Assert
        TypeMapper.IsNonNull(intType).Should().BeTrue();
    }

    private static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
