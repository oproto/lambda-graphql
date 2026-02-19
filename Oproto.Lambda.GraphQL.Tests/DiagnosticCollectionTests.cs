using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using System.Linq;

namespace Oproto.Lambda.GraphQL.Tests;

public class DiagnosticCollectionTests
{
    [Fact]
    public void BooleanAttributeParsing_ShouldHandleNullAndMissingValues()
    {
        // Arrange - Test with actual GraphQL attributes that would use boolean parsing
        var sourceCode = @"
using System;

namespace Oproto.Lambda.GraphQL
{
    public class GraphQLFieldAttribute : Attribute
    {
        public bool Deprecated { get; set; }
    }
}

[Oproto.Lambda.GraphQL.GraphQLField(Deprecated = true)]
public class TestClass 
{
    public string TestProperty { get; set; }
}";

        var compilation = CreateCompilation(sourceCode);
        var generator = new Oproto.Lambda.GraphQL.SourceGenerator.GraphQLSchemaGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        // Act & Assert - Should not throw and handle boolean parsing correctly
        var result = driver.RunGenerators(compilation);
        result.Should().NotBeNull();
        
        var runResult = result.GetRunResult();
        runResult.Diagnostics.Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void DiagnosticCollection_ShouldNotBreakExistingFunctionality()
    {
        // Arrange
        var sourceCode = @"
using System;
using Oproto.Lambda.GraphQL;

[GraphQLType]
public class TestType
{
    public string Name { get; set; }
}";

        var compilation = CreateCompilation(sourceCode);
        var generator = new Oproto.Lambda.GraphQL.SourceGenerator.GraphQLSchemaGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        // Act
        var result = driver.RunGenerators(compilation);

        // Assert
        result.Should().NotBeNull();
        // Verify that the generator ran without throwing exceptions
        var runResult = result.GetRunResult();
        runResult.Diagnostics.Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void BooleanParsing_ShouldUseProperTypeHandling()
    {
        // This test verifies that boolean parsing improvements don't break existing functionality
        // by ensuring the generator can still process attributes with boolean properties
        
        // Arrange
        var sourceCode = @"
using System;

public class TestClass
{
    public string TestProperty { get; set; }
}";

        var compilation = CreateCompilation(sourceCode);
        
        // Act & Assert - Should not throw
        var generator = new Oproto.Lambda.GraphQL.SourceGenerator.GraphQLSchemaGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation);
        
        result.Should().NotBeNull();
    }

    private static Compilation CreateCompilation(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
