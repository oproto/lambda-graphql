# Project Structure

```
Oproto.Lambda.GraphQL/
├── Oproto.Lambda.GraphQL/                    # Main package (attributes + build integration)
│   ├── Attributes/                           # GraphQL attribute definitions
│   └── build/                                # MSBuild props/targets
├── Oproto.Lambda.GraphQL.Build/              # MSBuild task for schema extraction
├── Oproto.Lambda.GraphQL.SourceGenerator/    # Roslyn incremental source generator
│   └── Models/                               # Type/Field/Resolver info models
├── Oproto.Lambda.GraphQL.Tests/              # Unit and property-based tests
├── Oproto.Lambda.GraphQL.Examples/           # Example Lambda project
└── docs/                                     # Documentation
```

## Package Responsibilities

| Package | Purpose |
|---------|---------|
| `Oproto.Lambda.GraphQL` | Attributes consumed by user code, bundled build targets |
| `Oproto.Lambda.GraphQL.Build` | MSBuild task that extracts schema post-build |
| `Oproto.Lambda.GraphQL.SourceGenerator` | Roslyn generator that analyzes code and generates SDL |

## Key Files
- `GraphQLSchemaGenerator.cs` - Main source generator entry point
- `ExtractGraphQLSchemaTask.cs` - MSBuild task implementation
- `Attributes/*.cs` - All GraphQL attribute definitions

## Conventions
- Attributes are in the `Attributes/` folder of the main package
- Source generator uses incremental generator pattern for performance
- Models for extracted info live in `Models/` subfolder of generator project
