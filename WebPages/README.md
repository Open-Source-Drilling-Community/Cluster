# NORCE.Drilling.Cluster.WebPages

`NORCE.Drilling.Cluster.WebPages` is a Razor class library that packages the cluster management pages, reference-aware editors, feature/identity administration pages, display pages, and plotting components required by the Cluster web application.

## Contents

- `ClusterMain`
- `ClusterEdit`
- `ClusterIdentities`
- `ClusterFeatures`
- `SlotFeatures`
- `ClusterSurveyRuns`
- `ClusterTrajectories`
- `ClusterBackupRestore`
- `StatisticsMain`
- `ScatterPlot`
- `Scatter3DPlot`
- Cluster page support classes such as API access helpers and unit/reference helpers

Calculator pages used by the Cluster web application are supplied by current OSDC Razor packages rather than this library:

- `OSDC.Drilling.Field.WebPages.FieldCartographicConverter` at `/FieldCartographicConverter`
- EarthVerticalDatum, EarthGravity, and EarthMagneticField components hosted by wrapper routes in `WebApp/Pages`

Cluster coordinate and mean-sea-level conversions call the Field and EarthVerticalDatum synchronous APIs directly. The library does not create, poll, or delete persisted calculation orders.

## Cluster Editing

`ClusterEdit` provides:

- Save/close workflows with unsaved-change confirmation.
- JSON import/export for clusters.
- Field association.
- Reference point editing with north/east, latitude/longitude, and depth values through the unit/reference system.
- Cluster identity assignment.
- Cluster feature assignment with exclusive/non-exclusive categories and optional date validity periods.
- Slot editing with row selection, north/east and latitude/longitude columns, shared coordinate accuracy values, and deletion of selected rows.
- Slot feature assignment driven by selected slot rows. Assigned slot features are summarized in the slot table.

## Batch Backup and Restore

`ClusterBackupRestore` exports all clusters or an ordered selection to one JSON document. It restores with fail-on-collision or replace behavior and lets the user either map existing local catalog definitions or create missing definitions/options. Field and Rig references are included by UUID and verified name and are validated live during restore; ambiguous or missing matches reject the complete operation.

## Feature and Identity Management

The web pages include dedicated administration pages for:

- Cluster identities: definitions such as official name, short name, common name, external database ID, WITSML UID, and similar identifiers.
- Cluster features: category/option definitions that may be exclusive and may require validity periods.
- Slot features: category/option definitions for slot status, usage, integrity, accessibility, readiness, operational constraints, and geometry confidence.

## Cluster Displays

`ClusterSurveyRuns` and `ClusterTrajectories` display all survey runs or trajectories associated with the selected field and cluster.

- The 3D view shows survey/trajectory traces and selected uncertainty ellipses.
- The horizontal projection shows the same data on the north/east plane.
- If the selected field defines delineation lines, the pages overlay the original delineation lines and calculated boundary lines.
- Boundary lines are drawn dashed in the horizontal projection.
- In the 3D view, delineation lines are displayed on the north/east plane at the top or bottom of the survey/trajectory bounding box, depending on camera angle.
- The 3D bounding box is based only on survey/trajectory data and uncertainty traces that contribute to bounds; delineation lines do not enlarge the plot bounds.

## Dependencies

The package depends on:

- `ModelSharedOut`
- `OSDC.DotnetLibraries.Drilling.WebAppUtils`
- `MudBlazor`
- `OSDC.UnitConversion.DrillingRazorMudComponents`
- `Plotly.Blazor`

## Host application requirements

The consuming web app is expected to:

1. Reference this package.
2. Provide an implementation of `IClusterWebPagesConfiguration`.
3. Register that configuration and `IClusterAPIUtils` in dependency injection.
4. Include the library assembly in Blazor routing via `AdditionalAssemblies`.
5. If hosting the same contextual-data and calculator menus as the Cluster web app, reference and register the Field and Earth service web page packages and provide wrapper routes where required.

Example registration:

```csharp
builder.Services.AddSingleton<IClusterWebPagesConfiguration>(new WebPagesHostConfiguration
{
    ClusterHostURL = builder.Configuration["ClusterHostURL"] ?? string.Empty,
    FieldHostURL = builder.Configuration["FieldHostURL"] ?? string.Empty,
    RigHostURL = builder.Configuration["RigHostURL"] ?? string.Empty,
    UnitConversionHostURL = builder.Configuration["UnitConversionHostURL"] ?? string.Empty
});
builder.Services.AddSingleton<IClusterAPIUtils, ClusterAPIUtils>();
```

Example routing:

```razor
<Router AppAssembly="@typeof(App).Assembly"
        AdditionalAssemblies="new[] { typeof(NORCE.Drilling.Cluster.WebPages.ClusterMain).Assembly }">
```

The Cluster web app registers the Field assembly and provides wrapper pages for EarthCartographicProjection, EarthGeodesy, EarthVerticalDatum, EarthGravity, and EarthMagneticField components. This keeps all user-facing routes under `/Cluster/webapp`.
