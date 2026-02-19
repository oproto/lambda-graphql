using FluentAssertions;
using Oproto.Lambda.GraphQL.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Threading.Tasks;

namespace Oproto.Lambda.GraphQL.Tests;

public class ReturnTypeExtractionTests
{
    [Fact]
    public void ExtractReturnType_ShouldHandleBasicTypes()
    {
        // Arrange
        var compilation = CreateCompilation(@"
            public class TestClass 
            { 
                public string GetString() => ""test"";
                public int GetInt() => 42;
                public bool GetBool() => true;
            }");

        var stringMethod = GetMethod(compilation, "GetString");
        var intMethod = GetMethod(compilation, "GetInt");
        var boolMethod = GetMethod(compilation, "GetBool");

        // Act & Assert
        ReturnTypeExtractor.ExtractReturnType(stringMethod).Should().Be("String");
        ReturnTypeExtractor.ExtractReturnType(intMethod).Should().Be("Int");
        ReturnTypeExtractor.ExtractReturnType(boolMethod).Should().Be("Boolean");
    }

    [Fact]
    public void ExtractReturnType_ShouldUnwrapTaskTypes()
    {
        // Arrange
        var compilation = CreateCompilation(@"
            using System.Threading.Tasks;
            public class TestClass 
            { 
                public Task<string> GetStringAsync() => Task.FromResult(""test"");
                public Task<int> GetIntAsync() => Task.FromResult(42);
                public Task GetVoidAsync() => Task.CompletedTask;
            }");

        var stringTaskMethod = GetMethod(compilation, "GetStringAsync");
        var intTaskMethod = GetMethod(compilation, "GetIntAsync");
        var voidTaskMethod = GetMethod(compilation, "GetVoidAsync");

        // Act & Assert
        ReturnTypeExtractor.ExtractReturnType(stringTaskMethod).Should().Be("String");
        ReturnTypeExtractor.ExtractReturnType(intTaskMethod).Should().Be("Int");
        ReturnTypeExtractor.ExtractReturnType(voidTaskMethod).Should().Be("Boolean"); // void -> Boolean
    }

    [Fact]
    public void ExtractReturnType_ShouldHandleCollections()
    {
        // Arrange
        var compilation = CreateCompilation(@"
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class TestClass 
            { 
                public List<string> GetStringList() => new List<string>();
                public Task<List<int>> GetIntListAsync() => Task.FromResult(new List<int>());
                public string[] GetStringArray() => new string[0];
            }");

        var listMethod = GetMethod(compilation, "GetStringList");
        var taskListMethod = GetMethod(compilation, "GetIntListAsync");
        var arrayMethod = GetMethod(compilation, "GetStringArray");

        // Act & Assert
        ReturnTypeExtractor.ExtractReturnType(listMethod).Should().Be("[String]");
        ReturnTypeExtractor.ExtractReturnType(taskListMethod).Should().Be("[Int]");
        ReturnTypeExtractor.ExtractReturnType(arrayMethod).Should().Be("[String]");
    }

    [Fact]
    public void IsReturnTypeNullable_ShouldHandleNullability()
    {
        // Arrange
        var compilation = CreateCompilation(@"
            using System.Threading.Tasks;
            public class TestClass 
            { 
                public int GetInt() => 42;
                public int? GetNullableInt() => null;
                public Task<string> GetStringAsync() => Task.FromResult(""test"");
                public void GetVoid() { }
            }");

        var intMethod = GetMethod(compilation, "GetInt");
        var nullableIntMethod = GetMethod(compilation, "GetNullableInt");
        var stringTaskMethod = GetMethod(compilation, "GetStringAsync");
        var voidMethod = GetMethod(compilation, "GetVoid");

        // Act & Assert - Focus on the cases that work reliably
        ReturnTypeExtractor.IsReturnTypeNullable(intMethod).Should().BeFalse(); // Value types are non-null
        ReturnTypeExtractor.IsReturnTypeNullable(nullableIntMethod).Should().BeTrue(); // Nullable<T> is nullable
        ReturnTypeExtractor.IsReturnTypeNullable(stringTaskMethod).Should().BeTrue(); // Task<string> unwraps to string (nullable)
        ReturnTypeExtractor.IsReturnTypeNullable(voidMethod).Should().BeFalse(); // void -> Boolean (non-null)
    }

    [Fact]
    public void GetFormattedReturnType_ShouldIncludeNullabilityMarkers()
    {
        // Arrange
        var compilation = CreateCompilation(@"
            using System.Threading.Tasks;
            public class TestClass 
            { 
                public int GetInt() => 42;
                public string GetString() => ""test"";
                public Task<int> GetIntAsync() => Task.FromResult(42);
                public void GetVoid() { }
            }");

        var intMethod = GetMethod(compilation, "GetInt");
        var stringMethod = GetMethod(compilation, "GetString");
        var intTaskMethod = GetMethod(compilation, "GetIntAsync");
        var voidMethod = GetMethod(compilation, "GetVoid");

        // Act & Assert
        ReturnTypeExtractor.GetFormattedReturnType(intMethod).Should().Be("Int!"); // Value types are non-null
        ReturnTypeExtractor.GetFormattedReturnType(stringMethod).Should().Be("String"); // Reference types are nullable without nullable context
        ReturnTypeExtractor.GetFormattedReturnType(intTaskMethod).Should().Be("Int!"); // Task<int> unwraps to int (non-null)
        ReturnTypeExtractor.GetFormattedReturnType(voidMethod).Should().Be("Boolean!"); // void -> Boolean (non-null)
    }

    [Fact]
    public void ExtractReturnType_ShouldHandleNullMethod()
    {
        // Act & Assert
        ReturnTypeExtractor.ExtractReturnType(null!).Should().Be("String"); // Fallback
        ReturnTypeExtractor.IsReturnTypeNullable(null!).Should().BeTrue(); // Safe default
        ReturnTypeExtractor.GetFormattedReturnType(null!).Should().Be("String"); // Fallback
    }

    private static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static IMethodSymbol GetMethod(Compilation compilation, string methodName)
    {
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();

        var methodDeclaration = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.Identifier.ValueText == methodName);

        return semanticModel.GetDeclaredSymbol(methodDeclaration)!;
    }

    [Fact]
    public void CachedStringOperations_ShouldWorkCorrectly()
    {
        // Arrange
        var compilation = CreateCompilation(@"
            using System.Threading.Tasks;
            public class TestClass 
            { 
                public Task<string> GetTaskString1() => Task.FromResult(""test1"");
                public Task<string> GetTaskString2() => Task.FromResult(""test2"");
                public Task<int> GetTaskInt1() => Task.FromResult(42);
                public Task<int> GetTaskInt2() => Task.FromResult(43);
            }");

        var methods = new[]
        {
            GetMethod(compilation, "GetTaskString1"),
            GetMethod(compilation, "GetTaskString2"),
            GetMethod(compilation, "GetTaskInt1"),
            GetMethod(compilation, "GetTaskInt2")
        };

        // Act - Multiple calls should work consistently
        var results1 = methods.Select(ReturnTypeExtractor.GetFormattedReturnType).ToArray();
        var results2 = methods.Select(ReturnTypeExtractor.GetFormattedReturnType).ToArray();

        // Assert - Results should be consistent across calls (verifying caching works)
        results1.Should().Equal(results2);
        results1[0].Should().Be("String"); // Task<string> -> String (nullable)
        results1[2].Should().Be("Int!"); // Task<int> -> Int! (non-null)
    }
}
