# ModelSharedOut (Shared Model Generator)

ModelSharedOut is a .NET 8 console tool that builds a distributed shared model for clients of the Cluster microservice. It merges OpenAPI schemas of the service and its dependencies, then:

- Generates a single merged OpenAPI document served by the Service at `Service/wwwroot/json-schema/ClusterMergedModel.json`.
- Generates C# DTOs and client types at `ModelSharedOut/ClusterMergedModel.cs` under the namespace `OSDC.Drilling.Cluster.ModelShared` for use by `WebApp` and `ServiceTest`.

## Purpose In The Solution

- Centralizes external model dependencies required by clients into one versioned artifact.
- Ensures client code stays in sync with the Service API by regenerating from OpenAPI.
- Produces the Swagger JSON that the Service publishes at `/Cluster/api/swagger/merged/swagger.json`.

## Project Layout

- `json-schemas/`: source OpenAPI JSON files to merge, including the Cluster service schema and the Field, Rig, Trajectory, and EarthVerticalDatum contracts used directly by Cluster clients.
- `Program.cs`: merge pipeline and C# client/DTO generation via NSwag.
- `OpenApiSchemaReferenceUpdater.cs`: utilities to normalize schema IDs and update `$ref`s during merge.
- Outputs:
  - `Service/wwwroot/json-schema/ClusterMergedModel.json`
  - `ModelSharedOut/ClusterMergedModel.cs`

## Prerequisites

- .NET SDK 8.0+
- No global tools required; code generation uses package references such as `NSwag.CodeGeneration.CSharp`.

## Typical Workflow

1. Produce the service schema into `json-schemas/`:
   - Debug build of `Service` runs the MSBuild target that exports the service OpenAPI:
   - `dotnet build Service -c Debug`
   - Output: `ModelSharedOut/json-schemas/ClusterFullName.json`
2. Add or refresh dependency schemas in `json-schemas/` when upstream services change:
   - `FieldModel.json` provides field references and field delineation line DTOs used by cluster display pages.
   - `TrajectoryModel.json` provides survey-run, trajectory, survey-station, and uncertainty-ellipse DTOs.
   - `RigModel.json` provides rig DTOs used by cluster editing.
   - `EarthVerticalDatumModel.json` provides the synchronous DTOs used for MSL depth reference calculations.
   - Refresh `TrajectoryModel.json` from Trajectory's service-owned
     `ModelSharedOut/json-schemas/TrajectoryFullName.json`, not its transitive merged aggregate.
3. Run the generator:
   - `dotnet run --project ModelSharedOut`
4. Verify outputs:
   - `Service/wwwroot/json-schema/ClusterMergedModel.json`
   - `ModelSharedOut/ClusterMergedModel.cs`

## Usage Examples

- Regenerate after API changes:
  - `dotnet build Service -c Debug`
  - `dotnet run --project ModelSharedOut`
- Consume in WebApp:
  - WebApp already references this project (`WebApp/WebApp.csproj`).
  - Use DTOs in the `OSDC.Drilling.Cluster.ModelShared` namespace.
  - The cluster web pages use generated DTOs for cluster data, field light/full data, field delineation lines, trajectories, survey runs, rig data, and synchronous EarthVerticalDatum calls.
- Consume in tests:
  - `ServiceTest/ServiceTest.csproj` references this project for strongly typed fixtures and generated clients.

## Generated Swagger In Service

- The merged OpenAPI JSON is placed at `Service/wwwroot/json-schema/ClusterMergedModel.json`.
- `Service/Program.cs` wires Swagger UI to expose it at `/Cluster/api/swagger/merged/swagger.json`.
- A temporary normalization step forces the OpenAPI version string to `3.0.3` for UI compatibility.

## Dependencies

- NuGet packages:
  - `Microsoft.OpenApi.Readers`
  - `NSwag.CodeGeneration.CSharp`
- Upstream project dependency:
  - `Service` exports `ClusterFullName.json` via the Debug build target.
- Downstream consumers:
  - `WebApp`
  - `ServiceTest`

## Integration Notes

- OpenAPI is the source of truth for generated DTOs and clients.

`ClientJsonSerializerSettings.cs` is a maintained partial-client extension, not generated output. It registers `JsonStringEnumConverter` because NSwag does not attach an item converter to arrays of string enums such as `StationKeepingSystem.Modes`. Keep it linked into WebPages when regenerating `ClusterMergedModel.cs`.
- When a dependency changes, refresh the corresponding schema JSON in `json-schemas/` and re-run the generator.
- Namespace consistency: the generator uses `OSDC.Drilling.Cluster.ModelShared` to avoid name collisions across services.
- If field delineation lines or trajectory/survey DTOs change upstream, regenerate this project before updating Cluster display code.

## Troubleshooting

- No output JSON or C# file:
  - Ensure `json-schemas/` contains at least the service JSON (`ClusterFullName.json`).
- Swagger UI shows `3.0.4` but UI expects `3.0.3`:
  - The generator already replaces `3.0.4` with `3.0.3`; confirm the output file is being copied to `Service/wwwroot/json-schema`.
- Build fails on missing schema files:
  - Re-run `dotnet build Service -c Debug` to refresh `ClusterFullName.json`.

## Preserving semantic metadata

Refresh `ClusterFullName.json` from the built Service OpenAPI output before running this generator. Refresh the sibling FieldModel.json input from that service's authoritative schema when its contract changes. The generator merges dependency snapshots first and `ClusterFullName.json` last so older copies of shared types cannot overwrite this service's annotations.

Run the existing `ModelSharedOut` program and confirm the overwrite prompt. Commit both generated `ClusterMergedModel.cs` and `Service/wwwroot/json-schema/ClusterMergedModel.json` with their schema inputs. `x-osdc-semantic` and contextual `x-osdc-semantic-bindings` remain in the merged JSON contract; generated C# carries the accompanying property documentation. Do not hand-edit generated model code. Build the full solution and run contract tests after regeneration.

## Bounded light discovery

Resource and catalog discovery returns light pages, with `offset` defaulting to 0 and `limit` defaulting to 50 (allowed range 1–200). Names and relevant resource-reference filters are evaluated on the server. Unbounded full-object `HeavyData` routes and tools, including filtered full-object lists, have been removed. Existing UUID and HTTP-metadata listings remain available; use named light pages for routine discovery.

REST routes and MCP tool names (relative to the service's existing API path base):

- `/Cluster/LightData` / `cluster_discover`
- `/ClusterFeatureCategory/LightData` / `cluster_feature_category_discover`
- `/ClusterIdentity/LightData` / `cluster_identity_discover`
- `/SlotFeatureCategory/LightData` / `slot_feature_category_discover`
- `/Cluster/{clusterId}/Slots/LightData` / `cluster_slots_discover`

Pages contain `Items`, `Offset`, `Limit`, `TotalCount`, and `NextOffset`; `NextOffset` is null on the final page. Offset pages are individually consistent reads, not a snapshot spanning requests: concurrent inserts/deletes can change later pages. Refresh discovery when the underlying collection changes. EarthGeodesy's existing search pages instead expose `Items[].Value`, `Offset`, `Limit`, and `TotalCount`.

Single-record reads, result/chunk reads, and deliberate backup/export/restore operations are preserved. Backups can still contain complete resources and should be treated as explicit bulk operations.

This is a breaking REST/MCP contract change: update generated clients, UIs, and external integrations together with the service release. Removed heavy-list names are not compatibility aliases. No persisted engineering shapes, database names, or storage volumes are changed. Historical usage counters are retained; bounded discovery has a separate additive `DiscoveryRequestsPerDay` history in resource services.


Regenerate from the owning service's freshly compiled OpenAPI, refresh dependent schema snapshots, then regenerate consuming clients/DTOs. Do not edit generated C# to restore removed methods. Keep the hand-authored discovery client extensions alongside generated clients: they iterate bounded light pages when a complete name/reference list is needed. Reference projections are partial display/context objects; retrieve a selected complete record before full-replacement writes or engineering calculations. Schema-name normalization must also rewrite discriminator mappings.
