# Requirements Document

## Introduction

This document captures the requirements for rebranding the Lambda.GraphQL project to Oproto.Lambda.GraphQL. The rebranding encompasses renaming all C# namespaces, project files, folder structures, NuGet packages, documentation, and repository references to reflect the new Oproto organizational identity. Hackathon-related content will be removed, and the README will be restructured to match the Oproto sister project (FluentDynamoDb) style.

## Glossary

- **Rebranding_System**: The set of tools, scripts, and manual processes used to rename and update all project artifacts from Lambda.GraphQL to Oproto.Lambda.GraphQL
- **Old_Name**: The current project identifier `Lambda.GraphQL` and its derivatives (e.g., `Lambda.GraphQL.Build`, `Lambda.GraphQL.SourceGenerator`)
- **New_Name**: The target project identifier `Oproto.Lambda.GraphQL` and its derivatives (e.g., `Oproto.Lambda.GraphQL.Build`, `Oproto.Lambda.GraphQL.SourceGenerator`)
- **Solution_File**: The `.sln` file that references all projects in the repository
- **Steering_Files**: Configuration files in `.kiro/steering/` that guide development tooling
- **CDK_Example**: The AWS CDK deployment example in the `cdk-example/` directory
- **Sister_Project_README**: The ExampleReadme.md file representing the FluentDynamoDb README layout and style

## Requirements

### Requirement 1: C# Namespace Renaming

**User Story:** As a developer consuming the NuGet packages, I want all C# namespaces to use the Oproto.Lambda.GraphQL prefix, so that the packages are consistent with the Oproto organization branding.

#### Acceptance Criteria

1. WHEN the Rebranding_System processes a C# source file containing a namespace declaration starting with `Lambda.GraphQL`, THE Rebranding_System SHALL replace the namespace with the equivalent `Oproto.Lambda.GraphQL` prefix
2. WHEN the Rebranding_System processes a C# source file containing a `using` directive referencing `Lambda.GraphQL`, THE Rebranding_System SHALL replace the directive with the equivalent `Oproto.Lambda.GraphQL` reference
3. WHEN all namespace renames are complete, THE Solution SHALL compile without errors using `dotnet build`

### Requirement 2: Project and Solution File Renaming

**User Story:** As a developer cloning the repository, I want all project files and the solution file to use the Oproto.Lambda.GraphQL naming, so that the on-disk structure matches the package identity.

#### Acceptance Criteria

1. THE Rebranding_System SHALL rename the solution file from `Lambda.GraphQL.sln` to `Oproto.Lambda.GraphQL.sln`
2. THE Rebranding_System SHALL rename each `.csproj` file from `Lambda.GraphQL.{Suffix}.csproj` to `Oproto.Lambda.GraphQL.{Suffix}.csproj` for all projects (main, Build, SourceGenerator, Tests, Examples)
3. THE Rebranding_System SHALL update all project references within `.csproj` files to use the new `Oproto.Lambda.GraphQL` project names and paths
4. THE Rebranding_System SHALL update the Solution_File to reference the renamed projects at their new paths
5. WHEN all project renames are complete, THE Solution SHALL build successfully using `dotnet build Oproto.Lambda.GraphQL.sln`

### Requirement 3: Folder Structure Renaming

**User Story:** As a developer navigating the repository, I want the folder names to match the new Oproto.Lambda.GraphQL naming, so that the directory structure is consistent with the project identity.

#### Acceptance Criteria

1. THE Rebranding_System SHALL rename the folder `Lambda.GraphQL/` to `Oproto.Lambda.GraphQL/`
2. THE Rebranding_System SHALL rename the folder `Lambda.GraphQL.Build/` to `Oproto.Lambda.GraphQL.Build/`
3. THE Rebranding_System SHALL rename the folder `Lambda.GraphQL.SourceGenerator/` to `Oproto.Lambda.GraphQL.SourceGenerator/`
4. THE Rebranding_System SHALL rename the folder `Lambda.GraphQL.Tests/` to `Oproto.Lambda.GraphQL.Tests/`
5. THE Rebranding_System SHALL rename the folder `Lambda.GraphQL.Examples/` to `Oproto.Lambda.GraphQL.Examples/`

### Requirement 4: NuGet Package Identity Update

**User Story:** As a developer adding the package via NuGet, I want the package IDs and assembly names to reflect Oproto.Lambda.GraphQL, so that the packages are discoverable under the Oproto organization.

#### Acceptance Criteria

1. THE Rebranding_System SHALL update the `<PackageId>` element in each `.csproj` to use the `Oproto.Lambda.GraphQL` prefix
2. THE Rebranding_System SHALL update the `<AssemblyName>` element in each `.csproj` to use the `Oproto.Lambda.GraphQL` prefix
3. THE Rebranding_System SHALL update the `<RootNamespace>` element in each `.csproj` to use the `Oproto.Lambda.GraphQL` prefix
4. WHEN MSBuild props and targets files reference the old package name, THE Rebranding_System SHALL update those references to use `Oproto.Lambda.GraphQL`

### Requirement 5: README Overhaul

**User Story:** As a visitor to the repository, I want the README to follow the Oproto branding style used by the sister project (FluentDynamoDb), so that the project presents a professional and consistent identity.

#### Acceptance Criteria

1. THE Rebranding_System SHALL rewrite the README to follow the layout and structure of the Sister_Project_README
2. THE Rebranding_System SHALL include Oproto branding elements: a logo placeholder, CI/CD badges pointing to `oproto/lambda-graphql`, and NuGet badges for each package
3. THE Rebranding_System SHALL include an About section listing Oproto Inc as the maintainer, Dan Guisinger as the developer, and links to oproto.com, oproto.io, and lambdagraphql.dev
4. THE Rebranding_System SHALL include a Related Projects section linking to FluentDynamoDb and LambdaOpenApi sister projects
5. THE Rebranding_System SHALL include a sponsorship section with GitHub Sponsors and Buy Me a Coffee links
6. THE Rebranding_System SHALL include a Community and Support section with links to GitHub Issues and Discussions under the `oproto/lambda-graphql` repository
7. THE Rebranding_System SHALL remove all hackathon references from the README
8. THE Rebranding_System SHALL preserve accurate technical content describing the project features, quick start guide, and usage examples, updated to use `Oproto.Lambda.GraphQL` naming

### Requirement 6: Documentation Updates

**User Story:** As a developer reading the documentation, I want all docs to reference Oproto.Lambda.GraphQL consistently, so that there is no confusion about the package name or repository location.

#### Acceptance Criteria

1. WHEN a documentation file in the `docs/` folder contains a reference to `Lambda.GraphQL`, THE Rebranding_System SHALL replace the reference with `Oproto.Lambda.GraphQL`
2. WHEN a documentation file contains a GitHub URL pointing to `dguisinger/lambda-graphql`, THE Rebranding_System SHALL replace the URL with `oproto/lambda-graphql`
3. THE Rebranding_System SHALL remove all hackathon references from documentation files
4. WHEN a documentation file contains code examples using the `Lambda.GraphQL` namespace, THE Rebranding_System SHALL update those examples to use `Oproto.Lambda.GraphQL`

### Requirement 7: Hackathon Content Removal

**User Story:** As a project maintainer, I want all hackathon-specific files and references removed, so that the project presents as a production-quality open-source library.

#### Acceptance Criteria

1. THE Rebranding_System SHALL delete the file `DEVLOG.md`
2. THE Rebranding_System SHALL delete the file `graphql-hackathon.md`
3. WHEN any remaining file contains a reference to "hackathon", THE Rebranding_System SHALL remove or replace that reference with appropriate production-oriented content

### Requirement 8: Repository URL Updates

**User Story:** As a contributor, I want all repository URLs to point to the oproto organization, so that links to issues, pull requests, and source code resolve correctly.

#### Acceptance Criteria

1. WHEN any file in the repository contains a GitHub URL referencing `dguisinger/lambda-graphql`, THE Rebranding_System SHALL replace the URL with the equivalent `oproto/lambda-graphql` URL
2. THE Rebranding_System SHALL update the CDK_Example README and configuration files to reference the new repository URL

### Requirement 9: CDK Example Updates

**User Story:** As a developer following the CDK deployment example, I want the example to reference the new Oproto.Lambda.GraphQL package names, so that the example works with the rebranded packages.

#### Acceptance Criteria

1. WHEN the CDK_Example references `Lambda.GraphQL` project names or paths, THE Rebranding_System SHALL update those references to use `Oproto.Lambda.GraphQL`
2. WHEN the CDK_Example README contains references to the old naming, THE Rebranding_System SHALL update those references to use the new naming
3. WHEN the CDK_Example contains assembly name references (e.g., Lambda handler names), THE Rebranding_System SHALL update those to reflect the new assembly names

### Requirement 10: Steering File Updates

**User Story:** As a developer using Kiro tooling, I want the steering files to reflect the new Oproto.Lambda.GraphQL naming, so that AI-assisted development uses accurate project context.

#### Acceptance Criteria

1. THE Rebranding_System SHALL update `.kiro/steering/product.md` to reference Oproto.Lambda.GraphQL instead of Lambda.GraphQL
2. THE Rebranding_System SHALL update `.kiro/steering/structure.md` to reflect the renamed folder structure and project names
3. THE Rebranding_System SHALL update `.kiro/steering/tech.md` to reference the new solution file name and updated build commands

### Requirement 11: Build Configuration Updates

**User Story:** As a developer building the project, I want the shared build configuration files to reference the new naming, so that the build pipeline functions correctly after the rename.

#### Acceptance Criteria

1. WHEN `Directory.Build.props` contains references to `Lambda.GraphQL`, THE Rebranding_System SHALL update those references to `Oproto.Lambda.GraphQL`
2. WHEN `Directory.Packages.props` contains references to `Lambda.GraphQL`, THE Rebranding_System SHALL update those references to `Oproto.Lambda.GraphQL`
3. WHEN `nuget.config` contains references to `Lambda.GraphQL`, THE Rebranding_System SHALL update those references to `Oproto.Lambda.GraphQL`
