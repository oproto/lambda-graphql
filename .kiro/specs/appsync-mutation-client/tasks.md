# Implementation Plan: AppSync Mutation Client

## Overview

Implement the `Oproto.Lambda.GraphQL.Client` package — a lightweight AppSync HTTP client for sending GraphQL operations with IAM (SigV4) and API Key authentication. The implementation follows the design document's architecture: project setup, data models, serialization, authentication, retry logic, main client class, and tests. Each task builds incrementally, wiring components together as they are created.

## Tasks

- [x] 1. Create project and solution structure
  - [x] 1.1 Create the `Oproto.Lambda.GraphQL.Client` project
    - Create `Oproto.Lambda.GraphQL.Client/Oproto.Lambda.GraphQL.Client.csproj` targeting `net8.0;net10.0`
    - Add `AWSSDK.Core` and `Microsoft.Extensions.Logging.Abstractions` package references (no `Version` attributes — centralized package management)
    - Add `AWSSDK.Core` and `Microsoft.Extensions.Logging.Abstractions` entries to `Directory.Packages.props`
    - Set `PackageId`, `AssemblyName`, `RootNamespace` to `Oproto.Lambda.GraphQL.Client`
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.6, 1.7_
  - [x] 1.2 Add the project to the solution and test project
    - Add `Oproto.Lambda.GraphQL.Client` to `Oproto.Lambda.GraphQL.sln`
    - Add a `ProjectReference` to `Oproto.Lambda.GraphQL.Client` in `Oproto.Lambda.GraphQL.Tests.csproj`
    - _Requirements: 1.5_

- [x] 2. Implement configuration and data models
  - [x] 2.1 Create `AuthMode` enum
    - Create `Oproto.Lambda.GraphQL.Client/AuthMode.cs` with `Iam` and `ApiKey` values
    - _Requirements: 2.2_
  - [x] 2.2 Create `AppSyncClientOptions` class
    - Create `Oproto.Lambda.GraphQL.Client/AppSyncClientOptions.cs`
    - `required string Endpoint`, `AuthMode AuthMode = AuthMode.Iam`, `string? ApiKey`, `string? Region`, `int MaxRetries = 3`, `HttpClient? HttpClient`, `JsonSerializerContext? JsonSerializerContext`
    - _Requirements: 2.1, 2.2, 2.3, 2.5, 2.6, 2.7, 7.2_
  - [x] 2.3 Create `GraphQLError` and `GraphQLErrorLocation` models
    - Create `Oproto.Lambda.GraphQL.Client/GraphQLError.cs` and `Oproto.Lambda.GraphQL.Client/GraphQLErrorLocation.cs`
    - Include `[JsonPropertyName]` attributes for `message`, `errorType`, `path`, `locations`, `line`, `column`
    - _Requirements: 6.2_
  - [x] 2.4 Create `AppSyncResponse<TResult>` class
    - Create `Oproto.Lambda.GraphQL.Client/AppSyncResponse.cs`
    - Properties: `Data`, `Errors`, `HasErrors`, `IsSuccess`, `StatusCode`, `RawBody`, `Exception`
    - `[JsonIgnore]` on computed and HTTP-level properties
    - _Requirements: 6.1, 6.5, 6.6_

- [x] 3. Implement serialization layer
  - [x] 3.1 Create internal request/response models and JSON context
    - Create `Oproto.Lambda.GraphQL.Client/Serialization/AppSyncClientJsonContext.cs`
    - Define internal `GraphQLRequestBody` with `[JsonPropertyName("query")]`, `[JsonPropertyName("variables")]`, and `[JsonIgnore(Condition = WhenWritingNull)]` on `Variables`
    - Define internal `AppSyncResponseEnvelope` with `JsonElement? Data` and `List<GraphQLError>? Errors`
    - Create `AppSyncClientJsonContext` partial class with `[JsonSerializable]` attributes for all internal types
    - Use `camelCase` property naming policy via `[JsonSourceGenerationOptions]`
    - _Requirements: 7.1, 7.5, 3.6_
  - [x] 3.2 Write property test for request body round-trip
    - **Property 1: Request body round-trip consistency**
    - Create `Oproto.Lambda.GraphQL.Tests/Client/Generators/GraphQLArbitraries.cs` with FsCheck generators for `GraphQLRequestBody`
    - Create `Oproto.Lambda.GraphQL.Tests/Client/AppSyncResponsePropertyTests.cs`
    - Verify: serialize `GraphQLRequestBody` → deserialize → produces equivalent object
    - **Validates: Requirements 3.7, 11.9**

- [x] 4. Checkpoint - Verify project builds
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Implement authentication
  - [x] 5.1 Implement IAM SigV4 signing
    - Add SigV4 signing logic in `AppSyncClient` using `AWS4Signer` from `AWSSDK.Core`
    - Resolve credentials via `FallbackCredentialsFactory.GetCredentials()`
    - Sign with `appsync` service name and configured region
    - Add `Authorization`, `X-Amz-Date`, and optionally `X-Amz-Security-Token` headers
    - Return failed response with descriptive error when credentials are unavailable
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_
  - [x] 5.2 Implement API Key authentication
    - Add `x-api-key` header when `AuthMode.ApiKey` is configured
    - Skip SigV4 signing for API Key mode
    - Return failed response when `ApiKey` is null or empty in `ApiKey` mode
    - _Requirements: 5.1, 5.2, 5.3_

- [x] 6. Implement retry logic
  - [x] 6.1 Implement exponential backoff with jitter
    - Add internal retry method with configurable max retries
    - Base delay 100ms × 2^attempt with ±50% random jitter
    - Retry on HTTP 5xx, `TaskCanceledException` (timeout, not cancellation), `HttpRequestException`
    - Do not retry on HTTP 4xx or when `CancellationToken` is cancelled
    - Return last error after all retries exhausted
    - Log retry attempts at `Warning` level via `ILogger`
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

- [x] 7. Implement AppSyncClient main class
  - [x] 7.1 Implement `AppSyncClient` constructor and `IDisposable`
    - Create `Oproto.Lambda.GraphQL.Client/AppSyncClient.cs`
    - Constructor validates `Endpoint` is not null/empty (throws `ArgumentException`)
    - Accept `AppSyncClientOptions` and optional `ILogger<AppSyncClient>?`
    - Create internal `HttpClient` if none supplied; track ownership for disposal
    - `Dispose()` disposes only internally-created `HttpClient`
    - _Requirements: 9.1, 10.1, 10.2, 10.3_
  - [x] 7.2 Implement `SendAsync<TResult>` method
    - Build `GraphQLRequestBody` with `query` and optional `variables`
    - Serialize using internal JSON context (variables use caller context if provided, else default camelCase)
    - Send HTTP POST with `Content-Type: application/json`
    - Apply authentication (IAM or API Key) before sending
    - Wrap in retry logic
    - On 2xx: deserialize response envelope, extract `Data` using caller context or default, map to `AppSyncResponse<TResult>`
    - On non-2xx after retries: return response with `StatusCode`, `RawBody`
    - On deserialization failure: return response with `RawBody`
    - On network exception after retries: return response with `Exception`
    - _Requirements: 3.1, 3.2, 3.3, 3.6, 6.3, 6.4, 6.7, 7.3, 7.4, 9.2, 9.3, 9.4, 9.5_
  - [x] 7.3 Implement `MutateAsync<TResult>` and `QueryAsync<TResult>` convenience methods
    - Both delegate directly to `SendAsync<TResult>`
    - _Requirements: 3.4, 3.5_

- [x] 8. Checkpoint - Verify project builds and compiles cleanly
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Implement unit tests
  - [x] 9.1 Write request construction tests
    - Create `Oproto.Lambda.GraphQL.Tests/Client/AppSyncClientTests.cs`
    - Test HTTP POST body contains `query` and `variables` fields
    - Test `variables` field is omitted when null
    - Test `Content-Type` header is `application/json`
    - _Requirements: 11.1, 3.2, 3.3, 3.6_
  - [x] 9.2 Write API Key authentication tests
    - Test `x-api-key` header is included when `AuthMode.ApiKey`
    - Test failed response when `ApiKey` is null/empty
    - _Requirements: 11.2, 5.1, 5.3_
  - [x] 9.3 Write response deserialization tests
    - Test successful GraphQL response deserializes into typed `TResult`
    - Test GraphQL error responses with `errors` array
    - Test mixed responses (both `data` and `errors` present)
    - Test non-JSON response bodies return `RawBody`
    - _Requirements: 11.3, 11.4, 11.5, 11.8, 6.3, 6.4_
  - [x] 9.4 Write retry behavior tests
    - Create `Oproto.Lambda.GraphQL.Tests/Client/AppSyncClientRetryTests.cs`
    - Test retry on HTTP 5xx transient failures
    - Test no retry on HTTP 4xx client errors
    - Test retry exhaustion returns last error
    - _Requirements: 11.6, 11.7, 8.1, 8.3, 8.5_
  - [x] 9.5 Write IDisposable behavior tests
    - Create `Oproto.Lambda.GraphQL.Tests/Client/AppSyncClientDisposableTests.cs`
    - Test internally-created `HttpClient` is disposed
    - Test caller-supplied `HttpClient` is NOT disposed
    - _Requirements: 11.10, 10.2, 10.3_

- [x] 10. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties
- The project uses centralized package management (`Directory.Packages.props`) — package references in `.csproj` must NOT include `Version` attributes
- `System.Text.Json` is in the shared framework for `net8.0`/`net10.0` — no explicit package reference needed
- Tests go in the existing `Oproto.Lambda.GraphQL.Tests` project under a `Client/` subfolder
