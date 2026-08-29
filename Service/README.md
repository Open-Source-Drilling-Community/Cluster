# Service (Cluster Microservice)

The Service project is an ASP.NET Core Web API that exposes REST endpoints to create, read, update, and delete Cluster domain objects and related cluster configuration objects. It persists data in a local SQLite database and publishes an OpenAPI schema for client generation and UI exploration. This microservice is consumed by the WebApp project and automated tests in ServiceTest.

## Purpose In The Solution
- Provides the backend API for Cluster operations at the base path `/Cluster/api`.
- Provides CRUD APIs for cluster identity definitions, cluster feature categories, and slot feature categories.
- Provides versioned batch backup and atomic restore of clusters and their dependency closure.
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
   - `Service/appsettings.*.json` configures `FieldHostURL` and `RigHostURL` for live backup/restore reference validation, and supports `WellHostURL`.
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
  - `POST /Cluster/api/Cluster/BatchExport` → export all clusters or an ordered selection with dependency manifests
  - `POST /Cluster/api/Cluster/BatchRestore` → validate, reconnect references, and atomically restore a document
  - `POST /Cluster/api/Cluster` → add new Cluster
  - `PUT /Cluster/api/Cluster/{id}?expectedModifiedUtc=<timestamp>` → replace an existing Cluster using its latest `LastModificationDate`
  - `DELETE /Cluster/api/Cluster/{id}` → delete Cluster
- Usage statistics: `GET /Cluster/api/ClusterUsageStatistics`
- Identity definitions:
  - `GET /Cluster/api/ClusterIdentity`
  - `GET /Cluster/api/ClusterIdentity/HeavyData`
  - `GET /Cluster/api/ClusterIdentity/{id}`
  - `POST /Cluster/api/ClusterIdentity`
  - `PUT /Cluster/api/ClusterIdentity/{id}?expectedModifiedUtc=<timestamp>`
  - `DELETE /Cluster/api/ClusterIdentity/{id}`
- Cluster feature category definitions:
  - `GET /Cluster/api/ClusterFeatureCategory`
  - `GET /Cluster/api/ClusterFeatureCategory/HeavyData`
  - `GET /Cluster/api/ClusterFeatureCategory/{id}`
  - `POST /Cluster/api/ClusterFeatureCategory`
  - `PUT /Cluster/api/ClusterFeatureCategory/{id}?expectedModifiedUtc=<timestamp>`
  - `DELETE /Cluster/api/ClusterFeatureCategory/{id}`
- Slot feature category definitions:
  - `GET /Cluster/api/SlotFeatureCategory`
  - `GET /Cluster/api/SlotFeatureCategory/HeavyData`
  - `GET /Cluster/api/SlotFeatureCategory/{id}`
  - `POST /Cluster/api/SlotFeatureCategory`
  - `PUT /Cluster/api/SlotFeatureCategory/{id}?expectedModifiedUtc=<timestamp>`
  - `DELETE /Cluster/api/SlotFeatureCategory/{id}`

## MCP Server

The service exposes a Model Context Protocol endpoint alongside the REST API:

- Streamable HTTP transport: `/Cluster/api/mcp`
- WebSocket transport: `/Cluster/api/mcp/ws`

The MCP tool surface exposes the domain CRUD and batch-transfer subset of the REST API:

- `ping`
- Cluster: `cluster_get_all_ids`, `cluster_get_all_meta_info`, `cluster_get_by_id`, `cluster_get_all`, `cluster_get_all_light`, `cluster_get_all_by_field_id`, `cluster_get_all_by_rig_id`, `cluster_get_all_single_well`, `cluster_get_all_fixed_platform`, `cluster_create`, `cluster_update_by_id`, `cluster_delete_by_id`, `cluster_batch_export`, `cluster_batch_restore`
- ClusterIdentity: `cluster_identity_...`
- ClusterFeatureCategory: `cluster_feature_category_...`
- SlotFeatureCategory: `slot_feature_category_...`
- Usage statistics remain available through REST and are intentionally not exposed through MCP.

The `create` and `update_by_id` tools expect the same JSON object body as the corresponding REST endpoints, wrapped in an argument named after the entity, for example `cluster`, `clusterIdentity`, `clusterFeatureCategory`, or `slotFeatureCategory`. Updates also require `expectedModifiedUtc`, copied exactly from the latest server `LastModificationDate`; a stale value returns `concurrency_conflict` (HTTP 409). Every tool publishes explicit input and success-output JSON Schemas plus a human-readable title and read-only, destructive, idempotent, and open-world behavior annotations. Successful calls return schema-conforming structured content and a JSON text fallback. Failures return `isError=true` with a stable JSON text envelope and no success-shaped structured content. Cluster coordinates use SI values and WGS84 references: angular values are radians and linear/depth values are metres.

Ordinary Cluster creates and updates atomically validate Cluster identity assignments, Cluster and Slot feature category/option assignments, and the invariant that each `Slots` dictionary key equals the contained `Slot.ID`. Deleting a locally owned identity or feature category, or removing an option, returns `reference_conflict` (HTTP 409) while a stored Cluster uses it. These operations never cascade. Creation and modification timestamps are assigned by the server.

The batch document uses `OSDC.Drilling.Cluster.BatchExport`, schema version 1. It contains complete clusters, only referenced local identity and cluster/slot feature definitions/options, and Field/Rig source UUID-to-name manifests. Restore resolves local catalogs by compatible UUID or unique normalized name and can create missing local definitions/options. Field and Rig resources are never created: an existing UUID is retained even if its display name changed; when that UUID is absent, the stored name must have one unique normalized-name match. All local catalog creation, reference rewriting, and cluster writes share one SQLite transaction.

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
  - First read the Cluster, retain the complete record and its `LastModificationDate`, then call `curl -k -X PUT "https://localhost:5001/Cluster/api/Cluster/<guid>?expectedModifiedUtc=<URL-encoded-last-modification-date>" -H "Content-Type: application/json" -d "<complete-cluster-json>"`
- Delete a Cluster:
  - `curl -k -X DELETE https://localhost:5001/Cluster/api/Cluster/<guid>`

Note: Updates are full replacements. Send every value that must remain stored, and use only locally catalogued identity/category/option UUIDs. The server rejects invalid references and mismatched Slot keys without changing the database.

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

The published image is `digiwells/osdcdrillingclusterservice:stable`. The Helm chart is `Service/charts/osdcdrillingclusterservice` and creates the internal service name `osdcclusterservice`. Existing installations must adopt `cluster-claim` through `persistence.existingClaim`; follow `deployment/identity-cutover.md` so the original Helm release cannot delete the PVC.
- Build (from repo root):
  - `docker build -t osdcdrillingclusterservice -f Service/Dockerfile .`
- Run:
  - `docker run --rm -p 5001:8080 -v %CD%/home:/home osdcdrillingclusterservice` (Windows PowerShell)
  - `docker run --rm -p 5001:8080 -v $(pwd)/home:/home osdcdrillingclusterservice` (bash)
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
- Container image name: `osdcdrillingclusterservice` (see Digiwells org on Docker Hub).
