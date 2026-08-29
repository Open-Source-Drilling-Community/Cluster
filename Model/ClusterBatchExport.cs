using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Cluster.Model;

public enum ClusterBatchExportScope
{
    Unspecified = 0,
    All = 1,
    Selected = 2
}

public sealed class ClusterBatchExportRequest
{
    /// <summary>All exports every cluster in UUID order. Selected preserves ClusterIDs order.</summary>
    public ClusterBatchExportScope Scope { get; set; }

    /// <summary>Required for Selected and forbidden for All. UUIDs must be non-empty and unique.</summary>
    public List<Guid>? ClusterIDs { get; set; }
}

/// <summary>A portable, versioned backup containing clusters and their local catalog dependency closure.</summary>
public sealed class ClusterBatchExportDocument
{
    public const string CurrentFormatIdentifier = "OSDC.Drilling.Cluster.BatchExport";
    public const int CurrentSchemaVersion = 1;

    public string FormatIdentifier { get; set; } = CurrentFormatIdentifier;
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public DateTimeOffset ExportedAtUtc { get; set; }
    public ClusterBatchCatalogDependencies CatalogDependencies { get; set; } = new();
    public ClusterBatchExternalReferences ExternalReferences { get; set; } = new();
    public List<Cluster> Clusters { get; set; } = [];
}

/// <summary>Only definitions and options referenced by the exported clusters are included.</summary>
public sealed class ClusterBatchCatalogDependencies
{
    public List<ClusterIdentity> Identities { get; set; } = [];
    public List<ClusterFeatureCategory> ClusterFeatureCategories { get; set; } = [];
    public List<SlotFeatureCategory> SlotFeatureCategories { get; set; } = [];
}

/// <summary>Names needed to validate or remap Cluster references owned by other services.</summary>
public sealed class ClusterBatchExternalReferences
{
    public List<ClusterBatchExternalReference> Fields { get; set; } = [];
    public List<ClusterBatchExternalReference> Rigs { get; set; } = [];
}

public sealed class ClusterBatchExternalReference
{
    public Guid SourceID { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class ClusterBatchErrorEnvelope
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<ClusterBatchError> Errors { get; set; } = [];
}

public sealed class ClusterBatchError
{
    /// <summary>Zero-based ClusterIDs or Clusters position, when applicable.</summary>
    public int? PositionIndex { get; set; }
    public string Property { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public enum ClusterBatchRestoreConflictPolicy
{
    Unspecified = 0,
    FailIfExists = 1,
    ReplaceExisting = 2
}

public enum ClusterBatchCatalogRestorePolicy
{
    Unspecified = 0,
    MapExisting = 1,
    MapOrCreateMissing = 2
}

public sealed class ClusterBatchRestoreRequest
{
    public ClusterBatchRestoreConflictPolicy ConflictPolicy { get; set; }
    public ClusterBatchCatalogRestorePolicy CatalogPolicy { get; set; }
    public ClusterBatchExportDocument? Document { get; set; }
}

public sealed class ClusterBatchRestoreResponse
{
    public DateTimeOffset RestoredAtUtc { get; set; }
    public int CreatedCount { get; set; }
    public int ReplacedCount { get; set; }
    public int CreatedCatalogDefinitionCount { get; set; }
    public int CreatedCatalogOptionCount { get; set; }
    public List<ClusterBatchCatalogMapping> CatalogMappings { get; set; } = [];
    public List<ClusterBatchExternalReferenceMapping> ExternalReferenceMappings { get; set; } = [];
    public List<Guid> ClusterIDs { get; set; } = [];
}

public sealed class ClusterBatchCatalogMapping
{
    public string Catalog { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid SourceID { get; set; }
    public Guid LocalID { get; set; }
    public string Resolution { get; set; } = string.Empty;
}

public sealed class ClusterBatchExternalReferenceMapping
{
    public string Resource { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid SourceID { get; set; }
    public Guid LocalID { get; set; }
    public string Resolution { get; set; } = string.Empty;
}
