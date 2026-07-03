# Cluster Model Library

The Model project provides the domain types used by the Cluster microservice and clients. It contains the core data structures representing a Cluster and related concepts that are exchanged via the REST API, persisted in storage, and used across the solution.

- Namespace: `NORCE.Drilling.Cluster.Model`
- Target framework: `net8.0`

## Purpose

The library defines plain data models with semantic annotations for drilling applications:

- `Cluster`: A logical group (or single well) with identity, metadata (`MetaInfo`), geospatial reference and depth-related properties, selected identity assignments, selected feature assignments, and a set of `Slot`s.
- `ClusterLight`: A lighter representation used when full cluster payloads are not required.
- `Slot`: A location within a cluster with identity, position properties, and optional slot feature assignments.
- `ClusterIdentity` and `ClusterIdentityAssignment`: user-defined identity definitions and cluster-specific assigned values.
- `ClusterFeatureCategory`, `ClusterFeatureOption`, and `ClusterFeatureAssignment`: user-defined cluster feature definitions and assignments. Categories may be exclusive and may require validity periods.
- `SlotFeatureCategory`, `SlotFeatureOption`, and `SlotFeatureAssignment`: user-defined slot feature definitions and assignments. Categories follow the same exclusivity and validity-period model as cluster features.
- `UsageStatisticsCluster`: Lightweight telemetry tracking of REST endpoint usage counts, with simple JSON file backup.

Key files:
- `Model/Cluster.cs:1`
- `Model/Slot.cs:1`
- `Model/UsageStatisticsCluster.cs:1`

## Usage Examples

Basic construction and JSON serialization:

```csharp
using System;
using System.Text.Json;
using NORCE.Drilling.Cluster.Model;
using OSDC.DotnetLibraries.General.DataManagement; // for MetaInfo

var id = Guid.NewGuid();
var cluster = new Cluster
{
    MetaInfo = new MetaInfo { ID = id },
    Name = "West Ridge",
    Description = "Pilot cluster",
    IsSingleWell = false,
    FieldID = null,
    Slots = new Dictionary<Guid, Slot>()
};

// Add a slot
var slotId = Guid.NewGuid();
cluster.Slots[slotId] = new Slot
{
    ID = slotId,
    Name = "Slot-A",
    Description = "North-east slot",
    SlotFeatureAssignments =
    [
        new SlotFeatureAssignment
        {
            ID = Guid.NewGuid(),
            FeatureCategoryID = Guid.NewGuid(),
            FeatureOptionID = Guid.NewGuid()
        }
    ]
};

// Serialize (for API calls or persistence)
var json = JsonSerializer.Serialize(cluster);
```

Working with endpoint usage stats (optional telemetry):

```csharp
using NORCE.Drilling.Cluster.Model;

// Increment counters as API calls are served
UsageStatisticsCluster.Instance.IncrementGetAllClusterIdPerDay();
UsageStatisticsCluster.Instance.IncrementPostClusterPerDay();
```

Notes:
- Coordinate and depth properties use `GaussianDrillingProperty` from OSDC libraries and are decorated with semantic attributes (DWIS vocabulary). They can be assigned as needed, or left `null` if not applicable.
- Feature assignments use category and option GUIDs. Validation of whether the referenced category/option exists, whether dates are required, and whether exclusive categories overlap is performed by the web/editor workflows before saving.
- Slot position values are persisted as WGS84 latitude/longitude drilling properties; the web editor uses unit/reference components to also display and edit north/east values.

## Dependencies

Direct NuGet dependencies (see `Model/Model.csproj:1`):
- `OSDC.DotnetLibraries.Drilling.DrillingProperties`
- `OSDC.DotnetLibraries.General.Common`
- `OSDC.DotnetLibraries.General.DataManagement`
- `OSDC.DotnetLibraries.General.Statistics`

The code also uses semantic attributes from DWIS vocabulary packages (via using directives in the models) to enrich metadata. These attributes do not add runtime behavior in this project but can aid downstream documentation and tooling.

## Integration in the Solution

- The REST API project `Service` references this library to define request/response contracts and to persist entities.
  - Example controller: `Service/Controllers/ClusterController.cs:1`
  - Manager layer (SQLite persistence): `Service/Managers/ClusterManager.cs:1`
- The web assets and generated OpenAPI schemas reference the model types for documentation and client generation (see `Service/wwwroot/json-schema/`).
- Tests in `ServiceTest` and `ModelTest` consume the Model types directly.

Typical data flow:
- Client sends/receives `Cluster` JSON using the API routes in `ClusterController`.
- `ClusterManager` serializes/deserializes `Cluster` instances for storage in SQLite.
- `UsageStatisticsCluster` optionally records per-endpoint call counts to a JSON file under `../home/history.json`.

## Building and Testing

- Build the project:
  - `dotnet build Model/Model.csproj -c Debug`
- Run solution tests (includes consumers of the Model):
  - `dotnet test`

## Versioning and Compatibility

- Targets .NET 8.0 and uses nullable reference types.
- Models are designed as plain POCOs for easy JSON serialization with `System.Text.Json`.

---
If you need additional examples (e.g., setting `GaussianDrillingProperty` values) or guidance on interacting with the REST API using these models, let us know and we can expand this README.
