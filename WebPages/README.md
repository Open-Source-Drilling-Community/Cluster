# NORCE.Drilling.Cluster.WebPages

`NORCE.Drilling.Cluster.WebPages` is a Razor class library that packages the `ClusterMain`, `ClusterEdit`, and `StatisticsMain` pages together with the plotting component and page utilities they require.

## Contents

- `ClusterMain`
- `ClusterEdit`
- `StatisticsMain`
- `ScatterPlot`
- Cluster page support classes such as API access helpers and unit/reference helpers

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
