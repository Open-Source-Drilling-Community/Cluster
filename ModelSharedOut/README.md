# ModelSharedOut (Shared Model Generator)

ModelSharedOut is a .NET 8 console tool that builds a “distributed shared model” for clients of the Cluster microservice.
It merges OpenAPI schemas of the service and its dependencies, then:
- Generates a single merged OpenAPI document served by the Service at `Service/wwwroot/json-schema/ClusterMergedModel.json`.
- Generates C# DTOs and client types at `ModelSharedOut/ClusterMergedModel.cs` under the namespace `NORCE.Drilling.Cluster.ModelShared` for use by `WebApp` and `ServiceTest`.

## Purpose In The Solution
- Centralizes external model dependencies required by clients (WebApp, tests) into one versioned artifact.
- Ensures client code stays in sync with the Service API by regenerating from OpenAPI.
- Produces the Swagger JSON that the Service publishes at `/Cluster/api/swagger/merged/swagger.json`.

## Project Layout
- `json-schemas/`: Source OpenAPI JSON files to merge (e.g., `ClusterFullName.json`, `FieldMergedModel.json`).
- `Program.cs`: Merge pipeline and C# client/DTO generation via NSwag.
- `OpenApiSchemaReferenceUpdater.cs`: Utilities to normalize schema IDs and update `$ref`s during merge.
- Outputs:
  - `Service/wwwroot/json-schema/ClusterMergedModel.json`
  - `ModelSharedOut/ClusterMergedModel.cs`

## Prerequisites
- .NET SDK 8.0+
- No global tools required; code generation uses package references (`NSwag.CodeGeneration.CSharp`).

## Typical Workflow
1. Produce the service schema into `json-schemas/`:
   - Debug build of `Service` runs the MSBuild target that exports the service OpenAPI:
     - `dotnet build Service -c Debug`
     - Output: `ModelSharedOut/json-schemas/ClusterFullName.json`
2. Add dependency schemas, if any, to `json-schemas/`:
   - Example: `FieldMergedModel.json` (copied from another service’s merged Swagger).
3. Run the generator:
   - `dotnet run --project ModelSharedOut`
4. Verify outputs:
   - `Service/wwwroot/json-schema/ClusterMergedModel.json` (served by the Service)
   - `ModelSharedOut/ClusterMergedModel.cs` (referenced by WebApp and tests)

## Usage Examples
- Regenerate after API changes:
  - `dotnet build Service -c Debug`
  - `dotnet run --project ModelSharedOut`
- Consume in WebApp:
  - WebApp already references this project (`WebApp/WebApp.csproj`). Use DTOs in the `NORCE.Drilling.Cluster.ModelShared` namespace.
- Consume in tests:
  - `ServiceTest/ServiceTest.csproj` references this project; import the same namespace for strongly-typed fixtures.

## Generated Swagger In Service
- The merged OpenAPI JSON is placed at `Service/wwwroot/json-schema/ClusterMergedModel.json`.
- `Service/Program.cs` wires Swagger UI to expose it at `/Cluster/api/swagger/merged/swagger.json`.
- A temporary normalization step forces the OpenAPI version string to `3.0.3` for UI compatibility.

## Dependencies
- NuGet (this project):
  - `Microsoft.OpenApi.Readers` — reads OpenAPI documents.
  - `NSwag.CodeGeneration.CSharp` — generates C# clients and DTOs.
- Upstream project dependency:
  - `Service` (Debug build) exports `ClusterFullName.json` via MSBuild target `CreateSwaggerJson`.
- Downstream consumers:
  - `WebApp` and `ServiceTest` reference `ModelSharedOut` to access generated types.

## Integration Notes
- The “distributed shared model” approach keeps client model code local to this solution while using OpenAPI as the source of truth.
- When a dependency changes, drop the new schema JSON into `json-schemas/` and re-run the generator.
- Namespace consistency: the generator uses `NORCE.Drilling.Cluster.ModelShared` to avoid name collisions across services.

## Troubleshooting
- No output JSON or C# file:
  - Ensure `json-schemas/` contains at least the service JSON (`ClusterFullName.json`).
- Swagger UI shows 3.0.4 but UI expects 3.0.3:
  - The generator already replaces `3.0.4` with `3.0.3`; confirm the output file is being copied to `Service/wwwroot/json-schema`.
- Build fails on missing schema files:
  - Re-run `dotnet build Service -c Debug` to refresh `ClusterFullName.json`.
