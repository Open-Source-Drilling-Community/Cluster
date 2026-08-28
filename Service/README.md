# Service (Cluster Microservice)

The Service project is an ASP.NET Core Web API that exposes REST endpoints to create, read, update, and delete Cluster domain objects and related cluster configuration objects. It persists data in a local SQLite database and publishes an OpenAPI schema for client generation and UI exploration. This microservice is consumed by the WebApp project and automated tests in ServiceTest.

## Purpose In The Solution
- Provides the backend API for Cluster operations at the base path `/Cluster/api`.
- Provides CRUD APIs for cluster identity definitions, cluster feature categories, and slot feature categories.
- Exposes an MCP endpoint with tools mirroring the REST API, plus optional MCP hub registration.
- Persists Cluster data to SQLite in `home/Cluster.db` with automatic schema checks and backup on mismatch.
- Serves a merged OpenAPI document and Swagger UI for client tooling and manual testing.
- References `Model` for shared data types and statistics models.

## Prerequisites
- .NET SDK 8.0+
- Optional: Docker (to build/run the container image)
- Optional (for schema export in Debug): `Swashbuckle.AspNetCore.Cli` dotnet tool (`dotnet tool install -g Swashbuckle.AspNetCore.Cli`)

## Installation
1. Restore and build the solution:
   - `dotnet restore`
   - `dotnet build Service -c Debug`
2. Run locally:
   - `dotnet run --project Service`
   - Default URLs (from `Service/Properties/launchSettings.json`): `https://localhost:5001`, `http://localhost:5002`
   - Base API path: `https://localhost:5001/Cluster/api`
3. Configure (optional):
   - `Service/appsettings.*.json` supports `WellHostURL` to point to an external Well service.
   - Logging and detailed errors configured per environment.
   - Optional external service configuration is loaded from `home/Cluster.Service.json`, or from the path specified by `CLUSTER_EXTERNAL_CONFIG`.
   - In Docker, the image reads optional external configuration from `/home/Cluster.Service.json`.

External MCP hub configuration example:

```json
{
  "McpHub": {
    "Enabled": true,
    "HubBaseUrl": "https://mcp-hub.example.com/api",
    "RegistrationEndpoint": "McpMicroservice",
    "RetryIntervalSeconds": 60,
    "PublicBaseUrl": "https://dev.digiwells.no",
    "ServiceName": "Cluster",
    "InstanceId": "",
    "UnregisterOnShutdown": true
  }
}
```

## API & Swagger
- Swagger UI: `https://localhost:5001/Cluster/api/swagger`
- OpenAPI JSON (merged): `https://localhost:5001/Cluster/api/swagger/merged/swagger.json`
- Base endpoints (controller `ClusterController`):
  - `GET /Cluster/api/Cluster` → list of Cluster IDs (GUID)
  - `GET /Cluster/api/Cluster/MetaInfo` → list of `MetaInfo`
  - `GET /Cluster/api/Cluster/{id}` → single Cluster by ID
  - `GET /Cluster/api/Cluster/HeavyData` → full list of Clusters
  - `POST /Cluster/api/Cluster` → add new Cluster
  - `PUT /Cluster/api/Cluster/{id}` → update existing Cluster
  - `DELETE /Cluster/api/Cluster/{id}` → delete Cluster
- Usage statistics: `GET /Cluster/api/ClusterUsageStatistics`
- Identity definitions:
  - `GET /Cluster/api/ClusterIdentity`
  - `GET /Cluster/api/ClusterIdentity/HeavyData`
  - `GET /Cluster/api/ClusterIdentity/{id}`
  - `POST /Cluster/api/ClusterIdentity`
  - `PUT /Cluster/api/ClusterIdentity/{id}`
  - `DELETE /Cluster/api/ClusterIdentity/{id}`
- Cluster feature category definitions:
  - `GET /Cluster/api/ClusterFeatureCategory`
  - `GET /Cluster/api/ClusterFeatureCategory/HeavyData`
  - `GET /Cluster/api/ClusterFeatureCategory/{id}`
  - `POST /Cluster/api/ClusterFeatureCategory`
  - `PUT /Cluster/api/ClusterFeatureCategory/{id}`
  - `DELETE /Cluster/api/ClusterFeatureCategory/{id}`
- Slot feature category definitions:
  - `GET /Cluster/api/SlotFeatureCategory`
  - `GET /Cluster/api/SlotFeatureCategory/HeavyData`
  - `GET /Cluster/api/SlotFeatureCategory/{id}`
  - `POST /Cluster/api/SlotFeatureCategory`
  - `PUT /Cluster/api/SlotFeatureCategory/{id}`
  - `DELETE /Cluster/api/SlotFeatureCategory/{id}`

## MCP Server

The service exposes a Model Context Protocol endpoint alongside the REST API:

- Streamable HTTP transport: `/Cluster/api/mcp`
- WebSocket transport: `/Cluster/api/mcp/ws`

The MCP tool surface mirrors the REST API:

- `ping`
- Cluster: `cluster_get_all_ids`, `cluster_get_all_meta_info`, `cluster_get_by_id`, `cluster_get_all`, `cluster_get_all_light`, `cluster_get_all_by_field_id`, `cluster_get_all_by_rig_id`, `cluster_get_all_single_well`, `cluster_get_all_fixed_platform`, `cluster_create`, `cluster_update_by_id`, `cluster_delete_by_id`
- ClusterIdentity: `cluster_identity_...`
- ClusterFeatureCategory: `cluster_feature_category_...`
- SlotFeatureCategory: `slot_feature_category_...`
- Usage statistics: `cluster_usage_statistics_get`

The `create` and `update_by_id` tools expect the same JSON object body as the corresponding REST endpoints, wrapped in an argument named after the entity, for example `cluster`, `clusterIdentity`, `clusterFeatureCategory`, or `slotFeatureCategory`. Every REST-backed tool publishes an explicit JSON input schema with descriptions for identifiers, filters, metadata, associations, feature and identity assignments, nested slots, Gaussian uncertainty, and update identity matching. Cluster coordinates use SI values and WGS84 references: angular values are radians and linear/depth values are meters.

When `McpHub:Enabled` is true, the service registers itself on the configured MCP hub with a fixed service type id, a configured or persisted instance id, and MCP endpoint URLs derived from `PublicBaseUrl`:

- `PublicBaseUrl + "/Cluster/api/mcp"`
- `PublicBaseUrl` converted to `ws`/`wss` plus `"/Cluster/api/mcp/ws"`

If `HubBaseUrl` or `PublicBaseUrl` is missing, registration is skipped. If the hub is configured but unreachable, registration is retried every `RetryIntervalSeconds` seconds. On graceful shutdown, the service attempts to unregister its instance when `UnregisterOnShutdown` is true.

## Usage Examples
Assuming `https://localhost:5001` and base path `/Cluster/api`.

- List Cluster IDs:
  - `curl -k https://localhost:5001/Cluster/api/Cluster`
- Get all Cluster meta info:
  - `curl -k https://localhost:5001/Cluster/api/Cluster/MetaInfo`
- Get a Cluster by ID:
  - `curl -k https://localhost:5001/Cluster/api/Cluster/<guid>`
- Create a Cluster (minimal JSON requires a non-empty `MetaInfo.ID`):
  - `curl -k -X POST https://localhost:5001/Cluster/api/Cluster -H "Content-Type: application/json" -d "{ \"MetaInfo\": { \"ID\": \"11111111-1111-1111-1111-111111111111\" }, \"Name\": \"Cluster A\", \"IsSingleWell\": false }"`
- Update a Cluster:
  - `curl -k -X PUT https://localhost:5001/Cluster/api/Cluster/<guid> -H "Content-Type: application/json" -d "{ \"MetaInfo\": { \"ID\": \"<guid>\" }, \"Name\": \"Cluster A (updated)\" }"`
- Delete a Cluster:
  - `curl -k -X DELETE https://localhost:5001/Cluster/api/Cluster/<guid>`

Note: The data model in `Model` defines richer, domain-specific fields (e.g., reference coordinates and depths). The controller only requires `MetaInfo.ID` to be present for create/update operations; populate additional fields as needed for your workflow.

## Data Persistence
- Database: SQLite at `home/Cluster.db` (relative to solution root).
- Optional external service configuration and the generated MCP hub instance id can also live under the shared `home` folder.
- On startup, the service validates the DB schema; if mismatches are found, it creates a timestamped backup and rebuilds tables.
- Main tables:
  - `ClusterTable`
  - `ClusterIdentityTable`
  - `ClusterFeatureCategoryTable`
  - `SlotFeatureCategoryTable`
- Identity and feature definition tables store complete JSON serializations of their model instances, matching the cluster persistence pattern.

## Docker
- Build (from repo root):
  - `docker build -t norcedrillingclusterservice -f Service/Dockerfile .`
- Run:
  - `docker run --rm -p 5001:8080 -v %CD%/home:/home norcedrillingclusterservice` (Windows PowerShell)
  - `docker run --rm -p 5001:8080 -v $(pwd)/home:/home norcedrillingclusterservice` (bash)
- Access: `https://localhost:5001/Cluster/api/swagger` (container listens on 8080; mapped to 5001 above)

## Dependencies
- NuGet packages (Service):
  - `Microsoft.Data.Sqlite` (SQLite driver)
  - `Microsoft.OpenApi`, `Microsoft.OpenApi.Readers` (OpenAPI document handling)
  - `Swashbuckle.AspNetCore.SwaggerGen`, `Swashbuckle.AspNetCore.SwaggerUI` (Swagger generation/UI)
- Project reference:
  - `Model/Model.csproj` (domain models like `Cluster`, `MetaInfo`, and usage statistics)
- Build-time (Debug):
  - `dotnet swagger tofile` target emits a schema to `ModelSharedOut/json-schemas/ClusterFullName.json` (install CLI tool if needed).

## Integration In The Solution
- `Model`: Defines shared DTOs and domain objects used by this service.
- `WebApp`: Frontend UI that calls this service; configure `WebApp/appsettings.*.json` `ClusterHostURL` to point to the service base URL.
- `ServiceTest`: Integration and API tests that exercise the endpoints.
- `wwwroot/json-schema/ClusterMergedModel.json`: Merged schema used to serve a single Swagger document at `/Cluster/api/swagger/merged/swagger.json`.

## Source & Credits
- Generated from NORCE Drilling and Wells .NET template (see Templates repo and wiki for details).
- Container image name: `norcedrillingclusterservice` (see Digiwells org on Docker Hub).
