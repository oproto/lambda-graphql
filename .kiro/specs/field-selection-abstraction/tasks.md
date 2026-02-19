# Implementation Plan: Field Selection Abstraction

## Overview

Implement the Layer 2 field selection abstraction for `Oproto.Lambda.GraphQL.Runtime` and source generator enhancements. The implementation proceeds bottom-up: core types first (`FieldNameMap`, `FieldSelection`), then `ResponseShaper`, then the `AppSyncResolverContext` extension, and finally the source generator `GraphQLFieldMap` emission. Tests are interleaved with implementation tasks.

## Tasks

- [x] 1. Implement FieldNameMap
  - [x] 1.1 Create `FieldNameMap` and `FieldNameMapBuilder` classes in `Oproto.Lambda.GraphQL.Runtime/FieldNameMap.cs`
    - Implement `FieldNameMap` with private `IReadOnlyDictionary<string, string>` backing store
    - Implement `Identity` static property returning a singleton with empty mappings
    - Implement `Builder()` returning a `FieldNameMapBuilder`
    - Implement `MapName(string sourceName)` returning mapped name or original
    - Implement `Then(FieldNameMap next)` composing two maps left-to-right
    - Implement `FieldNameMapBuilder` with `Map(string, string)` and `Build()`
    - Add `ArgumentNullException` guards per design error handling table
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.6_

  - [x] 1.2 Write unit tests for FieldNameMap in `Oproto.Lambda.GraphQL.Tests/Runtime/FieldNameMapTests.cs`
    - Test `Builder()` fluent API creates correct mappings
    - Test `Identity.MapName(x)` returns `x`
    - Test `MapName` returns original for unmapped names
    - Test `Then()` composition: `"displayName"→"Name"` then `"Name"→"name"` yields `"displayName"→"name"`
    - Test `MapName(null)` throws `ArgumentNullException`
    - Test `Then(null)` throws `ArgumentNullException`
    - Test `Builder().Map(null, ...)` and `Builder().Map(..., null)` throw
    - Test duplicate source in builder: last mapping wins
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.6_

  - [x] 1.3 Write property tests for FieldNameMap in `Oproto.Lambda.GraphQL.Tests/Runtime/FieldNameMapPropertyTests.cs`
    - Create FsCheck `Arbitrary` for `FieldNameMap` in `Oproto.Lambda.GraphQL.Tests/Runtime/Generators/FieldSelectionArbitraries.cs`
    - **Property 5: FieldNameMap.MapName pass-through** — for any map and any name NOT in explicit entries, `MapName` returns the name unchanged; for names IN the map, returns the mapped target
    - **Validates: Requirements 3.3**
    - **Property 6: FieldNameMap.Identity maps every name to itself** — for any string, `Identity.MapName(name)` returns `name`
    - **Validates: Requirements 3.6**
    - **Property 7: FieldNameMap composition equals sequential mapping** — for any two maps and any name, `map1.Then(map2).MapName(s)` equals `map2.MapName(map1.MapName(s))`
    - **Validates: Requirements 3.4, 10.10**

- [x] 2. Implement FieldSelection
  - [x] 2.1 Create `FieldSelection` class in `Oproto.Lambda.GraphQL.Runtime/FieldSelection.cs`
    - Implement private constructor with `HashSet<string> fields`, `bool isAll`, `Dictionary<string, List<string>> nestedPaths`
    - Implement `Fields` property (`IReadOnlySet<string>`)
    - Implement `IsAll` property
    - Implement `All()` factory returning `IsAll=true`, empty fields
    - Implement `Of(params string[] fields)` factory
    - Implement `FromSelectionSet(List<string>? selectionSetList)` parsing top-level segments and nested paths per design algorithm
    - Implement `FromSelectionSet(List<string>? selectionSetList, FieldNameMap map)` overload mapping top-level segments
    - Implement `IsRequested(string propertyName)` returning true if `IsAll` or field in set
    - Implement `ForNestedType(string propertyName)` extracting sub-selection per design algorithm
    - Implement `MapWith(FieldNameMap map)` translating field names
    - Add `ArgumentNullException` guards per design error handling table
    - Handle null/empty list → `All()` per Requirement 1.7
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 2.1, 2.2, 2.3, 2.4, 2.5, 3.7, 4.1, 4.2, 4.3, 8.1, 8.2, 8.3, 8.4_

  - [x] 2.2 Write unit tests for FieldSelection in `Oproto.Lambda.GraphQL.Tests/Runtime/FieldSelectionTests.cs`
    - Test `All()` returns `IsAll=true`, empty `Fields`
    - Test `Of("Id", "Name")` returns correct `Fields` set, `IsAll=false`
    - Test `FromSelectionSet` with `["id", "category/name", "category/description"]` includes `"id"` and `"category"` as top-level
    - Test `ForNestedType("category")` on above returns `["name", "description"]`
    - Test multi-level nesting: `["id", "category/subcategory/name"]` → `ForNestedType("category").ForNestedType("subcategory")` contains `["name"]`
    - Test `ForNestedType` on field without sub-paths returns `All()`
    - Test `FromSelectionSet(null)` and `FromSelectionSet(empty)` return `All()`
    - Test `IsRequested` on `All()` returns true for any name
    - Test `IsRequested` returns false for absent fields
    - Test `FromSelectionSet` with `FieldNameMap` maps top-level names correctly
    - Test `MapWith(FieldNameMap.Identity)` preserves fields
    - Test `IsRequested(null)` throws `ArgumentNullException`
    - Test `ForNestedType(null)` throws `ArgumentNullException`
    - _Requirements: 1.1–1.7, 2.1–2.5, 4.1–4.3, 8.1–8.4, 10.1–10.4_

  - [x] 2.3 Write property tests for FieldSelection in `Oproto.Lambda.GraphQL.Tests/Runtime/FieldSelectionPropertyTests.cs`
    - Add FsCheck generators for `FieldSelection` and selection set lists to `FieldSelectionArbitraries.cs`
    - **Property 1: FromSelectionSet parses top-level field names correctly with optional mapping** — for any list of paths and any map, `Fields` contains exactly the unique mapped top-level segments
    - **Validates: Requirements 1.3, 4.3**
    - **Property 2: Of() creates a FieldSelection containing exactly the given fields** — for any array of distinct strings, `Of(fields).Fields` contains exactly those strings with correct count
    - **Validates: Requirements 1.4**
    - **Property 3: IsRequested correctness** — for any selection and name, `IsRequested(name)` is true iff `IsAll` or name in `Fields`
    - **Validates: Requirements 2.1, 2.5**
    - **Property 4: ForNestedType multi-level extraction** — for paths of depth N, chaining `ForNestedType` N-1 times yields the leaf field
    - **Validates: Requirements 2.2, 8.3**
    - **Property 8: MapWith(Identity) is identity** — for any non-All selection, `MapWith(Identity).Fields` equals original `Fields`
    - **Validates: Requirements 3.7, 10.6**

- [x] 3. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Implement ResponseShaper
  - [x] 4.1 Create `ResponseShaper` static class in `Oproto.Lambda.GraphQL.Runtime/ResponseShaper.cs`
    - Implement `ShapeResponse<T>(T value, FieldSelection selection)` with default `JsonSerializerOptions` using `CamelCaseNamingPolicy`
    - Implement `ShapeResponse<T>(T value, FieldSelection selection, JsonSerializerOptions options)` overload
    - If `selection.IsAll`, serialize and return without filtering
    - If value is null, return `"null"`
    - Serialize to `JsonDocument`, walk DOM filtering properties against FieldSelection
    - Build reverse lookup from serialized JSON property name → C# property name using `JsonSerializerOptions.GetTypeInfo()` on net8.0+, falling back to `PropertyNamingPolicy` on net6.0
    - Handle nested objects recursively using `ForNestedType`
    - Add `ArgumentNullException` guards per design error handling table
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 9.1, 9.2, 9.3_

  - [x] 4.2 Write unit tests for ResponseShaper in `Oproto.Lambda.GraphQL.Tests/Runtime/ResponseShaperTests.cs`
    - Define test record types with known properties (e.g., `TestProduct` with `Id`, `Name`, `Price`, nested `Category`)
    - Test `ShapeResponse` with `FieldSelection.All()` returns full JSON
    - Test `ShapeResponse` with `FieldSelection.Of("Id", "Name")` filters out `Price`
    - Test `ShapeResponse` with nested field selection filters nested object properties
    - Test `ShapeResponse` with null value returns `"null"`
    - Test `ShapeResponse` with `CamelCase` naming policy matches C# names correctly
    - Test `ShapeResponse` with `[JsonPropertyName("display_name")]` override
    - Test `ShapeResponse(value, null)` throws `ArgumentNullException`
    - _Requirements: 7.1–7.6, 9.1–9.3, 10.7, 10.8_

  - [x] 4.3 Write property tests for ResponseShaper in `Oproto.Lambda.GraphQL.Tests/Runtime/ResponseShaperPropertyTests.cs`
    - Add FsCheck generator for test objects to `FieldSelectionArbitraries.cs`
    - **Property 9: ShapeResponse with All() returns full serialized JSON** — for any non-null test object, `ShapeResponse(value, All(), options)` equals `JsonSerializer.Serialize(value, options)`
    - **Validates: Requirements 7.3**
    - **Property 10: ShapeResponse filters to only selected fields** — for any test object and any subset of property names, shaped JSON contains only properties corresponding to the selection
    - **Validates: Requirements 7.5, 9.1**

- [x] 5. Implement AppSyncResolverContext extension methods
  - [x] 5.1 Create `AppSyncResolverContextExtensions` class in `Oproto.Lambda.GraphQL.Runtime/AppSyncResolverContextExtensions.cs`
    - Implement `GetFieldSelection<TArguments>(this AppSyncResolverContext<TArguments> context)` parsing from `Info.SelectionSetList`
    - Implement `GetFieldSelection<TArguments>(this AppSyncResolverContext<TArguments> context, FieldNameMap map)` overload
    - Return `FieldSelection.All()` when `Info` is null, `SelectionSetList` is null or empty
    - _Requirements: 6.1, 6.2, 6.3_

  - [x] 5.2 Write unit tests for AppSyncResolverContext extensions in `Oproto.Lambda.GraphQL.Tests/Runtime/AppSyncResolverContextExtensionTests.cs`
    - Test `GetFieldSelection()` with valid `SelectionSetList` returns correct FieldSelection
    - Test `GetFieldSelection()` with null `Info` returns `All()`
    - Test `GetFieldSelection()` with empty `SelectionSetList` returns `All()`
    - Test `GetFieldSelection(map)` maps field names correctly
    - _Requirements: 6.1, 6.2, 6.3_

- [x] 6. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Enhance source generator to emit GraphQLFieldMap
  - [x] 7.1 Extend `FieldInfo` model in `Oproto.Lambda.GraphQL.SourceGenerator/Models/TypeInfo.cs`
    - Add `CSharpPropertyName` property to `FieldInfo`
    - _Requirements: 5.1_

  - [x] 7.2 Update `GraphQLSchemaGenerator` to capture C# property names
    - In the existing type extraction logic, populate `FieldInfo.CSharpPropertyName` with the original C# property name (before `[GraphQLField]` renaming)
    - _Requirements: 5.1, 5.2_

  - [x] 7.3 Add `GraphQLFieldMap` partial class emission to the source generator
    - After schema generation, iterate over extracted `TypeInfo` entries with `Kind == TypeKind.Object`
    - For each type, check if the class is declared as `partial`; if not, emit a diagnostic warning and skip
    - Emit a partial class file with a `public static FieldNameMap GraphQLFieldMap` property
    - If any field has `CSharpPropertyName != Name` (GraphQL name), emit `FieldNameMap.Builder().Map(graphqlName, csharpName)...Build()`
    - If all names match, emit `FieldNameMap.Identity`
    - Use fully qualified `Oproto.Lambda.GraphQL.Runtime.FieldNameMap` in generated code (source generator cannot reference Runtime)
    - Skip enums, unions, interfaces, and input types
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

  - [x] 7.4 Write source generator tests in `Oproto.Lambda.GraphQL.Tests/Runtime/SourceGenerator/GraphQLFieldMapGeneratorTests.cs`
    - Use `CSharpGeneratorDriver` to verify generated output
    - Test: `[GraphQLType]` class with `[GraphQLField("displayName")]` on `Name` emits `Builder().Map("displayName", "Name").Build()`
    - Test: `[GraphQLType]` class with all matching names emits `FieldNameMap.Identity`
    - Test: Non-partial `[GraphQLType]` class emits diagnostic warning, no `GraphQLFieldMap`
    - Test: `[GraphQLType]` enum/input types do not get `GraphQLFieldMap`
    - Test: Generated code uses fully qualified type names (AOT-compatible, no reflection)
    - _Requirements: 5.1–5.6, 10.9_

- [x] 8. Integration wiring and Examples project update
  - [x] 8.1 Update `Oproto.Lambda.GraphQL.Examples` to demonstrate field selection usage
    - Ensure `Product` class in Examples is `partial` so it receives the generated `GraphQLFieldMap`
    - Add a usage example in a resolver function showing `ctx.GetFieldSelection(Product.GraphQLFieldMap)` and `ResponseShaper.ShapeResponse`
    - Verify the Examples project builds successfully with the new source-generated code
    - _Requirements: 5.5, 6.1, 6.2, 7.1_

- [x] 9. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The Runtime package targets net8.0;net10.0 (ignore net6.0 references in the design doc per the development plan)
- The source generator must remain on netstandard2.0
- All runtime code must be AOT-compatible (no reflection)
