# Requirements Document

## Introduction

This spec covers the Field Selection Abstraction layer for `Oproto.Lambda.GraphQL.Runtime`. Today, AppSync sends a `selectionSetList` (e.g., `["id", "name", "category/name"]`) in the resolver context, but there is no typed abstraction to work with it. Developers must manually parse the raw string list, handle GraphQL-to-C# name mapping, deal with nested field paths, and shape responses — all without any library support.

This spec introduces `FieldSelection` (a database-agnostic abstraction over requested fields), `FieldNameMap` (a composable mapping from GraphQL field names to C# property names), source generator enhancements to emit `FieldNameMap` for each `[GraphQLType]` class, a convenience extension on `AppSyncResolverContext` to extract field selections, and `ShapeResponse<T>()` for filtering serialized output to only the requested fields.

This is Layer 2 of the three-layer field selection design described in the development plan. It deliberately excludes database-specific projection (Layer 3), which is covered by separate specs for FluentDynamoDB and raw DynamoDB SDK integration.

## Glossary

- **Runtime_Package**: The `Oproto.Lambda.GraphQL.Runtime` NuGet package (net8.0;net10.0) providing typed models for AppSync resolver context
- **Source_Generator**: The existing Roslyn incremental source generator (`Oproto.Lambda.GraphQL.SourceGenerator`) that produces `schema.graphql`, `resolvers.json`, and source-generated code from C# attributes
- **FieldSelection**: A database-agnostic abstraction representing the set of fields requested by a GraphQL client, using C# property names
- **FieldNameMap**: An immutable mapping from one naming domain to another (e.g., GraphQL field names to C# property names), supporting composition via `Then()`
- **Selection_Set_List**: The `List<string>` from `AppSyncInfo.SelectionSetList` containing flattened field paths as provided by AppSync (e.g., `["id", "name", "category/name"]`)
- **GraphQL_Field_Name**: The name of a field as it appears in the GraphQL schema (e.g., `displayName`), which may differ from the C# property name
- **CSharp_Property_Name**: The name of a property on a C# class (e.g., `Name`), which the `[GraphQLField("displayName")]` attribute maps from
- **Nested_Field_Path**: A slash-separated path in the Selection_Set_List representing a field on a nested type (e.g., `"category/name"` means the `name` field on the `category` object)
- **Response_Shaping**: The process of filtering a fully-populated object's serialized JSON output to include only the fields present in the FieldSelection
- **GraphQLType_Class**: A C# class annotated with `[GraphQLType]` for which the Source_Generator produces GraphQL SDL
- **AOT**: Ahead-of-Time compilation; requires no runtime reflection

## Requirements

### Requirement 1: FieldSelection Core Abstraction

**User Story:** As a Lambda developer, I want a typed abstraction over the requested GraphQL fields, so that I can query which fields were requested without parsing raw string lists.

#### Acceptance Criteria

1. THE Runtime_Package SHALL provide a `FieldSelection` class with a `Fields` property of type `IReadOnlySet<string>` containing C# property names
2. THE `FieldSelection` class SHALL provide a static `All()` factory method that returns a FieldSelection representing "all fields requested" (no filtering)
3. THE `FieldSelection` class SHALL provide a static `FromSelectionSet(List<string> selectionSetList)` factory method that parses a Selection_Set_List into a FieldSelection using top-level field names only
4. THE `FieldSelection` class SHALL provide a static `Of(params string[] fields)` factory method that creates a FieldSelection from explicit field names
5. THE `FieldSelection` class SHALL provide an `IsAll` property that returns true when the FieldSelection represents "all fields" (created via `All()`)
6. WHEN `FieldSelection.All()` is used, THE `Fields` property SHALL return an empty set and `IsAll` SHALL return true
7. WHEN `FieldSelection.FromSelectionSet` receives a null or empty list, THE FieldSelection SHALL behave identically to `FieldSelection.All()`

### Requirement 2: FieldSelection Query Methods

**User Story:** As a service layer developer, I want to check whether specific fields were requested, so that I can conditionally skip expensive computations for unrequested fields.

#### Acceptance Criteria

1. THE `FieldSelection` class SHALL provide an `IsRequested(string propertyName)` method that returns true when the named field is in the selection or when `IsAll` is true
2. THE `FieldSelection` class SHALL provide a `ForNestedType(string propertyName)` method that extracts the sub-selection for a nested object field
3. WHEN `ForNestedType("category")` is called on a FieldSelection built from `["id", "category/name", "category/description"]`, THE returned FieldSelection SHALL contain `["name", "description"]`
4. WHEN `ForNestedType` is called with a field name that has no nested selections in the Selection_Set_List, THE returned FieldSelection SHALL behave as `FieldSelection.All()` (the nested type was requested but no specific sub-fields were specified)
5. WHEN `IsRequested` is called on a `FieldSelection.All()` instance, THE method SHALL return true for any field name

### Requirement 3: FieldNameMap for Name Translation

**User Story:** As a developer whose GraphQL field names differ from C# property names, I want a mapping layer that translates between naming domains, so that field selection works correctly across the GraphQL-to-C# boundary.

#### Acceptance Criteria

1. THE Runtime_Package SHALL provide a `FieldNameMap` class that maps source names to target names
2. THE `FieldNameMap` class SHALL provide a `Builder()` static method returning a fluent builder with a `Map(string sourceName, string targetName)` method and a `Build()` method
3. THE `FieldNameMap` class SHALL provide a `MapName(string sourceName)` method that returns the mapped target name, or the original source name when no mapping exists
4. THE `FieldNameMap` class SHALL provide a `Then(FieldNameMap next)` method that composes two maps into a single map (source → intermediate → target)
5. WHEN `Then()` composes two maps where the first maps "displayName" to "Name" and the second maps "Name" to "name", THE composed map SHALL map "displayName" to "name"
6. THE `FieldNameMap` class SHALL provide an `Identity` static property that returns a map where every name maps to itself
7. THE `FieldSelection` class SHALL provide a `MapWith(FieldNameMap map)` method that returns a new FieldSelection with all field names translated through the map

### Requirement 4: FieldSelection Parsing with FieldNameMap

**User Story:** As a developer, I want to parse the AppSync selection set directly into C# property names using a FieldNameMap, so that I get a FieldSelection I can use immediately in my service layer.

#### Acceptance Criteria

1. THE `FieldSelection` class SHALL provide a static `FromSelectionSet(List<string> selectionSetList, FieldNameMap map)` overload that parses and maps field names in a single step
2. WHEN the Selection_Set_List contains `["displayName", "price"]` and the FieldNameMap maps `"displayName"` to `"Name"`, THE resulting FieldSelection SHALL contain `["Name", "price"]` (unmapped names pass through unchanged)
3. WHEN the Selection_Set_List contains nested paths like `"category/displayName"`, THE `FromSelectionSet` method SHALL map only the top-level segment using the provided FieldNameMap and preserve nested path segments for later extraction via `ForNestedType`

### Requirement 5: Source Generator FieldNameMap Emission

**User Story:** As a developer, I want the source generator to automatically produce a `FieldNameMap` for each `[GraphQLType]` class, so that I do not need to manually maintain the GraphQL-to-C# name mapping.

#### Acceptance Criteria

1. THE Source_Generator SHALL emit a static `GraphQLFieldMap` property on a partial class matching each GraphQLType_Class that has at least one field where the GraphQL_Field_Name differs from the CSharp_Property_Name
2. THE emitted `GraphQLFieldMap` property SHALL return a `FieldNameMap` containing entries only for fields where the GraphQL_Field_Name differs from the CSharp_Property_Name
3. WHEN a GraphQLType_Class has a property `Name` with `[GraphQLField("displayName")]`, THE emitted FieldNameMap SHALL contain the mapping `"displayName"` → `"Name"`
4. WHEN all fields on a GraphQLType_Class have identical GraphQL and C# names, THE Source_Generator SHALL still emit a `GraphQLFieldMap` property returning `FieldNameMap.Identity`
5. THE Source_Generator SHALL emit the `GraphQLFieldMap` property in a source-generated partial class file so that the user's original class is not modified
6. THE emitted code SHALL be AOT-compatible with no reflection usage

### Requirement 6: AppSyncResolverContext Field Selection Extension

**User Story:** As a Lambda developer, I want a convenience method on `AppSyncResolverContext` to get a `FieldSelection`, so that I can extract field selection from the resolver context in one call.

#### Acceptance Criteria

1. THE Runtime_Package SHALL provide a `GetFieldSelection()` extension method on `AppSyncResolverContext<TArguments>` that returns a `FieldSelection` parsed from `Info.SelectionSetList`
2. THE Runtime_Package SHALL provide a `GetFieldSelection(FieldNameMap map)` overload that parses and maps in a single step
3. WHEN `Info` is null or `SelectionSetList` is null or empty, THE `GetFieldSelection()` method SHALL return `FieldSelection.All()`

### Requirement 7: Response Shaping

**User Story:** As a developer who must fetch all data (for computed fields or authorization) but wants to return only requested fields, I want to shape the serialized response to match the client's selection set, so that I reduce payload size without changing my data fetching logic.

#### Acceptance Criteria

1. THE Runtime_Package SHALL provide a `ShapeResponse<T>(T value, FieldSelection selection)` static method that serializes the value to JSON and removes properties not present in the FieldSelection
2. THE Runtime_Package SHALL provide a `ShapeResponse<T>(T value, FieldSelection selection, JsonSerializerOptions options)` overload that accepts custom serialization options
3. WHEN `ShapeResponse` is called with `FieldSelection.All()`, THE method SHALL return the full serialized JSON without filtering
4. WHEN `ShapeResponse` is called with a FieldSelection containing `["Id", "Name"]` on an object with `Id`, `Name`, and `Price` properties, THE returned JSON SHALL contain only `id` and `name` fields (using the serializer's naming policy)
5. THE `ShapeResponse` method SHALL support nested field selection, filtering nested object properties according to the sub-selection extracted via `ForNestedType`
6. IF the value is null, THEN THE `ShapeResponse` method SHALL return a JSON null literal

### Requirement 8: Nested Field Selection Support

**User Story:** As a developer with GraphQL types containing nested objects, I want field selection to correctly handle nested field paths, so that I can optimize data fetching for complex object graphs.

#### Acceptance Criteria

1. WHEN the Selection_Set_List contains `["id", "category/name", "category/description"]`, THE `FieldSelection.FromSelectionSet` method SHALL include `"id"` and `"category"` as top-level fields
2. WHEN `ForNestedType("category")` is called on the FieldSelection from criterion 1, THE returned FieldSelection SHALL contain `["name", "description"]`
3. WHEN the Selection_Set_List contains `["id", "category/subcategory/name"]`, THE `ForNestedType("category")` SHALL return a FieldSelection where `ForNestedType("subcategory")` returns a FieldSelection containing `["name"]`
4. WHEN the Selection_Set_List contains `["id", "category"]` (the nested type is listed without sub-fields), THE `ForNestedType("category")` SHALL return `FieldSelection.All()` since the entire nested object was requested

### Requirement 9: Response Shaping Naming Policy Alignment

**User Story:** As a developer using `camelCase` JSON serialization, I want response shaping to correctly match C# property names in the FieldSelection against the serialized JSON property names, so that filtering works regardless of the naming policy.

#### Acceptance Criteria

1. THE `ShapeResponse` method SHALL match FieldSelection field names (which are C# property names) against the serialized JSON property names by applying the same naming policy used during serialization
2. WHEN the `JsonSerializerOptions` uses `CamelCase` naming policy and the FieldSelection contains `"Name"`, THE `ShapeResponse` method SHALL retain the `"name"` property in the JSON output
3. WHEN a property has an explicit `[JsonPropertyName("display_name")]` attribute, THE `ShapeResponse` method SHALL match the FieldSelection entry for the C# property name (e.g., `"Name"`) against the serialized name `"display_name"`

### Requirement 10: Unit Tests for FieldSelection

**User Story:** As a developer, I want comprehensive tests for the field selection abstraction, so that I can trust it handles all edge cases correctly.

#### Acceptance Criteria

1. THE test suite SHALL include tests for `FieldSelection.All()`, `FieldSelection.Of()`, and `FieldSelection.FromSelectionSet()` factory methods
2. THE test suite SHALL include tests for `IsRequested` returning true for present fields and false for absent fields
3. THE test suite SHALL include tests for `ForNestedType` extracting correct sub-selections from nested paths
4. THE test suite SHALL include tests for multi-level nesting (e.g., `"a/b/c"`)
5. THE test suite SHALL include tests for `FieldNameMap` composition via `Then()`
6. THE test suite SHALL include a round-trip property test verifying that mapping a FieldSelection through `FieldNameMap.Identity` produces an equivalent FieldSelection
7. THE test suite SHALL include tests for `ShapeResponse` filtering top-level and nested properties
8. THE test suite SHALL include tests for `ShapeResponse` with `FieldSelection.All()` returning unfiltered output
9. THE test suite SHALL include tests for the source-generated `GraphQLFieldMap` property on a `[GraphQLType]` class with renamed fields
10. THE test suite SHALL include a property-based test verifying that for any FieldNameMap composed via `Then()`, mapping a name through the composed map produces the same result as mapping through each map sequentially
