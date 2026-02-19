using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Oproto.Lambda.GraphQL.SourceGenerator;

namespace Oproto.Lambda.GraphQL.Tests.Runtime.SourceGenerator;

public class GraphQLFieldMapGeneratorTests
{
    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var attributeAssembly = typeof(Oproto.Lambda.GraphQL.Attributes.GraphQLTypeAttribute).Assembly;

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(attributeAssembly.Location)
        };

        // Add core runtime assemblies needed for compilation
        var runtimeDir = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        foreach (var dllName in new[] { "System.Runtime.dll", "netstandard.dll" })
        {
            var path = System.IO.Path.Combine(runtimeDir, dllName);
            if (System.IO.File.Exists(path))
                references.Add(MetadataReference.CreateFromFile(path));
        }

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new GraphQLSchemaGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        return driver.RunGenerators(compilation).GetRunResult();
    }

    [Fact]
    public void PartialClass_WithRenamedField_EmitsBuilderMap()
    {
        var source = @"
using Oproto.Lambda.GraphQL.Attributes;

namespace TestNamespace;

[GraphQLType]
public partial class Product
{
    public string Id { get; set; }

    [GraphQLField(""displayName"")]
    public string Name { get; set; }

    public decimal Price { get; set; }
}";

        var result = RunGenerator(source);

        var fieldMapSource = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Product.GraphQLFieldMap.g.cs"));

        fieldMapSource.Should().NotBeNull("a GraphQLFieldMap file should be generated for a partial class with renamed fields");

        var text = fieldMapSource!.GetText().ToString();
        text.Should().Contain("public partial class Product");
        text.Should().Contain("namespace TestNamespace;");
        text.Should().Contain(".Builder()");
        text.Should().Contain(".Map(\"displayName\", \"Name\")");
        text.Should().Contain(".Build()");
        text.Should().Contain("Oproto.Lambda.GraphQL.Runtime.FieldNameMap");
    }

    [Fact]
    public void PartialClass_WithAllMatchingNames_EmitsIdentity()
    {
        var source = @"
using Oproto.Lambda.GraphQL.Attributes;

namespace TestNamespace;

[GraphQLType]
public partial class SimpleType
{
    public string Id { get; set; }
    public string Name { get; set; }
}";

        var result = RunGenerator(source);

        var fieldMapSource = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("SimpleType.GraphQLFieldMap.g.cs"));

        fieldMapSource.Should().NotBeNull("a GraphQLFieldMap file should be generated even when all names match");

        var text = fieldMapSource!.GetText().ToString();
        text.Should().Contain("public partial class SimpleType");
        text.Should().Contain("FieldNameMap.Identity");
        text.Should().NotContain(".Builder()");
        text.Should().NotContain(".Map(");
    }

    [Fact]
    public void NonPartialClass_EmitsDiagnosticWarning_NoFieldMap()
    {
        var source = @"
using Oproto.Lambda.GraphQL.Attributes;

namespace TestNamespace;

[GraphQLType]
public class NonPartialProduct
{
    public string Id { get; set; }

    [GraphQLField(""displayName"")]
    public string Name { get; set; }
}";

        var result = RunGenerator(source);

        var fieldMapSource = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("NonPartialProduct.GraphQLFieldMap.g.cs"));

        fieldMapSource.Should().BeNull("no GraphQLFieldMap should be generated for a non-partial class");

        result.Diagnostics.Should().Contain(d =>
            d.Id == "LGQL005" &&
            d.GetMessage().Contains("NonPartialProduct"));
    }

    [Fact]
    public void EnumType_DoesNotGetFieldMap()
    {
        var source = @"
using Oproto.Lambda.GraphQL.Attributes;

namespace TestNamespace;

[GraphQLType]
public enum Status
{
    Active,
    Inactive
}";

        var result = RunGenerator(source);

        var fieldMapSource = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Status.GraphQLFieldMap.g.cs"));

        fieldMapSource.Should().BeNull("enums should not get a GraphQLFieldMap");
    }

    [Fact]
    public void InputType_DoesNotGetFieldMap()
    {
        var source = @"
using Oproto.Lambda.GraphQL.Attributes;

namespace TestNamespace;

[GraphQLType(Kind = GraphQLTypeKind.Input)]
public partial class CreateProductInput
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}";

        var result = RunGenerator(source);

        var fieldMapSource = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("CreateProductInput.GraphQLFieldMap.g.cs"));

        fieldMapSource.Should().BeNull("input types should not get a GraphQLFieldMap");
    }

    [Fact]
    public void GeneratedCode_UsesFullyQualifiedTypeNames()
    {
        var source = @"
using Oproto.Lambda.GraphQL.Attributes;

namespace TestNamespace;

[GraphQLType]
public partial class Widget
{
    [GraphQLField(""widgetName"")]
    public string Name { get; set; }
}";

        var result = RunGenerator(source);

        var fieldMapSource = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Widget.GraphQLFieldMap.g.cs"));

        fieldMapSource.Should().NotBeNull();

        var text = fieldMapSource!.GetText().ToString();

        // Verify fully qualified type names (no using statements for Runtime)
        text.Should().Contain("Oproto.Lambda.GraphQL.Runtime.FieldNameMap");
        text.Should().NotContain("using Oproto.Lambda.GraphQL.Runtime;");
    }

    [Fact]
    public void PartialClass_WithMultipleRenamedFields_EmitsAllMappings()
    {
        var source = @"
using Oproto.Lambda.GraphQL.Attributes;

namespace TestNamespace;

[GraphQLType]
public partial class Customer
{
    public string Id { get; set; }

    [GraphQLField(""displayName"")]
    public string Name { get; set; }

    [GraphQLField(""emailAddress"")]
    public string Email { get; set; }

    public int Age { get; set; }
}";

        var result = RunGenerator(source);

        var fieldMapSource = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Customer.GraphQLFieldMap.g.cs"));

        fieldMapSource.Should().NotBeNull();

        var text = fieldMapSource!.GetText().ToString();
        text.Should().Contain(".Map(\"displayName\", \"Name\")");
        text.Should().Contain(".Map(\"emailAddress\", \"Email\")");
        // Non-renamed fields should NOT appear in the map
        text.Should().NotContain("\"Id\"");
        text.Should().NotContain("\"Age\"");
    }

    [Fact]
    public void GeneratedCode_ContainsAutoGeneratedComment()
    {
        var source = @"
using Oproto.Lambda.GraphQL.Attributes;

namespace TestNamespace;

[GraphQLType]
public partial class Item
{
    public string Id { get; set; }
}";

        var result = RunGenerator(source);

        var fieldMapSource = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Item.GraphQLFieldMap.g.cs"));

        fieldMapSource.Should().NotBeNull();

        var text = fieldMapSource!.GetText().ToString();
        text.Should().Contain("// <auto-generated/>");
    }
}
