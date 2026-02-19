# Requirements Document

## Introduction

This spec covers the foundational runtime library for Oproto.Lambda.GraphQL: the `Oproto.Lambda.GraphQL.Runtime` package. Currently, the source generator produces `schema.graphql` and `resolvers.json` at compile time, but there is zero runtime support. The CDK-generated JS resolver code sends only `ctx.arguments` as the Lambda payload, discarding the rich AppSync context (identity, source, selectionSetList, request headers, stash, prev). The `usesLambdaContext` flag was a partial workaround but has no corresponding C# model to deserialize into.

This spec introduces a typed `AppSyncResolverContext<TArguments>` model with AOT-compatible JSON deserialization, updates the source generator to always emit full-context JS resolver code, and updates the CDK example to use the new pattern.

## Glossary

- **Runtime_Package**: The new `Oproto.Lambda.GraphQL.Runtime` NuGet package (net8.0;net10.0) providing typed models for AppSync resolver context deserialization
- **Source_Generator**: The existing Roslyn incremental source generator (`Oproto.Lambda.GraphQL.SourceGenerator`) that produces `schema.graphql` and `resolvers.json` from C# attributes
- **Resolver_Manifest**: The `resolvers.json` file emitted by the Source_Generator containing resolver configurations for CDK deployment
- **JS_Resolver_Code**: The JavaScript resolver request/response handler code used by AppSync to transform the context before invoking Lambda
- **AppSync_Context**: The full context object AppSync passes to JS resolvers, containing arguments, source, identity, info, request, stash, and prev
- **Lambda_Annotations**: The AWS Lambda Annotations framework that provides source-generated routing for Lambda functions via the `ANNOTATIONS_HANDLER` environment variable
- **CDK_Example**: The TypeScript CDK stack in `cdk-example/` that reads `resolvers.json` and deploys the AppSync API with Lambda resolvers
- **AOT**: Ahead-of-Time compilation; requires no runtime reflection, using `System.Text.Json` source generators for serialization
- **Selection_Set**: The list of GraphQL fields requested by the client, available via `ctx.info.selectionSetList` in AppSync context

## Requirements

### Requirement 1: Runtime Package Project Structure

**User Story:** As a developer, I want a new `Oproto.Lambda.GraphQL.Runtime` NuGet package targeting net8.0 and net10.0, so that I can add runtime AppSync context support to my Lambda functions.

#### Acceptance Criteria

1. THE Runtime_Package SHALL target both `net8.0` and `net10.0` frameworks
2. THE Runtime_Package SHALL depend on `System.Text.Json` for serialization
3. THE Runtime_Package SHALL have no dependency on reflection-based serialization
4. THE Runtime_Package SHALL be added to the `Oproto.Lambda.GraphQL.sln` solution file
5. THE Runtime_Package SHALL use the namespace `Oproto.Lambda.GraphQL.Runtime`

### Requirement 2: AppSync Resolver Context Model

**User Story:** As a Lambda developer, I want a typed `AppSyncResolverContext<TArguments>` model, so that I can receive the full AppSync context in my resolver functions with strongly-typed arguments.

#### Acceptance Criteria

1. THE Runtime_Package SHALL provide a generic `AppSyncResolverContext<TArguments>` class with properties for `Arguments`, `Source`, `Identity`, `Info`, `Request`, `Stash`, and `Prev`
2. THE `AppSyncResolverContext<TArguments>` SHALL deserialize the `Arguments` property into the generic type parameter `TArguments`
3. THE `AppSyncResolverContext<TArguments>` SHALL deserialize the `Source` property as `JsonElement?` to support arbitrary parent object shapes
4. THE `AppSyncResolverContext<TArguments>` SHALL deserialize the `Stash` property as `JsonElement?` to support arbitrary pipeline stash data
5. THE `AppSyncResolverContext<TArguments>` SHALL deserialize the `Prev` property as `JsonElement?` to support arbitrary previous function results
6. WHEN any optional context property is absent from the JSON payload, THE `AppSyncResolverContext<TArguments>` SHALL default that property to null without throwing an exception
7. FOR ALL valid `AppSyncResolverContext<TArguments>` instances, serializing then deserializing SHALL produce an equivalent object (round-trip property)

### Requirement 3: AppSync Identity Models

**User Story:** As a Lambda developer, I want typed identity models for all AppSync authentication modes, so that I can access caller identity information without manual JSON parsing.

#### Acceptance Criteria

1. THE Runtime_Package SHALL provide an `AppSyncIdentity` base class with common identity properties
2. THE Runtime_Package SHALL provide a `CognitoUserPoolsIdentity` subtype with `Sub`, `Issuer`, `Username`, `Claims`, `DefaultAuthStrategy`, and `Groups` properties
3. THE Runtime_Package SHALL provide an `IamIdentity` subtype with `AccountId`, `CognitoIdentityPoolId`, `CognitoIdentityId`, `SourceIp`, and `UserArn` properties
4. THE Runtime_Package SHALL provide an `OidcIdentity` subtype with `Sub`, `Issuer`, and `Claims` properties
5. THE Runtime_Package SHALL provide a `LambdaAuthorizerIdentity` subtype with `ResolverContext` as a `Dictionary<string, string>` property
6. WHEN the `identity` field in the JSON payload contains Cognito-specific properties, THE Runtime_Package SHALL deserialize it as `CognitoUserPoolsIdentity`
7. WHEN the `identity` field in the JSON payload contains IAM-specific properties, THE Runtime_Package SHALL deserialize it as `IamIdentity`
8. WHEN the `identity` field in the JSON payload contains OIDC-specific properties, THE Runtime_Package SHALL deserialize it as `OidcIdentity`
9. WHEN the `identity` field in the JSON payload contains a `resolverContext` property, THE Runtime_Package SHALL deserialize it as `LambdaAuthorizerIdentity`
10. WHEN the `identity` field is null or absent, THE Runtime_Package SHALL set the Identity property to null

### Requirement 4: AppSync Info Model

**User Story:** As a Lambda developer, I want access to the GraphQL field info including selection sets, so that I can optimize data fetching based on requested fields.

#### Acceptance Criteria

1. THE Runtime_Package SHALL provide an `AppSyncInfo` class with `FieldName`, `ParentTypeName`, `SelectionSetList`, and `SelectionSetGraphQL` properties
2. THE `SelectionSetList` property SHALL be a `List<string>` containing the flattened list of requested fields (e.g., `["id", "name", "category/name"]`)
3. THE `SelectionSetGraphQL` property SHALL be a nullable `string` containing the raw GraphQL selection set text
4. WHEN the `info` field is absent from the JSON payload, THE `AppSyncResolverContext<TArguments>` SHALL set the Info property to null

### Requirement 5: AppSync Request Model

**User Story:** As a Lambda developer, I want access to the AppSync request headers, so that I can read client-provided HTTP headers in my resolver functions.

#### Acceptance Criteria

1. THE Runtime_Package SHALL provide an `AppSyncRequest` class with a `Headers` property of type `Dictionary<string, string>`
2. WHEN the `request` field is absent from the JSON payload, THE `AppSyncResolverContext<TArguments>` SHALL set the Request property to null
3. WHEN the `headers` field within `request` is an empty object, THE `AppSyncRequest` SHALL contain an empty `Headers` dictionary

### Requirement 6: AOT-Compatible JSON Serialization

**User Story:** As a developer deploying to Native AOT Lambda, I want the runtime models to serialize and deserialize without reflection, so that my functions work in AOT environments.

#### Acceptance Criteria

1. THE Runtime_Package SHALL provide a `JsonSerializerContext` subclass with `[JsonSerializable]` attributes for all public model types
2. THE Runtime_Package SHALL use `System.Text.Json` source-generated serialization for all model types
3. THE Runtime_Package SHALL use `camelCase` property naming policy to match the AppSync JSON payload format
4. WHEN deserializing an `AppSyncResolverContext<TArguments>` with the provided `JsonSerializerContext`, THE Runtime_Package SHALL produce a correctly populated object without using reflection
5. THE Runtime_Package SHALL provide a convenience `Deserialize<TArguments>` static method that accepts a JSON string or `Stream` and returns an `AppSyncResolverContext<TArguments>`

### Requirement 7: Source Generator Full-Context Resolver Code

**User Story:** As a developer, I want the source generator to always emit full-context JS resolver code in `resolvers.json`, so that all AppSync context is available to Lambda functions by default.

#### Acceptance Criteria

1. THE Source_Generator SHALL emit JS resolver request code that sends the full AppSync_Context (arguments, source, identity, info, request, stash, prev) as the Lambda payload for all unit resolvers
2. THE Resolver_Manifest SHALL include a `resolverCode` property on each unit resolver entry containing the inline JS resolver request and response handler code
3. THE Source_Generator SHALL remove the `usesLambdaContext` flag from the Resolver_Manifest since all resolvers now send full context
4. THE JS_Resolver_Code response handler SHALL propagate errors from `ctx.error` using `util.error()`
5. THE JS_Resolver_Code response handler SHALL return `ctx.result` on success

### Requirement 8: CDK Example Update

**User Story:** As a developer using the CDK example, I want the CDK stack to use the resolver code from `resolvers.json` instead of generating it inline, so that the deployment matches the source generator output.

#### Acceptance Criteria

1. WHEN a resolver entry in the Resolver_Manifest contains a `resolverCode` property, THE CDK_Example SHALL use that code for the AppSync resolver instead of generating inline JS
2. THE CDK_Example SHALL remove the conditional logic that switches between arguments-only and full-context JS resolver code
3. THE CDK_Example SHALL update the Lambda runtime from `DOTNET_6` to `DOTNET_8`

### Requirement 9: Backward Compatibility

**User Story:** As a developer with existing Lambda functions that receive arguments-only payloads, I want a clear migration path, so that I can adopt the new runtime without breaking existing functions.

#### Acceptance Criteria

1. WHEN a Lambda function parameter type is `AppSyncResolverContext<TArguments>`, THE Lambda_Annotations framework SHALL pass the entire JSON payload through for deserialization as a single parameter
2. IF Lambda_Annotations does not support single complex parameter deserialization, THEN THE Runtime_Package SHALL provide a `DeserializeFromStream` helper method that accepts a `Stream` and returns `AppSyncResolverContext<TArguments>` for manual deserialization
3. THE Runtime_Package SHALL document the migration path from arguments-only functions to full-context functions in XML documentation comments

### Requirement 10: Unit Tests for Deserialization

**User Story:** As a developer, I want comprehensive unit tests for all context deserialization scenarios, so that I can trust the runtime models handle real AppSync payloads correctly.

#### Acceptance Criteria

1. THE test suite SHALL include deserialization tests for `AppSyncResolverContext<TArguments>` with all context properties populated
2. THE test suite SHALL include deserialization tests for each identity subtype (Cognito, IAM, OIDC, Lambda authorizer)
3. THE test suite SHALL include deserialization tests for payloads with missing optional properties (source, identity, info, request, stash, prev)
4. THE test suite SHALL include deserialization tests for `AppSyncInfo` with `SelectionSetList` containing nested field paths
5. THE test suite SHALL include deserialization tests for `AppSyncRequest` with populated and empty headers
6. THE test suite SHALL include a round-trip property test verifying that serializing then deserializing `AppSyncResolverContext<TArguments>` produces an equivalent object
7. THE test suite SHALL verify AOT-compatible deserialization using the provided `JsonSerializerContext`
