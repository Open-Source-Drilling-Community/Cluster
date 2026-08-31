# WebApp (Cluster UI)

The WebApp project is a Blazor Server application that provides a user interface to manage Cluster data stored by the Service (Cluster microservice). It consumes the Service API via generated, strongly-typed clients from `ModelSharedOut` and renders forms, tables, and charts with MudBlazor components.

## Purpose In The Solution
- Frontend UI hosted under the base path `/Cluster/webapp` (see `WebApp/Program.cs`).
- Calls the Cluster microservice at `/Cluster/api` using generated clients from `ModelSharedOut`.
- Also integrates with Field, Trajectory, Rig, EarthCartographicProjection, EarthGeodesy, EarthVerticalDatum, EarthGravity, EarthMagneticField, and UnitConversion services for auxiliary data, displays, calculations, and unit/reference handling.

## Prerequisites
- .NET SDK 8.0+
- Optional: Docker (to containerize and run the webapp)

## Configuration
- App settings: `WebApp/appsettings.Development.json`, `WebApp/appsettings.Production.json` expose host URLs:
  - `ClusterHostURL`: base URL of the Cluster Service (e.g., `https://localhost:5001/`).
  - `FieldHostURL`: base URL of the Field Service.
  - `TrajectoryHostURL`: base URL of the Trajectory Service.
  - `RigHostURL`: base URL of the Rig Service.
  - `EarthCartographicProjectionHostURL`: base URL of EarthCartographicProjection.
  - `EarthGeodesyHostURL`: base URL of EarthGeodesy.
  - `EarthVerticalDatumHostURL`: base URL of EarthVerticalDatum.
  - `EarthGravityHostURL`: base URL of EarthGravity.
  - `EarthMagneticFieldHostURL`: base URL of EarthMagneticField.
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
- Contextual Rig page: `/Cluster/webapp/Rig` hosts the `OSDC.Drilling.Rig.WebPages` catalog and editor against the configured Rig service.
- Detail page: edit cluster metadata, field association, reference coordinates, identities, features, environment depths, slots, and slot features.
- Admin pages: manage cluster identities, cluster feature categories/options, and slot feature categories/options.
- Backup and restore: `/Cluster/webapp/ClusterBackupRestore` exports multiple clusters with their referenced local definitions and reconnects Field/Rig references during atomic restore.
- Display pages: show cluster trajectories and survey runs in 3D and horizontal projection.
- Field delineation overlays: cluster trajectory and survey-run displays load delineation lines from the selected field and draw original lines plus calculated boundaries. Boundaries are dashed in the horizontal projection. In 3D, delineation lines are placed on the north/east plane at the top or bottom of the survey/trajectory bounding box without changing the plot bounds.
- Calculators menu:
  - `Cartographic Conversion` opens the Field cartographic conversion page at `/Cluster/webapp/FieldCartographicConverter`.
  - `Vertical Datum Conversion` opens the EarthVerticalDatum calculator at `/Cluster/webapp/EarthVerticalDatumCalculation`.
  - `Earth Gravity Evaluation` opens the EarthGravity calculator at `/Cluster/webapp/EarthGravityCalculation`.
  - `Earth Magnetic Field Evaluation` opens the EarthMagneticField calculator at `/Cluster/webapp/EarthMagneticFieldCalculation`.
- The UI uses the generated `Client` from `ModelSharedOut` to call endpoints like:
  - `GET /Cluster/api/Cluster`, `GET /Cluster/api/Cluster/{id}`
  - `POST /Cluster/api/Cluster`, `PUT /Cluster/api/Cluster/{id}`, `DELETE /Cluster/api/Cluster/{id}`
- Unit selection and conversions leverage components from `OSDC.UnitConversion.DrillingRazorMudComponents`.
- Reference handling supports field, cluster, cartographic, geodetic, MSL, ground/mud line, and other configured reference sources through the shared unit/reference components.

## Docker

The published image is `digiwells/osdcdrillingclusterwebappclient:stable`. The Helm chart is `WebApp/charts/osdcdrillingclusterwebappclient`, creates the internal resource name `osdcclusterwebappclient`, and calls the Cluster service at `http://osdcclusterservice/` in production.
- Build (from repo root):
  - `docker build -t osdcdrillingclusterwebappclient -f WebApp/Dockerfile .`
- Run:
  - PowerShell: `docker run --rm -p 5011:8080 -e ASPNETCORE_URLS=http://+:8080 -e ClusterHostURL=https://host.docker.internal:5001/ osdcdrillingclusterwebappclient`
  - Bash: `docker run --rm -p 5011:8080 -e ASPNETCORE_URLS=http://+:8080 -e ClusterHostURL=https://host.docker.internal:5001/ osdcdrillingclusterwebappclient`
- Access UI: `http://localhost:5011/Cluster/webapp/Cluster`

## Dependencies
- Project references:
  - `ModelSharedOut/ModelSharedOut.csproj` — provides generated DTOs and the typed `Client` used by `APIUtils`.
- NuGet packages (declared in `WebApp/WebApp.csproj`):
  - `OSDC.DotnetLibraries.General.DataManagement` — general utilities.
  - `OSDC.UnitConversion.DrillingRazorMudComponents` — UI components for unit systems (brings MudBlazor transitively).
  - `Plotly.Blazor` — charting components.
  - `OSDC.Drilling.Field.WebPages` — field pages, including the cartographic conversion calculator.
  - `OSDC.Drilling.Rig.WebPages` — the rig catalog and detailed rig editor.
  - `OSDC.Drilling.EarthCartographicProjection.WebPages` — projection-definition pages.
  - `OSDC.Drilling.EarthGeodesy.WebPages` — geodetic-datum and spheroid pages.
  - `OSDC.Drilling.EarthVerticalDatum.WebPages`, `OSDC.Drilling.EarthGravity.WebPages`, and `OSDC.Drilling.EarthMagneticField.WebPages` — synchronous calculator pages.
- UI framework:
  - MudBlazor services are added in `WebApp/Program.cs`.

## Integration With The Solution
- Service: backend API provider; configure `ClusterHostURL` to point to it. The Service publishes Swagger UI at `/Cluster/api/swagger` and serves the merged schema consumed by clients.
- ModelSharedOut: generates `ClusterMergedModel.cs` and merged OpenAPI used by WebApp for strongly-typed calls and by the Service for Swagger UI.
- ServiceTest: shares the same generated models for end-to-end and integration tests.
- External Razor pages: `WebApp/ExternalRazorAssemblies.cs` registers the Field and Rig page assemblies. Local wrapper pages host the EarthCartographicProjection, EarthGeodesy, EarthVerticalDatum, EarthGravity, and EarthMagneticField components under the Cluster web app path base; service registration is centralized in `WebApp/ExternalWebPagesServiceCollectionExtensions.cs`.
- Helm chart: `WebApp/charts/osdcdrillingclusterwebappclient/values.yaml` configures ingress at `/Cluster/webapp` for various hosts.

## Notes
- Path base: The app is mounted at `/Cluster/webapp` (`UsePathBase` in `WebApp/Program.cs`). If reverse-proxying, ensure the ingress/path matches this setting.
- Certificates: `APIUtils` disables certificate validation for development convenience; use trusted certificates in production.

## Shared-page package versions

The exact shared-page versions are declared in `WebApp.csproj`. Keep package references, dependency injection registrations, route wrappers, configuration keys, and generated dependency schemas synchronized when an upstream service contract changes.
