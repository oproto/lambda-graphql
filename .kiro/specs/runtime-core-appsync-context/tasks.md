# Implementation Plan: Runtime Core — AppSync Resolver Context

## Overview

Implement the `Oproto.Lambda.GraphQL.Runtime` package providing typed C# models for AppSync resolver context deserialization, update the source generator to emit full-context JS resolver code in `resolvers.json`, and update the CDK example to consume the new `resolverCode` property. All runtime code must be AOT-compatible using `System.Text.Json` source generators.

## Tasks

- [x] 1. Create Runtime package project and core context model
  - [x] 1.1 Create `Oproto.Lambda.GraphQL.Runtime` project targeting `net8.0;net10.0`
    - Create `Oproto.Lambda.GraphQL.Runtime/Oproto.Lambda.GraphQL.Runtime.csproj` with `net8.0;net10.0` target frameworks
    - Namespace: `Oproto.Lambda.GraphQL.Runtime`
    - No NuGet dependency on `System.Text.Json` (framework-provided)
    - No reflection-based serialization dependencies
    - Add the project to `Oproto.Lambda.GraphQL.sln`
    - Add a project reference from `Oproto.Lambda.GraphQL.Tests` to the new Runtime project
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

  - [x] 1.2 Implement `AppSyncResolverContext<TArguments>` model
    - Create `AppSyncResolverContext.cs` with generic class containing `Arguments`, `Source` (`JsonElement?`), `Identity` (`AppSyncIdentity?`), `Info` (`AppSyncInfo?`), `Request` (`AppSyncRequest?`), `Stash` (`JsonElement?`), `Prev` (`JsonElement?`)
    - All optional properties default to null when absent from JSON
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [x] 1.3 Implement `AppSyncInfo` model
    - Create `AppSyncInfo.cs` with `FieldName`, `ParentTypeName`, `SelectionSetList` (`List<string>?`), `SelectionSetGraphQL` (`string?`)
    - _Requirements: 4.1, 4.2, 4.3, 4.4_

  - [x] 1.4 Implement `AppSyncRequest` model
    - Create `AppSyncRequest.cs` with `Headers` (`Dictionary<string, string>?`)
    - _Requirements: 5.1, 5.2, 5.3_

- [x] 2. Implement identity type hierarchy and polymorphic deserialization
  - [x] 2.1 Implement identity model classes
    - Create `AppSyncIdentity.cs` base class
    - Create `CognitoUserPoolsIdentity.cs` with `Sub`, `Issuer`, `Username`, `Claims` (`Dictionary<string, string>?`), `DefaultAuthStrategy`, `Groups` (`List<string>?`)
    - Create `IamIdentity.cs` with `AccountId`, `CognitoIdentityPoolId`, `CognitoIdentityId`, `SourceIp` (`List<string>?`), `Username`, `UserArn`
    - Create `OidcIdentity.cs` with `Sub`, `Issuer`, `Claims` (`Dictionary<string, string>?`)
    - Create `LambdaAuthorizerIdentity.cs` with `ResolverContext` (`Dictionary<string, string>?`)
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

  - [x] 2.2 Implement `AppSyncIdentityConverter` custom JsonConverter
    - Create `AppSyncIdentityConverter.cs` as `JsonConverter<AppSyncIdentity>`
    - Discriminate by inspecting JSON properties: `resolverContext` → `LambdaAuthorizerIdentity`, `cognitoIdentityPoolId`/`userArn` → `IamIdentity`, `defaultAuthStrategy`/`groups` → `CognitoUserPoolsIdentity`, `sub`+`issuer` fallback → `OidcIdentity`, none → base `AppSyncIdentity`
    - Must be AOT-compatible (no reflection), read JSON into `JsonElement` for inspection
    - _Requirements: 3.6, 3.7, 3.8, 3.9, 3.10_

  - [x] 2.3 Write unit tests for `AppSyncIdentityConverter`
    - Test deserialization of each identity subtype from realistic AppSync JSON payloads
    - Test null/absent identity field returns null
    - Test unknown identity shape (no discriminating properties) returns base `AppSyncIdentity`
    - _Requirements: 10.2_

- [x] 3. Implement AOT-compatible serialization and convenience methods
  - [x] 3.1 Implement `AppSyncResolverContextJsonSerializerContext`
    - Create source-generated `JsonSerializerContext` with `[JsonSerializable]` attributes for all public model types
    - Use `JsonKnownNamingPolicy.CamelCase` and `JsonIgnoreCondition.WhenWritingNull`
    - Register `AppSyncIdentityConverter` via `[JsonConverter]` attribute on `AppSyncIdentity` or via options
    - _Requirements: 6.1, 6.2, 6.3_

  - [x] 3.2 Implement `AppSyncResolverContextSerializer` convenience class
    - Create static `Deserialize<TArguments>(string json, JsonSerializerOptions?)` method
    - Create static `Deserialize<TArguments>(Stream stream, JsonSerializerOptions?)` method for Lambda Annotations fallback
    - Create static `Deserialize<TArguments>(string json, JsonTypeInfo<AppSyncResolverContext<TArguments>>)` method for AOT
    - Expose `DefaultOptions` property with camelCase naming and `AppSyncIdentityConverter` registered
    - Add XML documentation comments describing migration path from arguments-only to full-context
    - _Requirements: 6.4, 6.5, 9.2, 9.3_

- [x] 4. Checkpoint - Verify runtime package builds and basic deserialization works
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Update source generator for full-context resolver code
  - [x] 5.1 Remove `UsesLambdaContext` from `ResolverInfo` model
    - Remove the `UsesLambdaContext` property from `Oproto.Lambda.GraphQL.SourceGenerator/Models/ResolverInfo.cs`
    - Remove any references to `UsesLambdaContext` in the source generator code (`GraphQLSchemaGenerator.cs` and related files)
    - _Requirements: 7.3_

  - [x] 5.2 Update `ResolverManifestGenerator` to emit `resolverCode`
    - Add `resolverCode` property to each unit resolver entry in the generated JSON
    - The JS code sends full AppSync context: `arguments`, `source`, `identity`, `info`, `request`, `stash`, `prev`
    - Response handler propagates errors via `util.error()` and returns `ctx.result` on success
    - Remove the `usesLambdaContext` JSON property emission
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

  - [x] 5.3 Write property test for resolver manifest full-context code
    - **Property 5: All unit resolvers emit full-context resolverCode**
    - For any collection of `ResolverInfo` entries with at least one unit resolver, the manifest SHALL contain `resolverCode` with all seven context fields and no `usesLambdaContext` property
    - **Validates: Requirements 7.1, 7.2, 7.3**

  - [x] 5.4 Update existing `ResolverManifestTests` for new format
    - Update existing tests to account for `resolverCode` presence and `usesLambdaContext` removal
    - Add unit test verifying `resolverCode` contains all seven context fields
    - Add unit test verifying no resolver entry contains `usesLambdaContext`
    - _Requirements: 7.1, 7.2, 7.3_

- [x] 6. Checkpoint - Verify source generator changes and manifest format
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Write runtime deserialization tests
  - [x] 7.1 Create FsCheck generators for runtime types
    - Create `Oproto.Lambda.GraphQL.Tests/Runtime/Generators/AppSyncArbitraries.cs`
    - Implement `Arbitrary` instances for `AppSyncResolverContext<JsonElement>`, all identity subtypes, `AppSyncInfo` (including nested paths like `"category/name"`), `AppSyncRequest`
    - _Requirements: 10.6_

  - [x] 7.2 Write property test for round-trip serialization
    - **Property 1: Context model round-trip serialization**
    - For any valid `AppSyncResolverContext<TArguments>`, serialize then deserialize SHALL produce equivalent object
    - **Validates: Requirements 2.2, 2.3, 2.4, 2.5, 2.7, 4.2, 4.3**

  - [x] 7.3 Write property test for missing optional properties
    - **Property 2: Missing optional properties default to null**
    - For any subset of optional properties omitted from JSON, deserialization SHALL produce null for omitted properties
    - **Validates: Requirements 2.6**

  - [x] 7.4 Write property test for identity polymorphic deserialization
    - **Property 3: Identity polymorphic deserialization preserves concrete type**
    - For any identity subtype, serialize then deserialize through `AppSyncIdentityConverter` SHALL preserve concrete type
    - **Validates: Requirements 3.6, 3.7, 3.8, 3.9**

  - [x] 7.5 Write property test for camelCase serialization
    - **Property 4: Serialized JSON uses camelCase property names**
    - For any valid context, serialized JSON SHALL have all property names in camelCase
    - **Validates: Requirements 6.3**

  - [x] 7.6 Write unit tests for context deserialization
    - Create `Oproto.Lambda.GraphQL.Tests/Runtime/AppSyncResolverContextTests.cs`
    - Test full context deserialization with all properties populated
    - Test payloads with missing optional properties (source, identity, info, request, stash, prev)
    - Test `AppSyncInfo` with nested selection set paths
    - Test `AppSyncRequest` with populated and empty headers
    - Test AOT-compatible deserialization using `AppSyncResolverContextJsonSerializerContext`
    - _Requirements: 10.1, 10.3, 10.4, 10.5, 10.7_

- [x] 8. Update CDK example
  - [x] 8.1 Update CDK stack to use `resolverCode` from manifest
    - Update `ResolverConfig` interface to include `resolverCode` property and remove `usesLambdaContext`
    - Replace inline JS resolver code generation with `config.resolverCode` from the manifest
    - Remove the conditional logic that switches between arguments-only and full-context JS
    - Update Lambda runtime from `DOTNET_6` to `DOTNET_8`
    - _Requirements: 8.1, 8.2, 8.3_

- [x] 9. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The design uses C# throughout, so all implementation tasks use C#
- The Runtime package must be AOT-compatible: no reflection, use `System.Text.Json` source generators
- The `AppSyncIdentityConverter` is the most complex piece — it uses property-name inspection to discriminate identity subtypes without reflection
