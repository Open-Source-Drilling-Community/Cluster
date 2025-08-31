# Cluster Solution

A complete, domain-focused solution for managing Cluster data, composed of an ASP.NET Core microservice (Service), a Blazor Server frontend (WebApp), shared model/code-generation utilities (ModelSharedOut), and domain models (Model). The solution exposes a REST API with an OpenAPI definition and provides a UI for CRUD operations on Cluster entities.

## Projects
- Service: ASP.NET Core Web API at base path `/Cluster/api`; persists data in SQLite and serves Swagger UI. See `Service/README.md`.
- WebApp: Blazor Server UI at base path `/Cluster/webapp`; consumes the Service via generated clients. See `WebApp/README.md`.
- Model: Domain models and statistics used by Service and clients. See `Model/README.md`.
- ModelSharedOut: Generates merged OpenAPI and C# client/DTOs for consumers; outputs JSON to Service and code to itself. See `ModelSharedOut/README.md`.
- ServiceTest: Tests that target the running Service using the generated client.
- ModelTest: Unit tests for domain logic in Model.
- home: Local storage folder used by the Service for SQLite (`home/Cluster.db`).

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
  - Navigate to `https://localhost:5011/Cluster/webapp/Cluster`

## Docker (Optional)
- Build images from repo root:
  - Service: `docker build -t norcedrillingclusterservice -f Service/Dockerfile .`
  - WebApp: `docker build -t norcedrillingclusterwebappclient -f WebApp/Dockerfile .`
- Run locally:
  - Service: `docker run --rm -p 5001:8080 -v %CD%/home:/home norcedrillingclusterservice` (PowerShell)
  - WebApp: `docker run --rm -p 5011:8080 -e ASPNETCORE_URLS=http://+:8080 -e ClusterHostURL=https://host.docker.internal:5001/ norcedrillingclusterwebappclient`

## Dependencies
- Runtime packages (high level):
  - Service: `Microsoft.Data.Sqlite`, `Swashbuckle.AspNetCore.*`, `Microsoft.OpenApi*`.
  - Model: `OSDC.DotnetLibraries.*` (Common, DataManagement, Statistics, DrillingProperties).
  - WebApp: `OSDC.UnitConversion.DrillingRazorMudComponents`, `Plotly.Blazor`, `OSDC.DotnetLibraries.General.DataManagement`.
  - ModelSharedOut: `Microsoft.OpenApi.Readers`, `NSwag.CodeGeneration.CSharp`.
- Project references:
  - Service → Model
  - WebApp → ModelSharedOut
  - ServiceTest → Service, ModelSharedOut
  - ModelTest → Model
- External services (optional but supported):
  - Field service and UnitConversion service URLs are configurable in `WebApp/appsettings.*.json`.

## Notes
- Path bases: Service uses `/Cluster/api`; WebApp uses `/Cluster/webapp`. Ensure reverse proxies/ingress match these paths.
- Persistence: SQLite file at `home/Cluster.db`. Startup validates schema and backs up on mismatch.
- Swagger: Service exposes UI and a merged OpenAPI JSON at `/Cluster/api/swagger/merged/swagger.json`.

## Links
- Docker Hub organization: https://hub.docker.com/?namespace=digiwells
- Templates and docs: https://github.com/NORCE-DrillingAndWells/DrillingAndWells/wiki

## Demo Environment
- Example Service (dev): `https://dev.DigiWells.no/Cluster/api/swagger` (Swagger UI) and `https://dev.DigiWells.no/Cluster/api/Cluster` (API)
- Example WebApp (dev): `https://dev.DigiWells.no/Cluster/webapp/Cluster`
