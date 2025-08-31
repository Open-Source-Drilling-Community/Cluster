# WebApp (Cluster UI)

The WebApp project is a Blazor Server application that provides a user interface to manage Cluster data stored by the Service (Cluster microservice). It consumes the Service API via generated, strongly-typed clients from `ModelSharedOut` and renders forms, tables, and charts with MudBlazor components.

## Purpose In The Solution
- Frontend UI hosted under the base path `/Cluster/webapp` (see `WebApp/Program.cs`).
- Calls the Cluster microservice at `/Cluster/api` using generated clients from `ModelSharedOut`.
- Also integrates with Field and UnitConversion services for auxiliary data and unit handling.

## Prerequisites
- .NET SDK 8.0+
- Optional: Docker (to containerize and run the webapp)

## Configuration
- App settings: `WebApp/appsettings.Development.json`, `WebApp/appsettings.Production.json` expose host URLs:
  - `ClusterHostURL`: base URL of the Cluster Service (e.g., `https://localhost:5001/`).
  - `FieldHostURL`: base URL of the Field Service.
  - `UnitConversionHostURL`: base URL of the UnitConversion Service.
- Defaults for local development:
  - WebApp URLs: `https://localhost:5011; http://localhost:5012` (see `WebApp/Properties/launchSettings.json`).
  - Service base path: `/Cluster/webapp`, so the main page is at `https://localhost:5011/Cluster/webapp/Cluster`.

## Installation
1. Restore and build:
   - `dotnet restore`
   - `dotnet build WebApp -c Debug`
2. Run locally:
   - `dotnet run --project WebApp`
   - Navigate to `https://localhost:5011/Cluster/webapp/Cluster`
3. Ensure the Service is running and reachable at the configured `ClusterHostURL` (default `https://localhost:5001/`).

## Usage
- Main page: Cluster list with search, selection, add, and delete actions.
- Detail page: Edit cluster metadata, reference coordinates, slots, and related parameters.
- The UI uses the generated `Client` from `ModelSharedOut` to call endpoints like:
  - `GET /Cluster/api/Cluster`, `GET /Cluster/api/Cluster/{id}`
  - `POST /Cluster/api/Cluster`, `PUT /Cluster/api/Cluster/{id}`, `DELETE /Cluster/api/Cluster/{id}`
- Unit selection and conversions leverage components from `OSDC.UnitConversion.DrillingRazorMudComponents`.

## Docker
- Build (from repo root):
  - `docker build -t norcedrillingclusterwebappclient -f WebApp/Dockerfile .`
- Run:
  - PowerShell: `docker run --rm -p 5011:8080 -e ASPNETCORE_URLS=http://+:8080 -e ClusterHostURL=https://host.docker.internal:5001/ norcedrillingclusterwebappclient`
  - Bash: `docker run --rm -p 5011:8080 -e ASPNETCORE_URLS=http://+:8080 -e ClusterHostURL=https://host.docker.internal:5001/ norcedrillingclusterwebappclient`
- Access UI: `http://localhost:5011/Cluster/webapp/Cluster`

## Dependencies
- Project references:
  - `ModelSharedOut/ModelSharedOut.csproj` — provides generated DTOs and the typed `Client` used by `APIUtils`.
- NuGet packages (declared in `WebApp/WebApp.csproj`):
  - `OSDC.DotnetLibraries.General.DataManagement` — general utilities.
  - `OSDC.UnitConversion.DrillingRazorMudComponents` — UI components for unit systems (brings MudBlazor transitively).
  - `Plotly.Blazor` — charting components.
- UI framework:
  - MudBlazor services are added in `WebApp/Program.cs`.

## Integration With The Solution
- Service: backend API provider; configure `ClusterHostURL` to point to it. The Service publishes Swagger UI at `/Cluster/api/swagger` and serves the merged schema consumed by clients.
- ModelSharedOut: generates `ClusterMergedModel.cs` and merged OpenAPI used by WebApp for strongly-typed calls and by the Service for Swagger UI.
- ServiceTest: shares the same generated models for end-to-end and integration tests.
- Helm chart: `WebApp/charts/norcedrillingclusterwebappclient/values.yaml` configures ingress at `/Cluster/webapp` for various hosts.

## Notes
- Path base: The app is mounted at `/Cluster/webapp` (`UsePathBase` in `WebApp/Program.cs`). If reverse-proxying, ensure the ingress/path matches this setting.
- Certificates: `APIUtils` disables certificate validation for development convenience; use trusted certificates in production.
