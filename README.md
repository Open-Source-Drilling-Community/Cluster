# Cluster Solution

A complete, domain-focused solution for managing Cluster data, composed of an ASP.NET Core microservice (Service), a Blazor Server frontend (WebApp), shared model/code-generation utilities (ModelSharedOut), and domain models (Model). The solution exposes a REST API with an OpenAPI definition and provides a UI for CRUD operations on Cluster entities, cluster identities, cluster feature categories, and slot feature categories.

The repository uses the root namespace `OSDC.Drilling.Cluster`. Its reusable Razor package is `OSDC.Drilling.Cluster.WebPages`; Docker and Helm artifacts use the `osdcdrillingcluster...` identity. Existing `/Cluster` routes, database files, and domain UUIDs are unchanged. Follow [deployment/identity-cutover.md](deployment/identity-cutover.md) before replacing an existing `NORCE.Drilling.Cluster` deployment.

## Projects
- Service: ASP.NET Core Web API at base path `/Cluster/api`; persists data in SQLite, serves Swagger UI, exposes MCP tools, and can register its MCP endpoint on an MCP hub. See `Service/README.md`.
- WebApp: Blazor Server UI at base path `/Cluster/webapp`; consumes the Service via generated clients. See `WebApp/README.md`.
- Model: Domain models and statistics used by Service and clients. See `Model/README.md`.
- ModelSharedOut: Generates merged OpenAPI and C# client/DTOs for consumers; outputs JSON to Service and code to itself. See `ModelSharedOut/README.md`.
- ServiceTest: Tests that target the running Service using the generated client.
- ModelTest: Unit tests for domain logic in Model.
- home: Local storage folder used by the Service for SQLite (`home/Cluster.db`), optional external service configuration, and the generated MCP hub instance id.

## Prerequisites
- .NET SDK 8.0+
- Optional: Docker (to run Service/WebApp in containers)

## Installation
1. Restore and build all:
   - `dotnet restore`
   - `dotnet build -c Debug`
2. Generate merged schema and clients (if schemas changed):
   - Build Service (Debug) to emit `ModelSharedOut/json-schemas/ClusterFullName.json` via MSBuild target.
   - Run the generator: `dotnet run --project ModelSharedOut`
3. Run the Service:
   - `dotnet run --project Service`
   - Base API: `https://localhost:5001/Cluster/api`
   - Swagger UI: `https://localhost:5001/Cluster/api/swagger`
4. Run the WebApp:
   - `dotnet run --project WebApp`
   - UI: `https://localhost:5011/Cluster/webapp/Cluster`

## Usage Examples
- List Cluster IDs:
  - `curl -k https://localhost:5001/Cluster/api/Cluster`
- Get Cluster by ID:
  - `curl -k https://localhost:5001/Cluster/api/Cluster/<guid>`
- Create a Cluster (requires `MetaInfo.ID`):
  - `curl -k -X POST https://localhost:5001/Cluster/api/Cluster -H "Content-Type: application/json" -d "{ \"MetaInfo\": { \"ID\": \"11111111-1111-1111-1111-111111111111\" }, \"Name\": \"Cluster A\" }"`
- Open the UI:
  - Navigate to `https://localhost:5011/Cluster/webapp/Home`

## MCP Server

The Service exposes a Model Context Protocol endpoint alongside the REST API:

- Streamable HTTP: `/Cluster/api/mcp`
- WebSocket: `/Cluster/api/mcp/ws`

MCP tools expose cluster and locally owned catalog CRUD plus atomic batch backup/restore. Usage statistics remain available through REST and are not exposed as an MCP tool. The main tool groups are:

- `cluster_...`
- `cluster_identity_...`
- `cluster_feature_category_...`
- `slot_feature_category_...`

The Docker image reads optional service configuration from `/home/Cluster.Service.json`, or from the path specified by `CLUSTER_EXTERNAL_CONFIG`. This file can enable MCP hub registration through the `McpHub` section.

## Main Capabilities
- Cluster management: create, edit, import, export, and delete clusters.
- Portable batch transfer: export or atomically restore several clusters together with referenced local identity/feature definitions and verified Field/Rig UUID-name manifests.
- Field association: clusters can be linked to a field and displayed using field, cluster, cartographic, geodetic, and depth reference systems.
- Cluster identities: user-defined identity definitions can be managed separately and assigned to clusters with cluster-specific values.
- Cluster features: user-defined feature categories/options can be managed separately and assigned to clusters with exclusivity and optional validity periods.
- Slot features: user-defined slot feature categories/options can be managed separately and assigned to selected slots in the cluster editor.
- Safe mutations: full replacements use optimistic concurrency, locally owned catalog references are validated atomically, referenced definitions/options cannot be removed, and Slot dictionary keys must match their `Slot.ID` values.
- Slot editing: slots can be edited with north/east and latitude/longitude coordinates through the unit/reference system; the slot table summarizes assigned features.
- Survey/trajectory displays: cluster survey runs and trajectories are shown in 3D and in horizontal projection, with optional uncertainty ellipses.
- Field delineation overlays: when a cluster belongs to a field with delineation lines, those lines and their calculated boundaries are displayed together with cluster survey runs and trajectories. In 3D, delineation lines are placed on the north/east plane at the top or bottom of the displayed survey/trajectory bounding box. The bounding box itself is based on survey/trajectory data, not on delineation extents.
- Calculators: the web app exposes synchronous cartographic conversion, vertical datum conversion, Earth gravity evaluation, and Earth magnetic-field evaluation pages under the `Calculators` menu. Calculation requests and results are not persisted by the Earth services.

## Docker (Optional)
- Build images from repo root:
  - Service: `docker build -t osdcdrillingclusterservice -f Service/Dockerfile .`
  - WebApp: `docker build -t osdcdrillingclusterwebappclient -f WebApp/Dockerfile .`
- Run locally:
  - Service: `docker run --rm -p 5001:8080 -v %CD%/home:/home osdcdrillingclusterservice` (PowerShell)
  - WebApp: `docker run --rm -p 5011:8080 -e ASPNETCORE_URLS=http://+:8080 -e ClusterHostURL=https://host.docker.internal:5001/ osdcdrillingclusterwebappclient`

## Dependencies
- Runtime packages (high level):
  - Service: `Microsoft.Data.Sqlite`, `Swashbuckle.AspNetCore.*`, `Microsoft.OpenApi*`.
  - Model: `OSDC.DotnetLibraries.*` (Common, DataManagement, Statistics, DrillingProperties).
  - WebApp: `MudBlazor`, `OSDC.DotnetLibraries.General.DataManagement`, the current `OSDC.Drilling.Field.WebPages` and `OSDC.Drilling.Rig.WebPages` packages, and reusable EarthCartographicProjection, EarthGeodesy, EarthVerticalDatum, EarthGravity, and EarthMagneticField web page packages.
  - ModelSharedOut: `Microsoft.OpenApi.Readers`, `NSwag.CodeGeneration.CSharp`.
- Project references:
  - Service → Model
  - WebApp → ModelSharedOut
  - ServiceTest → Service, ModelSharedOut
  - ModelTest → Model
- External services (optional but supported):
  - Field, Trajectory, Rig, EarthCartographicProjection, EarthGeodesy, EarthVerticalDatum, EarthGravity, EarthMagneticField, and UnitConversion service URLs are configurable in `WebApp/appsettings.*.json`.

## Notes
- Path bases: Service uses `/Cluster/api`; WebApp uses `/Cluster/webapp`. Ensure reverse proxies/ingress match these paths.
- Persistence: SQLite file at `home/Cluster.db`. Startup validates schema and backs up on mismatch. Optional external service configuration and the generated MCP hub instance id can also live under `home/`.
- Swagger: Service exposes UI and a merged OpenAPI JSON at `/Cluster/api/swagger/merged/swagger.json`.

## Links
- Docker Hub organization: https://hub.docker.com/?namespace=digiwells
- Templates and docs: https://github.com/NORCE-DrillingAndWells/DrillingAndWells/wiki

## Demo Environment
- Example Service (dev): `https://dev.DigiWells.no/Cluster/api/swagger` (Swagger UI) and `https://dev.DigiWells.no/Cluster/api/Cluster` (API)
- Example WebApp (dev): `https://dev.DigiWells.no/Cluster/webapp/Home`

## Current WebApp dependencies

The Cluster WebApp consumes `OSDC.Drilling.Field.WebPages` 2.0.0, `OSDC.Drilling.Rig.WebPages` 1.1.0, `OSDC.Drilling.EarthCartographicProjection.WebPages` 1.1.0, `OSDC.Drilling.EarthGeodesy.WebPages` 1.1.0, `OSDC.Drilling.EarthVerticalDatum.WebPages` 1.1.0, `OSDC.Drilling.EarthGravity.WebPages` 1.0.1, and `OSDC.Drilling.EarthMagneticField.WebPages` 1.0.1. Field and Rig pages are hosted directly from their reusable Razor assemblies; Earth contextual and calculator pages use local route wrappers so all routes remain under `/Cluster/webapp`.
