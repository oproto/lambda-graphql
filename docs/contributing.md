# Contributing to Oproto.Lambda.GraphQL

Thank you for your interest in contributing to Oproto.Lambda.GraphQL! This guide will help you get started with development, testing, and submitting contributions.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Project Structure](#project-structure)
- [Development Workflow](#development-workflow)
- [Testing Guidelines](#testing-guidelines)
- [Code Style](#code-style)
- [Submitting Changes](#submitting-changes)
- [Release Process](#release-process)

---

## Code of Conduct

This project follows a simple code of conduct:

- **Be respectful** - Treat all contributors with respect
- **Be constructive** - Provide helpful feedback and suggestions
- **Be collaborative** - Work together to improve the project
- **Be patient** - Remember that everyone is learning

---

## Getting Started

### Prerequisites

- **.NET 6.0 SDK or later** - [Download](https://dotnet.microsoft.com/download)
- **Git** - [Download](https://git-scm.com/downloads)
- **IDE** (recommended):
  - Visual Studio 2022+ with .NET workload
  - JetBrains Rider 2022.3+
  - Visual Studio Code with C# extension

### Fork and Clone

```bash
# Fork the repository on GitHub, then clone your fork
git clone https://github.com/YOUR-USERNAME/lambda-graphql.git
cd lambda-graphql

# Add upstream remote
git remote add upstream https://github.com/oproto/lambda-graphql.git
```

---

## Development Setup

### Initial Build

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Verify example project builds and generates schema
cd Oproto.Lambda.GraphQL.Examples
dotnet build
ls -la schema.graphql resolvers.json
cd ..
```

**Expected Output**:
- All projects build successfully
- 84+ tests pass
- `schema.graphql` and `resolvers.json` generated in Examples project

### IDE Setup

#### Visual Studio

1. Open `Oproto.Lambda.GraphQL.sln`
2. Set build configuration to Debug
3. Enable "Show All Files" in Solution Explorer
4. View generated files in `obj/` directories

#### Rider

1. Open `Oproto.Lambda.GraphQL.sln`
2. Enable "Show All Files" in Solution view
3. Settings → Build, Execution, Deployment → Toolset and Build
   - Use MSBuild version: .NET SDK

#### VS Code

1. Open project folder
2. Install recommended extensions:
   - C# (Microsoft)
   - C# Dev Kit (Microsoft)
3. Use integrated terminal for build commands

---

## Project Structure

```
Oproto.Lambda.GraphQL/
├── Oproto.Lambda.GraphQL/                    # Main package
│   ├── Attributes/                    # GraphQL attribute definitions
│   │   ├── GraphQLTypeAttribute.cs
│   │   ├── GraphQLFieldAttribute.cs
│   │   ├── GraphQLOperationAttributes.cs
│   │   └── ...
│   └── build/                         # MSBuild integration files
│       ├── Oproto.Lambda.GraphQL.props
│       └── Oproto.Lambda.GraphQL.targets
│
├── Oproto.Lambda.GraphQL.SourceGenerator/    # Roslyn source generator
│   ├── GraphQLSchemaGenerator.cs      # Main generator entry point
│   ├── SdlGenerator.cs                # SDL generation logic
│   ├── TypeMapper.cs                  # C# to GraphQL type mapping
│   ├── AwsScalarMapper.cs             # AWS scalar type mapping
│   ├── ReturnTypeExtractor.cs         # Method return type analysis
│   ├── ResolverManifestGenerator.cs   # Resolver JSON generation
│   ├── DiagnosticDescriptors.cs       # Compiler diagnostics
│   └── Models/                        # Data models
│       ├── TypeInfo.cs
│       ├── FieldInfo.cs
│       ├── ResolverInfo.cs
│       └── ...
│
├── Oproto.Lambda.GraphQL.Build/              # MSBuild task
│   └── ExtractGraphQLSchemaTask.cs    # Schema extraction task
│
├── Oproto.Lambda.GraphQL.Tests/              # Unit tests
│   ├── TypeMapperTests.cs
│   ├── SdlGeneratorTests.cs
│   ├── ResolverManifestTests.cs
│   └── ...
│
├── Oproto.Lambda.GraphQL.Examples/           # Example project
│   ├── Product.cs
│   ├── ProductFunctions.cs
│   └── AdvancedTypes.cs
│
└── docs/                              # Documentation
    ├── README.md
    ├── getting-started.md
    ├── architecture.md
    └── ...
```

### Package Responsibilities

| Package | Purpose | Key Files |
|---------|---------|-----------|
| `Oproto.Lambda.GraphQL` | Attributes and build integration | Attributes/*.cs, build/*.targets |
| `Oproto.Lambda.GraphQL.SourceGenerator` | Compile-time schema generation | GraphQLSchemaGenerator.cs, SdlGenerator.cs |
| `Oproto.Lambda.GraphQL.Build` | Post-build schema extraction | ExtractGraphQLSchemaTask.cs |
| `Oproto.Lambda.GraphQL.Tests` | Unit and integration tests | *Tests.cs |
| `Oproto.Lambda.GraphQL.Examples` | Example usage and validation | *.cs |

---

## Development Workflow

### Making Changes

1. **Create a feature branch**:
```bash
git checkout -b feature/your-feature-name
```

2. **Make your changes** following the code style guidelines

3. **Build and test**:
```bash
# IMPORTANT: Shutdown build server after source generator changes
dotnet build-server shutdown

# Build
dotnet build

# Run tests
dotnet test

# Test with example project
cd Oproto.Lambda.GraphQL.Examples
dotnet build
cat schema.graphql
cd ..
```

4. **Commit your changes**:
```bash
git add .
git commit -m "feat: add support for X"
```

### Commit Message Format

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Types**:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `test`: Test additions or changes
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `chore`: Build process or tooling changes

**Examples**:
```
feat(generator): add support for union types
fix(mapper): handle nullable reference types correctly
docs(readme): update installation instructions
test(sdl): add tests for interface generation
```

---

## Testing Guidelines

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~TypeMapperTests"

# Run with detailed output
dotnet test -v detailed

# Run with coverage (requires coverlet)
dotnet test /p:CollectCoverage=true
```

### Writing Tests

#### Unit Test Structure

```csharp
using Xunit;
using FluentAssertions;

public class YourFeatureTests
{
    [Fact]
    public void MethodName_Scenario_ExpectedBehavior()
    {
        // Arrange
        var input = "test";
        
        // Act
        var result = YourMethod(input);
        
        // Assert
        result.Should().Be("expected");
    }
}
```

#### Test Categories

1. **Type Mapping Tests** - Verify C# to GraphQL type conversion
2. **SDL Generation Tests** - Verify correct GraphQL schema syntax
3. **Resolver Tests** - Verify resolver manifest generation
4. **Attribute Tests** - Verify attribute parsing
5. **Diagnostic Tests** - Verify error reporting

#### Test Data Builders

```csharp
// Use helper methods for complex test data
private static TypeInfo CreateTypeInfo(string name, GraphQLTypeKind kind)
{
    return new TypeInfo
    {
        Name = name,
        Kind = kind,
        Fields = new List<FieldInfo>()
    };
}

[Fact]
public void GenerateType_WithFields_GeneratesCorrectSdl()
{
    // Arrange
    var typeInfo = CreateTypeInfo("Product", GraphQLTypeKind.Object);
    typeInfo.Fields.Add(new FieldInfo { Name = "id", Type = "ID!" });
    
    // Act & Assert
    // ...
}
```

### Test Coverage Goals

- **Minimum**: 80% code coverage
- **Target**: 90% code coverage
- **Critical paths**: 100% coverage (type mapping, SDL generation)

---

## Code Style

### C# Coding Conventions

Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions):

```csharp
// ✅ Good
public class TypeMapper
{
    private readonly Dictionary<string, string> _typeMap;
    
    public string MapType(string csharpType)
    {
        if (string.IsNullOrEmpty(csharpType))
        {
            throw new ArgumentException("Type cannot be null or empty", nameof(csharpType));
        }
        
        return _typeMap.TryGetValue(csharpType, out var graphqlType)
            ? graphqlType
            : "String";
    }
}

// ❌ Bad
public class typemapper
{
    private Dictionary<string, string> typeMap;
    
    public string mapType(string t)
    {
        if (t == null || t == "") throw new Exception("bad type");
        return typeMap.ContainsKey(t) ? typeMap[t] : "String";
    }
}
```

### Naming Conventions

- **Classes**: PascalCase (`TypeMapper`, `SdlGenerator`)
- **Methods**: PascalCase (`MapType`, `GenerateSchema`)
- **Properties**: PascalCase (`Name`, `Description`)
- **Fields**: _camelCase with underscore (`_typeMap`, _context`)
- **Parameters**: camelCase (`csharpType`, `context`)
- **Constants**: PascalCase (`DefaultTimeout`)

### File Organization

```csharp
// 1. Using statements (sorted)
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

// 2. Namespace
namespace Oproto.Lambda.GraphQL.SourceGenerator;

// 3. Class documentation
/// <summary>
/// Maps C# types to GraphQL types.
/// </summary>
public class TypeMapper
{
    // 4. Constants
    private const string DefaultType = "String";
    
    // 5. Fields
    private readonly Dictionary<string, string> _typeMap;
    
    // 6. Constructor
    public TypeMapper()
    {
        _typeMap = new Dictionary<string, string>();
    }
    
    // 7. Public methods
    public string MapType(string csharpType)
    {
        // Implementation
    }
    
    // 8. Private methods
    private bool IsNullable(string type)
    {
        // Implementation
    }
}
```

### Documentation Comments

```csharp
/// <summary>
/// Maps a C# type to its corresponding GraphQL type.
/// </summary>
/// <param name="csharpType">The C# type name (e.g., "string", "int", "List<Product>").</param>
/// <returns>The GraphQL type (e.g., "String!", "Int!", "[Product]!").</returns>
/// <exception cref="ArgumentException">Thrown when csharpType is null or empty.</exception>
public string MapType(string csharpType)
{
    // Implementation
}
```

---

## Submitting Changes

### Pull Request Process

1. **Update your branch**:
```bash
git fetch upstream
git rebase upstream/main
```

2. **Push to your fork**:
```bash
git push origin feature/your-feature-name
```

3. **Create Pull Request** on GitHub:
   - Use descriptive title following commit message format
   - Fill out PR template completely
   - Link related issues
   - Add screenshots/examples if applicable

4. **Address review feedback**:
```bash
# Make changes
git add .
git commit -m "fix: address review feedback"
git push origin feature/your-feature-name
```

### PR Checklist

- [ ] Code builds successfully
- [ ] All tests pass
- [ ] New tests added for new functionality
- [ ] Documentation updated
- [ ] CHANGELOG.md updated (for significant changes)
- [ ] No breaking changes (or clearly documented)
- [ ] Commit messages follow convention
- [ ] Code follows style guidelines

### Review Process

1. **Automated Checks**: CI/CD runs tests and linting
2. **Code Review**: Maintainer reviews code quality and design
3. **Feedback**: Address any requested changes
4. **Approval**: Maintainer approves PR
5. **Merge**: Maintainer merges to main branch

---

## Source Generator Development

### Important Notes

**CRITICAL**: After making changes to `Oproto.Lambda.GraphQL.SourceGenerator`, you MUST run:

```bash
dotnet build-server shutdown
```

The Roslyn compiler server caches loaded analyzers/generators, so changes won't take effect until the server is restarted.

### Debugging Source Generators

#### View Generated Files

```bash
# Find generated files
find Oproto.Lambda.GraphQL.Examples/obj -name "*GraphQLSchemaGenerator*.cs"

# View generated file
cat Oproto.Lambda.GraphQL.Examples/obj/Debug/net6.0/generated/Oproto.Lambda.GraphQL.SourceGenerator/Oproto.Lambda.GraphQL.SourceGenerator.GraphQLSchemaGenerator/GraphQLSchema.g.cs
```

#### Enable Generator Logging

```xml
<!-- Add to .csproj for debugging -->
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

#### Debug with Visual Studio

1. Set `Oproto.Lambda.GraphQL.SourceGenerator` as startup project
2. Right-click project → Properties → Debug
3. Launch profile: Roslyn Component
4. Set breakpoints in generator code
5. F5 to debug

---

## Release Process

### Version Numbering

Follow [Semantic Versioning](https://semver.org/):

- **Major** (1.0.0): Breaking changes
- **Minor** (0.1.0): New features, backward compatible
- **Patch** (0.0.1): Bug fixes, backward compatible

### Release Checklist

1. Update version in `Directory.Build.props`
2. Update CHANGELOG.md
3. Create release branch: `release/v1.0.0`
4. Run full test suite
5. Build NuGet packages: `dotnet pack -c Release`
6. Test packages locally
7. Create GitHub release with tag
8. Publish to NuGet.org

---

## Getting Help

### Questions?

- **GitHub Discussions**: Ask questions and discuss ideas
- **GitHub Issues**: Report bugs or request features
- **Email**: Contact maintainers directly for sensitive issues

### Resources

- [Architecture Documentation](architecture.md)
- [API Reference](api-reference.md)
- [Roslyn Source Generators](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- [MSBuild Tasks](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-tasks)

---

Thank you for contributing to Oproto.Lambda.GraphQL! 🎉
