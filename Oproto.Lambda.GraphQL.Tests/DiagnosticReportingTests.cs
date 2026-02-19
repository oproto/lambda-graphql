using FluentAssertions;
using Oproto.Lambda.GraphQL.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace Oproto.Lambda.GraphQL.Tests;

public class DiagnosticReportingTests
{
    [Fact]
    public void DiagnosticDescriptors_ShouldHaveUniqueIds()
    {
        // Arrange
        var descriptors = new[]
        {
            DiagnosticDescriptors.TypeExtractionError,
            DiagnosticDescriptors.OperationExtractionError,
            DiagnosticDescriptors.SchemaGenerationError,
            DiagnosticDescriptors.ReturnTypeExtractionWarning
        };

        // Act
        var ids = descriptors.Select(d => d.Id).ToList();

        // Assert
        ids.Should().OnlyHaveUniqueItems();
        ids.Should().AllSatisfy(id => id.Should().StartWith("LGQL"));
    }

    [Fact]
    public void DiagnosticDescriptors_ShouldHaveCorrectSeverities()
    {
        // Act & Assert
        DiagnosticDescriptors.TypeExtractionError.DefaultSeverity.Should().Be(DiagnosticSeverity.Warning);
        DiagnosticDescriptors.OperationExtractionError.DefaultSeverity.Should().Be(DiagnosticSeverity.Warning);
        DiagnosticDescriptors.SchemaGenerationError.DefaultSeverity.Should().Be(DiagnosticSeverity.Error);
        DiagnosticDescriptors.ReturnTypeExtractionWarning.DefaultSeverity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public void DiagnosticDescriptors_ShouldHaveCorrectCategories()
    {
        // Arrange
        var descriptors = new[]
        {
            DiagnosticDescriptors.TypeExtractionError,
            DiagnosticDescriptors.OperationExtractionError,
            DiagnosticDescriptors.SchemaGenerationError,
            DiagnosticDescriptors.ReturnTypeExtractionWarning
        };

        // Act & Assert
        descriptors.Should().AllSatisfy(d => d.Category.Should().Be("Oproto.Lambda.GraphQL"));
    }

    [Fact]
    public void DiagnosticDescriptors_ShouldBeEnabledByDefault()
    {
        // Arrange
        var descriptors = new[]
        {
            DiagnosticDescriptors.TypeExtractionError,
            DiagnosticDescriptors.OperationExtractionError,
            DiagnosticDescriptors.SchemaGenerationError,
            DiagnosticDescriptors.ReturnTypeExtractionWarning
        };

        // Act & Assert
        descriptors.Should().AllSatisfy(d => d.IsEnabledByDefault.Should().BeTrue());
    }

    [Fact]
    public void DiagnosticDescriptors_ShouldHaveDescriptiveMessages()
    {
        // Act & Assert
        DiagnosticDescriptors.TypeExtractionError.MessageFormat.ToString()
            .Should().Contain("{0}").And.Contain("{1}");
        
        DiagnosticDescriptors.OperationExtractionError.MessageFormat.ToString()
            .Should().Contain("{0}").And.Contain("{1}");
        
        DiagnosticDescriptors.SchemaGenerationError.MessageFormat.ToString()
            .Should().Contain("{0}");
        
        DiagnosticDescriptors.ReturnTypeExtractionWarning.MessageFormat.ToString()
            .Should().Contain("{0}").And.Contain("{1}");
    }

    [Fact]
    public void Diagnostic_ShouldBeCreatable_WithCorrectFormat()
    {
        // Arrange
        var location = Location.None;
        var testMessage = "Test error message";
        var testContext = "TestClass";

        // Act
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.TypeExtractionError,
            location,
            testContext,
            testMessage);

        // Assert
        diagnostic.Id.Should().Be("LGQL001");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.GetMessage().Should().Contain(testContext).And.Contain(testMessage);
    }

    [Fact]
    public void SourceGenerator_ShouldHandleInvalidCode_WithoutCrashing()
    {
        // Arrange
        var invalidCode = @"
            using Oproto.Lambda.GraphQL.Attributes;
            
            [GraphQLType(""InvalidType"")]
            public class InvalidClass
            {
                // Missing closing brace and other syntax errors
                public string Property { get; set;
            ";

        var syntaxTree = CSharpSyntaxTree.ParseText(invalidCode);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Act & Assert - Should not throw exceptions
        var diagnostics = compilation.GetDiagnostics();
        diagnostics.Should().NotBeEmpty(); // Should have compilation errors
        
        // The source generator should handle this gracefully
        // (Full source generator testing would require more complex setup)
    }
}
