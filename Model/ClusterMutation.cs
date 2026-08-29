using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Cluster.Model;

/// <summary>Stable error envelope for Cluster and locally owned catalog mutations.</summary>
public sealed class ClusterMutationErrorEnvelope
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<ClusterMutationError> Errors { get; set; } = [];
}

/// <summary>Identifies invalid input, an active dependent reference, or a stale concurrency token.</summary>
public sealed class ClusterMutationError
{
    public string Property { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<Guid> ReferencingClusterIDs { get; set; } = [];
}
