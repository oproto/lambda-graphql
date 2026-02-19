# Technology Stack

## Language & Framework
- .NET (C#)
- Native AOT compatible

## Core Technologies
- Roslyn Source Generators - compile-time schema generation
- MSBuild Tasks - post-build schema extraction
- AWS Lambda Annotations - Lambda function definitions
- AWS AppSync - GraphQL API service

## Key Dependencies

### Runtime
- None (attributes only package)

### Build
- Microsoft.CodeAnalysis.CSharp (source generator)
- Microsoft.Build.Framework (MSBuild task)
- System.Reflection.MetadataLoadContext (fallback extraction)

### Testing
- xUnit
- FsCheck.Xunit (property-based testing)
- FluentAssertions

## Common Commands

```bash
# Build solution
dotnet build Oproto.Lambda.GraphQL.sln

# Run tests
dotnet test

# Pack NuGet packages
dotnet pack

# Clean build artifacts
dotnet clean

# Shutdown build server (REQUIRED after source generator changes)
dotnet build-server shutdown
```

## Important: Source Generator Development

When making changes to `Oproto.Lambda.GraphQL.SourceGenerator`, you MUST run `dotnet build-server shutdown` before rebuilding. The Roslyn compiler server caches loaded analyzers/generators, so changes won't take effect until the server is restarted.

## Build Outputs
- `schema.graphql` - Generated GraphQL SDL
- `resolvers.json` - Resolver manifest for CDK
