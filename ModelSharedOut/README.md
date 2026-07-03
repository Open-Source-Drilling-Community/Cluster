# ModelSharedOut (Shared Model Generator)

ModelSharedOut is a .NET 8 console tool that builds a distributed shared model for clients of the Cluster microservice. It merges OpenAPI schemas of the service and its dependencies, then:

- Generates a single merged OpenAPI document served by the Service at `Service/wwwroot/json-schema/ClusterMergedModel.json`.
- Generates C# DTOs and client types at `ModelSharedOut/ClusterMergedModel.cs` under the namespace `NORCE.Drilling.Cluster.ModelShared` for use by `WebApp` and `ServiceTest`.

## Purpose In The Solution

- Centralizes external model dependencies required by clients into one versioned artifact.
- Ensures client code stays in sync with the Service API by regenerating from OpenAPI.
- Produces the Swagger JSON that the Service publishes at `/Cluster/api/swagger/merged/swagger.json`.

## Project Layout

- `json-schemas/`: source OpenAPI JSON files to merge, including the Cluster service schema and dependency schemas from Field, Rig, Trajectory, VerticalDatum, and related services.
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
   - `VerticalDatumModel.json` provides DTOs used for MSL depth reference calculations.
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
  - Use DTOs in the `NORCE.Drilling.Cluster.ModelShared` namespace.
  - The cluster web pages use generated DTOs for cluster data, field light/full data, field delineation lines, trajectories, survey runs, rig data, and vertical datum reference calls.
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
- When a dependency changes, refresh the corresponding schema JSON in `json-schemas/` and re-run the generator.
- Namespace consistency: the generator uses `NORCE.Drilling.Cluster.ModelShared` to avoid name collisions across services.
- If field delineation lines or trajectory/survey DTOs change upstream, regenerate this project before updating Cluster display code.

## Troubleshooting

- No output JSON or C# file:
  - Ensure `json-schemas/` contains at least the service JSON (`ClusterFullName.json`).
- Swagger UI shows `3.0.4` but UI expects `3.0.3`:
  - The generator already replaces `3.0.4` with `3.0.3`; confirm the output file is being copied to `Service/wwwroot/json-schema`.
- Build fails on missing schema files:
  - Re-run `dotnet build Service -c Debug` to refresh `ClusterFullName.json`.
