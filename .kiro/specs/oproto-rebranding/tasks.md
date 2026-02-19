# Implementation Plan: Oproto Rebranding

## Overview

Systematic rebranding of Lambda.GraphQL to Oproto.Lambda.GraphQL. The implementation follows a bottom-up approach: delete hackathon files first, rename folders and project files, update all source code namespaces, update configuration and build files, rewrite documentation, and finally verify correctness with property-based and unit tests.

## Tasks

- [x] 1. Delete hackathon content and old files
  - [x] 1.1 Delete `DEVLOG.md` and `graphql-hackathon.md`
    - Remove `DEVLOG.md` from the repository root
    - Remove `graphql-hackathon.md` from the repository root
    - _Requirements: 7.1, 7.2_

- [x] 2. Rename folder structure and project files
  - [x] 2.1 Rename solution file and project directories
    - Rename `Lambda.GraphQL.sln` to `Oproto.Lambda.GraphQL.sln`
    - Rename folder `Lambda.GraphQL/` to `Oproto.Lambda.GraphQL/`
    - Rename folder `Lambda.GraphQL.Build/` to `Oproto.Lambda.GraphQL.Build/`
    - Rename folder `Lambda.GraphQL.SourceGenerator/` to `Oproto.Lambda.GraphQL.SourceGenerator/`
    - Rename folder `Lambda.GraphQL.Tests/` to `Oproto.Lambda.GraphQL.Tests/`
    - Rename folder `Lambda.GraphQL.Examples/` to `Oproto.Lambda.GraphQL.Examples/`
    - _Requirements: 2.1, 3.1, 3.2, 3.3, 3.4, 3.5_

  - [x] 2.2 Rename `.csproj` files within each project directory
    - Rename `Lambda.GraphQL.csproj` to `Oproto.Lambda.GraphQL.csproj`
    - Rename `Lambda.GraphQL.Build.csproj` to `Oproto.Lambda.GraphQL.Build.csproj`
    - Rename `Lambda.GraphQL.SourceGenerator.csproj` to `Oproto.Lambda.GraphQL.SourceGenerator.csproj`
    - Rename `Lambda.GraphQL.Tests.csproj` to `Oproto.Lambda.GraphQL.Tests.csproj`
    - Rename `Lambda.GraphQL.Examples.csproj` to `Oproto.Lambda.GraphQL.Examples.csproj`
    - _Requirements: 2.2_

  - [x] 2.3 Rename MSBuild props/targets files in the build folder
    - Rename `Oproto.Lambda.GraphQL/build/Lambda.GraphQL.props` to `Oproto.Lambda.GraphQL/build/Oproto.Lambda.GraphQL.props`
    - Rename `Oproto.Lambda.GraphQL/build/Lambda.GraphQL.targets` to `Oproto.Lambda.GraphQL/build/Oproto.Lambda.GraphQL.targets`
    - _Requirements: 4.4_

- [x] 3. Update solution file and project references
  - [x] 3.1 Update the `.sln` file to reference renamed projects
    - Update all project paths and names in `Oproto.Lambda.GraphQL.sln` to use `Oproto.Lambda.GraphQL` prefixed paths
    - _Requirements: 2.4_

  - [x] 3.2 Update all `.csproj` files with new identities and references
    - Update `<PackageId>`, `<AssemblyName>`, `<RootNamespace>` to use `Oproto.Lambda.GraphQL` prefix in each `.csproj`
    - Update all `<ProjectReference>` paths to point to renamed project files and folders
    - Update any pack paths or DLL name references within `.csproj` files
    - _Requirements: 2.3, 4.1, 4.2, 4.3_

- [x] 4. Update C# source code namespaces and using directives
  - [x] 4.1 Update namespaces and usings in `Oproto.Lambda.GraphQL/` (main package)
    - Replace all `namespace Lambda.GraphQL` with `namespace Oproto.Lambda.GraphQL` in all `.cs` files under `Oproto.Lambda.GraphQL/`
    - Replace all `using Lambda.GraphQL` with `using Oproto.Lambda.GraphQL` in all `.cs` files
    - _Requirements: 1.1, 1.2_

  - [x] 4.2 Update namespaces and usings in `Oproto.Lambda.GraphQL.Build/`
    - Replace namespace and using directives in `ExtractGraphQLSchemaTask.cs` and any other `.cs` files
    - _Requirements: 1.1, 1.2_

  - [x] 4.3 Update namespaces and usings in `Oproto.Lambda.GraphQL.SourceGenerator/`
    - Replace namespace and using directives in all `.cs` files including `Models/` subfolder
    - Update any string literals referencing `Lambda.GraphQL` assembly or namespace names (e.g., in `GraphQLSchemaGenerator.cs`)
    - _Requirements: 1.1, 1.2_

  - [x] 4.4 Update namespaces and usings in `Oproto.Lambda.GraphQL.Tests/`
    - Replace namespace and using directives in all test `.cs` files including `Usings.cs`
    - _Requirements: 1.1, 1.2_

  - [x] 4.5 Update namespaces and usings in `Oproto.Lambda.GraphQL.Examples/`
    - Replace namespace and using directives in all example `.cs` files
    - _Requirements: 1.1, 1.2_

- [x] 5. Update build configuration and MSBuild files
  - [x] 5.1 Update `Directory.Build.props`
    - Update `<Product>` to `Oproto.Lambda.GraphQL`
    - Update `<Authors>`, `<Company>` to reflect Oproto Inc / Dan Guisinger
    - Update `<Copyright>` to `Copyright © Oproto Inc`
    - Update `<PackageProjectUrl>` and `<RepositoryUrl>` to `https://github.com/oproto/lambda-graphql`
    - _Requirements: 11.1, 8.1_

  - [x] 5.2 Update `Directory.Packages.props` and `nuget.config`
    - Replace any `Lambda.GraphQL` references with `Oproto.Lambda.GraphQL`
    - _Requirements: 11.2, 11.3_

  - [x] 5.3 Update MSBuild props and targets file contents
    - Update `Oproto.Lambda.GraphQL/build/Oproto.Lambda.GraphQL.props` internal references to use `Oproto.Lambda.GraphQL.Build` and `Oproto.Lambda.GraphQL.SourceGenerator` DLL names
    - Update `Oproto.Lambda.GraphQL/build/Oproto.Lambda.GraphQL.targets` internal references including `UsingTask` assembly paths and task class names (`Oproto.Lambda.GraphQL.Build.ExtractGraphQLSchemaTask`)
    - _Requirements: 4.4_

- [x] 6. Checkpoint - Verify build compiles
  - Ensure `dotnet build Oproto.Lambda.GraphQL.sln` compiles without errors, ask the user if questions arise.
  - _Requirements: 1.3, 2.5_

- [x] 7. Update documentation
  - [x] 7.1 Update all docs in `docs/` folder
    - Replace all `Lambda.GraphQL` references with `Oproto.Lambda.GraphQL` in text and code examples
    - Replace all `dguisinger/lambda-graphql` URLs with `oproto/lambda-graphql`
    - Remove any hackathon references from documentation files
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

  - [x] 7.2 Rewrite `README.md` following the `ExampleReadme.md` template
    - Rewrite the README to follow the FluentDynamoDb sister project layout from `ExampleReadme.md`
    - Include Oproto branding: logo placeholder, CI/CD badges for `oproto/lambda-graphql`, NuGet badges for `Oproto.Lambda.GraphQL`, `Oproto.Lambda.GraphQL.Build`, `Oproto.Lambda.GraphQL.SourceGenerator`
    - Include About section with Oproto Inc, Dan Guisinger, links to oproto.com, oproto.io, lambdagraphql.dev
    - Include Related Projects section linking to FluentDynamoDb and LambdaOpenApi
    - Include sponsorship section with GitHub Sponsors and Buy Me a Coffee links
    - Include Community & Support section with GitHub Issues/Discussions under `oproto/lambda-graphql`
    - Remove all hackathon references
    - Preserve accurate technical content (features, quick start, usage examples) updated to `Oproto.Lambda.GraphQL` naming
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 7.3_

- [x] 8. Update CDK example
  - [x] 8.1 Update CDK example references
    - Update `cdk-example/README.md` to replace all `Lambda.GraphQL` references with `Oproto.Lambda.GraphQL`
    - Update `cdk-example/src/graphql-api-stack.ts` assembly handler name from `Lambda.GraphQL.Examples` to `Oproto.Lambda.GraphQL.Examples`
    - Update `cdk-example/src/app.ts` description string if it references old naming
    - Update any other CDK config files referencing old naming or old repository URLs
    - _Requirements: 9.1, 9.2, 9.3, 8.2_

- [x] 9. Update steering files
  - [x] 9.1 Update `.kiro/steering/product.md`
    - Replace `Lambda.GraphQL` with `Oproto.Lambda.GraphQL` in product description
    - _Requirements: 10.1_

  - [x] 9.2 Update `.kiro/steering/structure.md`
    - Update folder names and project names to reflect `Oproto.Lambda.GraphQL` naming
    - _Requirements: 10.2_

  - [x] 9.3 Update `.kiro/steering/tech.md`
    - Update solution file name to `Oproto.Lambda.GraphQL.sln`
    - Update build commands to reference new solution name
    - _Requirements: 10.3_

- [x] 10. Checkpoint - Full build and test verification
  - Ensure `dotnet build Oproto.Lambda.GraphQL.sln` succeeds and `dotnet test` passes, ask the user if questions arise.
  - _Requirements: 1.3, 2.5_


## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- The rebranding is a mechanical prefix-prepend operation for most artifacts (`Lambda.GraphQL` → `Oproto.Lambda.GraphQL`)
- Repository URLs change from `dguisinger/lambda-graphql` to `oproto/lambda-graphql`
- The README is a full rewrite following the `ExampleReadme.md` template, not just find-and-replace
- MSBuild user-facing property names like `EnableLambdaGraphQLSchemaGeneration` remain unchanged
- Checkpoints ensure incremental validation after structural changes and after all changes
