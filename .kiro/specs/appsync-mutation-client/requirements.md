# Requirements Document

## Introduction

This spec covers the `Oproto.Lambda.GraphQL.Client` package: a lightweight AppSync HTTP client for sending GraphQL operations (primarily mutations) from backend processes such as EventBridge handlers, SQS processors, and other Lambda functions. The primary use case is triggering AppSync subscriptions by calling mutations against the AppSync GraphQL endpoint.

The client supports IAM (SigV4) and API Key authentication modes, uses `System.Text.Json` source generators for AOT compatibility, and includes retry logic for transient failures. It is completely independent of the Runtime and SourceGenerator packages — it has no dependency on the rest of the Oproto.Lambda.GraphQL library.

The implementation sends GraphQL operations over HTTP POST. While mutations are the documented primary use case, queries use the identical HTTP mechanism and are supported by the same code path. This is not a full GraphQL client framework — there is no caching, no WebSocket subscription support, and no schema introspection.

## Glossary

- **Client_Package**: The new `Oproto.Lambda.GraphQL.Client` NuGet package (net8.0;net10.0) providing a lightweight AppSync HTTP client
- **AppSync_Client**: The `AppSyncClient` class that sends GraphQL operations to an AppSync endpoint over HTTP POST
- **AppSync_Endpoint**: The AWS AppSync GraphQL HTTPS URL that accepts GraphQL operations
- **SigV4_Signing**: AWS Signature Version 4 request signing used for IAM authentication against AWS services
- **Auth_Mode**: The authentication mode used when calling the AppSync_Endpoint, either IAM (SigV4) or API Key
- **GraphQL_Operation**: A GraphQL query or mutation string sent as the `query` field in the HTTP POST body
- **GraphQL_Variables**: A JSON object of variable values sent alongside a GraphQL_Operation
- **GraphQL_Response**: The JSON response from AppSync containing `data` and/or `errors` fields per the GraphQL specification
- **Transient_Failure**: A temporary HTTP error (5xx status codes, request timeouts, network errors) that may succeed on retry
- **AOT**: Ahead-of-Time compilation; requires no runtime reflection, using `System.Text.Json` source generators for serialization
- **JsonSerializerContext**: A `System.Text.Json.Serialization.JsonSerializerContext` subclass providing AOT-compatible type metadata for serialization

## Requirements

### Requirement 1: Client Package Project Structure

**User Story:** As a developer, I want a new `Oproto.Lambda.GraphQL.Client` NuGet package targeting net8.0 and net10.0, so that I can send GraphQL operations to AppSync from my backend services.

#### Acceptance Criteria

1. THE Client_Package SHALL target both `net8.0` and `net10.0` frameworks
2. THE Client_Package SHALL depend on `AWSSDK.Core` v4.x for SigV4_Signing credential resolution
3. THE Client_Package SHALL depend on `System.Text.Json` for serialization
4. THE Client_Package SHALL have no dependency on reflection-based serialization
5. THE Client_Package SHALL be added to the `Oproto.Lambda.GraphQL.sln` solution file
6. THE Client_Package SHALL use the namespace `Oproto.Lambda.GraphQL.Client`
7. THE Client_Package SHALL have no dependency on any other `Oproto.Lambda.GraphQL` package

### Requirement 2: AppSync Client Configuration

**User Story:** As a developer, I want to configure the AppSync client with an endpoint URL and authentication mode, so that I can connect to my AppSync API with the correct credentials.

#### Acceptance Criteria

1. THE Client_Package SHALL provide an `AppSyncClientOptions` class with a required `Endpoint` property of type `string` for the AppSync GraphQL URL
2. THE `AppSyncClientOptions` SHALL provide an `AuthMode` property that accepts values `Iam` or `ApiKey`
3. WHEN Auth_Mode is `ApiKey`, THE `AppSyncClientOptions` SHALL require an `ApiKey` property of type `string`
4. WHEN Auth_Mode is `Iam`, THE AppSync_Client SHALL use the default AWS credential chain for SigV4_Signing
5. THE `AppSyncClientOptions` SHALL provide an optional `Region` property of type `string` that defaults to the `AWS_REGION` environment variable
6. THE `AppSyncClientOptions` SHALL provide an optional `MaxRetries` property of type `int` that defaults to 3
7. THE `AppSyncClientOptions` SHALL provide an optional `HttpClient` property so that callers can supply a pre-configured `HttpClient` instance for connection reuse and testing

### Requirement 3: Sending GraphQL Operations

**User Story:** As a backend developer, I want to send GraphQL mutations (and queries) to AppSync with typed response deserialization, so that I can trigger subscriptions and retrieve data from my resolvers.

#### Acceptance Criteria

1. THE AppSync_Client SHALL provide a `SendAsync<TResult>` method that accepts a GraphQL_Operation string, a variables object, and a `CancellationToken`, and returns an `AppSyncResponse<TResult>`
2. THE AppSync_Client SHALL send the GraphQL_Operation as an HTTP POST request to the configured AppSync_Endpoint with a JSON body containing `query` and `variables` fields
3. THE AppSync_Client SHALL set the `Content-Type` header to `application/json` on all requests
4. THE AppSync_Client SHALL provide a convenience `MutateAsync<TResult>` method that delegates to `SendAsync<TResult>` for mutation operations
5. THE AppSync_Client SHALL provide a convenience `QueryAsync<TResult>` method that delegates to `SendAsync<TResult>` for query operations
6. WHEN the variables parameter is null, THE AppSync_Client SHALL omit the `variables` field from the request body
7. FOR ALL valid GraphQL_Operation strings and variables, serializing the request body then deserializing it SHALL produce an equivalent request object (round-trip property)

### Requirement 4: IAM Authentication with SigV4

**User Story:** As a developer deploying to AWS, I want the client to automatically sign requests with SigV4 using the Lambda execution role, so that I can call IAM-authorized AppSync endpoints without manual credential management.

#### Acceptance Criteria

1. WHEN Auth_Mode is `Iam`, THE AppSync_Client SHALL sign each HTTP request using SigV4_Signing with the `appsync` service name
2. THE AppSync_Client SHALL resolve AWS credentials using the default credential provider chain from `AWSSDK.Core`
3. THE AppSync_Client SHALL use the configured `Region` for the SigV4 signing region
4. WHEN AWS credentials are expired or unavailable, THE AppSync_Client SHALL return a failed result with a descriptive error message
5. THE SigV4_Signing SHALL include the request body in the signature calculation

### Requirement 5: API Key Authentication

**User Story:** As a developer using API Key-authorized AppSync endpoints, I want the client to include the API key in requests, so that I can authenticate without IAM.

#### Acceptance Criteria

1. WHEN Auth_Mode is `ApiKey`, THE AppSync_Client SHALL include the API key in the `x-api-key` HTTP header on each request
2. WHEN Auth_Mode is `ApiKey`, THE AppSync_Client SHALL NOT apply SigV4_Signing to the request
3. IF Auth_Mode is `ApiKey` and the `ApiKey` property is null or empty, THEN THE AppSync_Client SHALL return a failed result with a descriptive error message

### Requirement 6: Response Handling

**User Story:** As a developer, I want typed response objects that clearly distinguish between successful data and GraphQL errors, so that I can handle both cases in my code.

#### Acceptance Criteria

1. THE Client_Package SHALL provide an `AppSyncResponse<TResult>` class with a `Data` property of type `TResult?` and an `Errors` property of type `List<GraphQLError>?`
2. THE `GraphQLError` class SHALL contain `Message`, `ErrorType`, `Path`, and `Locations` properties matching the GraphQL error specification
3. WHEN the AppSync response JSON contains a `data` field, THE AppSync_Client SHALL deserialize it into the `TResult` type
4. WHEN the AppSync response JSON contains an `errors` array, THE AppSync_Client SHALL deserialize each entry into a `GraphQLError` instance
5. THE `AppSyncResponse<TResult>` SHALL provide a `HasErrors` property that returns true when the `Errors` list is non-null and non-empty
6. THE `AppSyncResponse<TResult>` SHALL provide an `IsSuccess` property that returns true when `Data` is non-null and `HasErrors` is false
7. WHEN the HTTP response status code indicates a non-GraphQL error (non-200 status), THE AppSync_Client SHALL return a failed result containing the HTTP status code and response body

### Requirement 7: AOT-Compatible Serialization

**User Story:** As a developer deploying to Native AOT Lambda, I want the client to serialize and deserialize without reflection, so that my functions work in AOT environments.

#### Acceptance Criteria

1. THE Client_Package SHALL provide a `JsonSerializerContext` subclass with `[JsonSerializable]` attributes for all internal model types (request body, response envelope, GraphQL errors)
2. THE `AppSyncClientOptions` SHALL provide an optional `JsonSerializerContext` property so that callers can supply their own context for AOT-compatible serialization of custom `TResult` and variables types
3. WHEN a caller-supplied `JsonSerializerContext` is provided, THE AppSync_Client SHALL use it for serializing variables and deserializing the `data` field of the response
4. WHEN no caller-supplied `JsonSerializerContext` is provided, THE AppSync_Client SHALL fall back to default `System.Text.Json` serialization with `camelCase` property naming
5. THE Client_Package SHALL use `camelCase` property naming policy for all internal serialization

### Requirement 8: Retry Logic for Transient Failures

**User Story:** As a developer, I want the client to automatically retry on transient failures, so that temporary network issues or service hiccups do not cause my operations to fail.

#### Acceptance Criteria

1. WHEN a Transient_Failure occurs (HTTP 5xx, request timeout, or `HttpRequestException`), THE AppSync_Client SHALL retry the request up to the configured `MaxRetries` count
2. THE AppSync_Client SHALL use exponential backoff with jitter between retry attempts
3. THE AppSync_Client SHALL NOT retry on HTTP 4xx responses (client errors)
4. THE AppSync_Client SHALL NOT retry when the `CancellationToken` is cancelled
5. IF all retry attempts are exhausted, THEN THE AppSync_Client SHALL return a failed result containing the last error encountered
6. THE AppSync_Client SHALL log retry attempts at the `Warning` level using `ILogger` when a logger is provided

### Requirement 9: Error Handling

**User Story:** As a developer, I want clear, typed error information when operations fail, so that I can diagnose and handle failures appropriately.

#### Acceptance Criteria

1. IF the AppSync_Endpoint URL is null or empty, THEN THE AppSync_Client SHALL throw an `ArgumentException` during construction
2. IF the HTTP request fails with a non-transient error, THEN THE AppSync_Client SHALL return a failed result containing the HTTP status code and response body
3. IF the response body cannot be deserialized as a GraphQL_Response, THEN THE AppSync_Client SHALL return a failed result containing the raw response body
4. THE AppSync_Client SHALL NOT throw exceptions for GraphQL-level errors; GraphQL errors SHALL be returned in the `AppSyncResponse<TResult>.Errors` property
5. IF a network-level exception occurs after all retries are exhausted, THEN THE AppSync_Client SHALL return a failed result wrapping the underlying exception

### Requirement 10: Disposable Resource Management

**User Story:** As a developer, I want the client to properly manage HTTP resources, so that connections are cleaned up when the client is no longer needed.

#### Acceptance Criteria

1. THE AppSync_Client SHALL implement `IDisposable`
2. WHEN the AppSync_Client creates its own `HttpClient` internally, THE AppSync_Client SHALL dispose of the `HttpClient` when `Dispose()` is called
3. WHEN a caller supplies an `HttpClient` via `AppSyncClientOptions`, THE AppSync_Client SHALL NOT dispose of the caller-supplied `HttpClient`

### Requirement 11: Unit Tests

**User Story:** As a developer, I want comprehensive unit tests for request construction, authentication, response parsing, and retry behavior, so that I can trust the client handles all scenarios correctly.

#### Acceptance Criteria

1. THE test suite SHALL include tests for constructing the HTTP POST request body with `query` and `variables` fields
2. THE test suite SHALL include tests for API Key authentication header inclusion
3. THE test suite SHALL include tests for deserializing successful GraphQL responses into typed `TResult` objects
4. THE test suite SHALL include tests for deserializing GraphQL error responses with `errors` array
5. THE test suite SHALL include tests for handling mixed responses (both `data` and `errors` present)
6. THE test suite SHALL include tests for retry behavior on transient HTTP 5xx failures
7. THE test suite SHALL include tests for non-retry on HTTP 4xx failures
8. THE test suite SHALL include tests for handling non-JSON response bodies
9. THE test suite SHALL include a round-trip property test verifying that serializing then deserializing the GraphQL request body produces an equivalent object
10. THE test suite SHALL include tests for `IDisposable` behavior with both internally-created and caller-supplied `HttpClient` instances
