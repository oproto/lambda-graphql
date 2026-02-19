# Oproto.Lambda.GraphQL — Development Plan

## Current State (v0.1.0)

What we have today:
- Roslyn source generator that produces `schema.graphql` and `resolvers.json` from C# attributes
- 16 GraphQL attributes covering types, operations, directives, auth, scalars
- MSBuild task for post-build schema extraction (AOT-compatible)
- CDK example showing deployment pattern
- Zero runtime dependencies — pure compile-time generation

What we don't have:
- No runtime library at all — once the schema is generated, developers are on their own
- The resolver workaround in CDK rewrites the payload to pass just arguments (or a single value), which means Lambda functions can't access AppSync context (identity, source, selectionSetList, request headers, etc.)
- No field selection / request shaping support
- No subscription mutation helper (pushing data to AppSync to trigger subscriptions)
- Everything targets .NET 6 / netstandard2.0

---

## Problem Analysis

### 1. The Resolver Context Problem

AppSync sends a rich context object to Lambda resolvers via the JS resolver code:

```
ctx.arguments    — the GraphQL arguments
ctx.source       — parent object (for nested resolvers)
ctx.identity     — caller identity (Cognito, IAM, OIDC, Lambda)
ctx.info         — field info including selectionSetList, selectionSetGraphQL
ctx.request      — request headers
ctx.stash        — pipeline stash
ctx.prev         — previous function result (pipeline)
```

Our current CDK workaround generates JS resolver code that sends ONLY `ctx.arguments` as the Lambda payload. This was done because Lambda Annotations expects clean parameter binding — if you have `GetProduct(Guid id)`, the payload needs to be just the id value or `{"id": "..."}`.

The problem: you lose everything else. No identity, no selection set, no source, no headers.

The `usesLambdaContext` flag in resolvers.json was a partial fix — when true, the JS resolver sends a richer payload with `field`, `arguments`, `source`, `identity`, `request`. But there's no C# runtime support to deserialize or work with this.

What we need: A way for Lambda functions to receive the full AppSync context while still having clean parameter binding for the actual arguments. This needs to be AOT-compatible (no reflection).

### 2. The Field Selection / Request Shaping Problem

GraphQL's power is that clients request only the fields they need. But on the backend, we currently fetch everything and let AppSync filter the response. This wastes:
- Database read capacity (reading all attributes when only 3 are needed)
- Network bandwidth (Lambda → AppSync)
- Serialization time

The challenge is multi-layered:
- **Extraction**: Parse `selectionSetList` from AppSync context into something usable
- **Propagation**: Pass field selection through the service layer without coupling everything to GraphQL
- **Database mapping**: GraphQL field names ≠ C# property names ≠ DynamoDB attribute names
- **Partial application**: Sometimes the backend MUST fetch everything (computed fields, authorization checks) and only shape the response before serialization
- **Multiple database backends**: Need to support raw DynamoDB SDK, FluentDynamoDB, and potentially others

### 3. The Subscription Mutation Problem

AppSync subscriptions are triggered by mutations. To push real-time updates from backend processes (EventBridge handlers, SQS processors, etc.), you need to call an AppSync mutation. This requires:
- A GraphQL client that supports IAM SigV4 authentication
- Constructing the mutation query
- Handling the HTTP call to the AppSync endpoint

Existing .NET GraphQL clients are heavy, don't play well with SigV4, and aren't AOT-friendly. We need a lightweight, focused client that just does mutation calls with SigV4 auth.

### 4. The .NET Version Problem

Current state:
- Main package + Source Generator: `netstandard2.0` (correct for Roslyn analyzers)
- Build task: `net6.0`
- Tests + Examples: `net6.0`

The source generator and main attributes package MUST stay on `netstandard2.0` — that's a Roslyn requirement for analyzers. But the Build task, tests, examples, and any new runtime libraries should target modern .NET.

---

## Proposed Package Architecture

```
Oproto.Lambda.GraphQL                          (existing, netstandard2.0)
  └── Attributes, MSBuild integration, bundles generator

Oproto.Lambda.GraphQL.SourceGenerator          (existing, netstandard2.0)
  └── Roslyn incremental generator

Oproto.Lambda.GraphQL.Build                    (existing, net8.0 bump)
  └── MSBuild extraction task

Oproto.Lambda.GraphQL.Runtime                  (NEW, net8.0;net10.0)
  └── Core runtime: AppSync context model, deserialization,
      field selection abstraction, resolver base classes

Oproto.Lambda.GraphQL.Runtime.FluentDynamoDB   (NEW, net8.0;net10.0)
  └── FluentDynamoDB integration for field selection → projection

Oproto.Lambda.GraphQL.Client                   (NEW, net8.0;net10.0)
  └── Lightweight AppSync mutation client with SigV4 auth
```

### Why This Split?

- **Runtime** is the core package most users will add. It has no database opinions.
- **Runtime.FluentDynamoDB** is optional — only for teams using FluentDynamoDB who want automatic projection mapping.
- **Client** is completely independent — useful even without the rest of the library. An EventBridge handler that needs to push a subscription notification doesn't need the resolver runtime.
- The existing three packages stay untouched in their roles. The source generator remains netstandard2.0 (Roslyn requirement). The main attributes package stays netstandard2.0 (consumed by any TFM).

### Dependency Graph

```
User's Lambda Project
  ├── Oproto.Lambda.GraphQL              (compile-time only)
  ├── Oproto.Lambda.GraphQL.Runtime      (runtime)
  │     └── depends on: AWSSDK.Core (for identity models)
  │     └── depends on: System.Text.Json
  ├── Oproto.Lambda.GraphQL.Runtime.FluentDynamoDB  (optional, runtime)
  │     └── depends on: Runtime + Oproto.FluentDynamoDb
  └── Oproto.Lambda.GraphQL.Client       (optional, runtime)
        └── depends on: AWSSDK.Core (SigV4 signing)
        └── depends on: System.Text.Json
```

---

## Design Decisions & Approach

### Decision 1: How to Handle Resolver Context

**Approach: Always send full context, provide typed accessor**

The JS resolver code (generated into resolvers.json or CDK) should ALWAYS send the full AppSync context as the Lambda payload:

```javascript
export function request(ctx) {
  return {
    operation: 'Invoke',
    payload: {
      arguments: ctx.arguments,
      source: ctx.source,
      identity: ctx.identity,
      info: ctx.info,           // includes selectionSetList, selectionSetGraphQL
      request: ctx.request,
      stash: ctx.stash,
      prev: ctx.prev
    }
  };
}
```

On the C# side, the Runtime package provides:

```csharp
// The full context model (AOT-compatible, System.Text.Json)
public class AppSyncResolverContext<TArguments>
{
    public TArguments Arguments { get; set; }
    public JsonElement? Source { get; set; }
    public AppSyncIdentity? Identity { get; set; }
    public AppSyncInfo? Info { get; set; }
    public AppSyncRequest? Request { get; set; }
    public JsonElement? Stash { get; set; }
    public JsonElement? Prev { get; set; }
}

public class AppSyncInfo
{
    public string FieldName { get; set; }
    public string ParentTypeName { get; set; }
    public List<string> SelectionSetList { get; set; }
    public string? SelectionSetGraphQL { get; set; }
}
```

**The key insight**: Lambda Annotations is itself source-generated, so we can't wrap or intercept its parameter binding. Instead, the user's Lambda function receives the raw payload and uses our Runtime helpers inside the function body to parse it. Lambda Annotations still handles routing (via `ANNOTATIONS_HANDLER` env var), but the function signature takes a raw input type:

```csharp
[LambdaFunction]
[GraphQLQuery("getProduct")]
[GraphQLResolver]
public async Task<Product> GetProduct(AppSyncResolverContext<GetProductArgs> ctx)
{
    var id = ctx.Arguments.Id;
    var requestedFields = ctx.Info?.SelectionSetList;
    var callerSub = ctx.Identity?.Sub;
    
    return await _productService.GetAsync(id, requestedFields);
}
```

The `AppSyncResolverContext<T>` is a plain POCO that System.Text.Json can deserialize directly from the Lambda payload. Lambda Annotations will see it as a single parameter and pass the payload through. The Runtime package provides the typed model — no wrapping, no interception, no magic.

**Alternative considered**: Ambient context (like HttpContext.Current or ILambdaRequestContextAccessor). This would let functions keep clean signatures like `GetProduct(Guid id)` while accessing context via DI. However:
- Lambda Annotations is source-generated — we can't hook into its pipeline
- Ambient context requires scoped DI setup which adds complexity
- The explicit parameter approach is more discoverable and testable
- We can always add ambient context later as a convenience layer on top

### Decision 2: How to Handle Field Selection

**Approach: Three-layer abstraction**

```
Layer 1: GraphQL Selection Set (AppSync-specific)
  └── selectionSetList: ["id", "name", "price", "category/name"]

Layer 2: FieldSelection (database-agnostic abstraction)
  └── Typed, mappable, composable
  └── Lives in Runtime package
  └── Knows about GraphQL→C# property name mapping

Layer 3: Database Projection (database-specific)
  └── DynamoDB: ProjectionExpression + ExpressionAttributeNames
  └── FluentDynamoDB: .WithProjection() string
  └── SQL: SELECT column list
  └── Lives in database-specific packages
```

The `FieldSelection` abstraction:

```csharp
// Core abstraction in Runtime package
public class FieldSelection
{
    public IReadOnlySet<string> Fields { get; }          // C# property names
    public bool IsEmpty { get; }                          // true = select all
    
    // Factory methods
    public static FieldSelection All();                   // no filtering
    public static FieldSelection FromSelectionSet(       // from AppSync
        List<string> selectionSetList);
    public static FieldSelection Of(params string[] fields);
    
    // Query methods
    public bool IsRequested(string propertyName);
    public FieldSelection ForNestedType(string prefix);   // "category/name" → "name"
    
    // Mapping
    public FieldSelection MapTo<TTarget>(                 // GraphQL names → C# names
        FieldNameMap map);
}

// Name mapping (source-generated or manual)
public class FieldNameMap
{
    // GraphQL field name → C# property name → DB attribute name
    // Built from [GraphQLField] and [DynamoDbAttribute] at compile time
}
```

**Source generator enhancement**: The existing source generator can emit `FieldNameMap` instances for each `[GraphQLType]` class, mapping GraphQL field names to C# property names. This covers the GraphQL → API layer mapping.

**Cross-assembly / multi-layer mapping**: In real-world architectures, the GraphQL types (API layer), service models (service layer), and database entities (data layer) are separate classes in separate assemblies. The field selection needs to flow through all layers:

```
GraphQL field name  →  API model property  →  Service model property  →  DB attribute name
"displayName"       →  Product.Name        →  ProductDto.Name        →  "name" (DynamoDB)
```

The `FieldNameMap` must be composable and explicitly configured at each boundary, not auto-generated from co-located attributes (since the attributes live on different classes in different assemblies):

```csharp
// API layer: source-generated from [GraphQLField] attributes
var graphqlToApi = Product.GraphQLFieldMap;  // "displayName" → "Name"

// Service layer: manually defined or convention-based
var apiToService = FieldNameMap.Builder()
    .Map("Name", "Name")           // same name, explicit
    .Map("Price", "UnitPrice")     // different name
    .Build();

// Data layer: manually defined from DynamoDB attribute knowledge
var serviceToDb = FieldNameMap.Builder()
    .Map("Name", "name")           // C# prop → DynamoDB attribute
    .Map("UnitPrice", "price")
    .Build();

// Compose across layers
var graphqlToDb = graphqlToApi.Then(apiToService).Then(serviceToDb);
```

For the simple case where one class has both `[GraphQLField]` and `[DynamoDbAttribute]`, the FluentDynamoDB integration package can auto-generate the full map. But the architecture must support the multi-layer case as the primary design target.

**The "must fetch everything" escape hatch**: `FieldSelection.All()` means "I need everything." Services can check `selection.IsEmpty` and skip projection optimization. For response shaping (fetch all, return subset), we provide a serialization filter:

```csharp
// Shape response to only include requested fields
var product = await _repo.GetFullProduct(id);
return ctx.ShapeResponse(product);  // uses selectionSetList to filter JSON output
```

### Decision 3: Subscription Mutation Client

**Approach: Minimal HTTP client with SigV4**

```csharp
public class AppSyncClient
{
    public AppSyncClient(AppSyncClientOptions options);
    
    // Send a mutation to trigger subscriptions
    public Task<AppSyncResponse<TResult>> MutateAsync<TResult>(
        string mutation,
        object variables,
        CancellationToken ct = default);
    
    // Convenience for common pattern
    public Task<AppSyncResponse<TResult>> MutateAsync<TResult>(
        string mutationName,
        string selectionSet,
        object input,
        CancellationToken ct = default);
}

public class AppSyncClientOptions
{
    public string Endpoint { get; set; }          // AppSync GraphQL URL
    public AuthMode AuthMode { get; set; }        // IAM, ApiKey, etc.
    public string? ApiKey { get; set; }           // for API_KEY auth
    // IAM auth uses default credential chain automatically
}
```

Usage from an EventBridge handler:

```csharp
await _appSyncClient.MutateAsync<Order>(
    mutation: @"mutation UpdateOrder($input: UpdateOrderInput!) {
        updateOrder(input: $input) { id status }
    }",
    variables: new { input = new { id = orderId, status = "SHIPPED" } }
);
```

**SigV4 signing**: Use `AWSSDK.Core`'s built-in SigV4 signer rather than implementing our own. This keeps us aligned with AWS SDK updates and handles credential refresh, STS assume-role, etc.

**AOT compatibility**: Use `System.Text.Json` source generators for serialization. The client will accept a `JsonSerializerContext` for AOT scenarios.

**Scope**: The client sends GraphQL operations over HTTP — mutations are the primary use case (triggering subscriptions), but queries use the exact same HTTP POST mechanism. We're not building a full GraphQL client framework with caching, subscriptions, or schema introspection. Just a thin HTTP wrapper with SigV4 auth that can send any GraphQL operation string.

### Decision 4: .NET Version Strategy

**Source Generator + Attributes package**: Stay on `netstandard2.0`. This is non-negotiable — Roslyn requires it for analyzers. This already works across .NET 6-10.

**Build task**: Bump from `net6.0` to `net8.0`. MSBuild tasks run in the build host, and .NET 8 is the current LTS. .NET 6 goes out of support.

**New runtime packages**: Multi-target `net8.0;net10.0`. 
- net8.0 = current LTS, covers the majority of production workloads
- net10.0 = current LTS (GA November 2025), test and target from day one
- We don't need net6.0 (EOL), net7.0 (EOL), or net9.0 (STS, EOL November 2025)

**Tests**: Target `net10.0` (primary) and `net8.0` for backward compat validation.

**Examples**: Target `net10.0`.

---

## Spec Breakdown

### Spec 1: Runtime Core — AppSync Resolver Context (Foundation)

**Priority**: Highest — everything else depends on this.

**Scope**:
- Create `Oproto.Lambda.GraphQL.Runtime` project (net8.0;net10.0)
- Implement `AppSyncResolverContext<TArguments>` model
- Implement `AppSyncIdentity` (Cognito, IAM, OIDC, Lambda subtypes)
- Implement `AppSyncInfo` with `SelectionSetList` and `SelectionSetGraphQL`
- Implement `AppSyncRequest` (headers)
- AOT-compatible JSON serialization with `JsonSerializerContext`
- Update source generator to always emit full-context JS resolver code in resolvers.json
- Update CDK example to use new resolver code pattern
- Unit tests for deserialization of all context shapes

**Key design questions to resolve during spec**:
- How does Lambda Annotations handle a single complex parameter like `AppSyncResolverContext<T>`? Need to validate it passes the payload through for deserialization.
- If Annotations doesn't cooperate, do we fall back to `Stream` input with a manual deserialize helper?
- How do we handle backward compatibility for users who want the old arguments-only mode?

**Estimated complexity**: Medium

### Spec 2: Field Selection Abstraction

**Priority**: High — this is the main value-add for production use.

**Scope**:
- Implement `FieldSelection` in Runtime package
- Implement `FieldNameMap` for GraphQL → C# property name mapping
- Source generator enhancement: emit `FieldNameMap` for each `[GraphQLType]` class
- `AppSyncResolverContext` extension: `.GetFieldSelection()` convenience method
- Response shaping: `ShapeResponse<T>()` that filters serialized output to requested fields
- Nested field selection support (e.g., `"category/name"` → nested type selection)
- Unit tests for mapping, nesting, edge cases

**Does NOT include**: Database-specific projection. That's Spec 4.

**Estimated complexity**: Medium-High (source generator changes are the tricky part)

### Spec 3: AppSync Mutation Client

**Priority**: High — independent of Specs 1-2, can be built in parallel.

**Scope**:
- Create `Oproto.Lambda.GraphQL.Client` project (net8.0;net10.0)
- Implement `AppSyncClient` with SigV4 signing for IAM auth
- Support API Key auth mode
- AOT-compatible serialization
- Retry logic for transient failures
- Integration test example (manual, against real AppSync endpoint)
- Unit tests for request construction, signing, response parsing

**Key design questions**:
- Do we also support sending queries? (Technically the same HTTP POST — start with mutations as the documented use case, but the implementation naturally supports queries too since it's the same code path)
- Do we support batched mutations?

**Estimated complexity**: Medium

### Spec 4: FluentDynamoDB Field Selection Integration

**Priority**: Medium — depends on Spec 2.

**Scope**:
- Create `Oproto.Lambda.GraphQL.Runtime.FluentDynamoDB` project
- Implement `FieldSelection` → FluentDynamoDB `.WithProjection()` mapping
- `FieldNameMap.Builder()` for manual C# property → DynamoDB attribute name mapping
- For the simple case (single class with both `[GraphQLField]` and `[DynamoDbAttribute]`): provide a source generator or reflection-free helper that reads both attribute sets
- For the multi-layer case (separate API/service/data classes): provide `FieldNameMap.Then()` composition so maps can be chained across assembly boundaries
- Extension method: `FieldSelection.ToDynamoDbProjection(FieldNameMap map)` → returns projection string + attribute names
- Handle edge cases: computed fields, ignored fields, nested objects/maps
- Unit tests for both simple and multi-layer scenarios
- Documentation showing both patterns

**Key insight**: The multi-layer architecture (GraphQL types → service models → DynamoDB entities in separate assemblies) is the primary design target. Auto-generation from co-located attributes is a convenience for simpler projects, not the core path.

**Estimated complexity**: Medium-High (composable mapping is the hard part)

### Spec 5: Raw DynamoDB SDK Field Selection Support

**Priority**: Medium-Low — depends on Spec 2. Many users will use FluentDynamoDB instead.

**Scope**:
- Extension methods in Runtime package (no new project needed)
- `FieldSelection.ToProjectionExpression()` → returns `ProjectionExpression` string + `ExpressionAttributeNames` dictionary
- Manual `FieldNameMap` builder for users who don't use FluentDynamoDB attributes
- Documentation and examples

**Estimated complexity**: Low

### Spec 6: .NET Version Modernization

**Priority**: Medium — can be done early as a quick win, or alongside Spec 1.

**Scope**:
- Bump Build task from net6.0 to net8.0
- Bump Tests from net6.0 to net8.0
- Bump Examples from net6.0 to net8.0
- Add net10.0 TFM to new runtime packages (when SDK is stable)
- Update CI/CD if applicable
- Verify source generator still works across all target frameworks
- Update documentation references from .NET 6 to .NET 8+

**Estimated complexity**: Low

---

## Execution Order

```
Phase 1 (Foundation):
  Spec 6: .NET Version Modernization     ← quick win, do first
  Spec 1: Runtime Core                   ← everything depends on this

Phase 2 (Core Features — can parallelize):
  Spec 2: Field Selection Abstraction    ← depends on Spec 1
  Spec 3: AppSync Mutation Client        ← independent, parallel with Spec 2

Phase 3 (Database Integration):
  Spec 4: FluentDynamoDB Integration     ← depends on Spec 2
  Spec 5: Raw DynamoDB SDK Support       ← depends on Spec 2, lower priority
```

---

## Open Questions / Things to Validate

1. **Lambda Annotations parameter binding**: When the payload is the full AppSync context JSON, Lambda Annotations needs to deserialize it into the function's parameter. If the function takes `AppSyncResolverContext<GetProductArgs>`, Lambda Annotations should pass the entire payload through as a single parameter via System.Text.Json deserialization. We need to verify this works — specifically that Annotations doesn't try to do anything clever with the payload shape when there's a single complex parameter. If it doesn't work, the fallback is to take `Stream` and deserialize manually, which is less ergonomic but reliable.

2. **Source generator cross-package awareness**: For the source-generated `GraphQLFieldMap`, the generator needs to see `[GraphQLField]` attributes. Since these are in our own package, this is straightforward. For the FluentDynamoDB simple-case auto-mapping, the generator would need to see `[DynamoDbAttribute]` from a different package — this works by resolving attribute types by name (same approach the current generator uses for `[LambdaFunction]`), but needs validation.

3. **AppSync subscription trigger pattern**: Need to verify the exact mutation format AppSync expects for triggering subscriptions. Specifically: does the mutation need to be defined in the schema, or can we use a "pass-through" pattern? If it must be in the schema, the source generator should help generate the mutation type.

4. **Pipeline resolver support**: The current architecture supports pipeline resolvers in the manifest, but we haven't tested them end-to-end. The Runtime package needs to handle `ctx.stash` and `ctx.prev` for pipeline scenarios. This could be a follow-up spec.

5. **Nested resolvers / DataLoader pattern**: AppSync supports field-level resolvers (e.g., `Product.reviews` resolved by a different Lambda). The Runtime package should support `ctx.source` for this pattern. This is covered by Spec 1's context model but may need its own examples/documentation spec.

6. **Error handling conventions**: AppSync has specific error response formats. The Runtime package should provide helpers for returning errors in the expected format. This could be part of Spec 1 or a small follow-up.

7. **FieldSelection propagation ergonomics**: How does `FieldSelection` flow through DI / service method signatures without being annoying? Options: explicit parameter on service methods, ambient via `IFieldSelectionAccessor`, or attached to a request context object. Need to decide during Spec 2 design.

---

## What's NOT in Scope (Future Considerations)

- **CDK Construct Library**: A proper CDK construct that reads resolvers.json and creates all resources. Currently the CDK example is hand-written TypeScript. A published construct would be valuable but is a separate project.
- **AppSync Events / WebSocket support**: AppSync has a newer Events API alongside the traditional GraphQL subscriptions. Worth watching but not ready to invest in yet.
- **Code-first schema (no attributes)**: Some developers prefer convention-based schema generation without attributes. This is a fundamentally different approach and would be a major undertaking.
- **Query complexity analysis**: Limiting query depth/complexity at the resolver level. AppSync has some built-in support for this.
- **Caching integration**: AppSync has resolver-level caching. We could generate caching configuration in resolvers.json, but this is a nice-to-have.
- **Other database integrations**: SQL/PostgreSQL projection support. The abstraction in Spec 2 makes this possible, but we won't build it until there's demand.
