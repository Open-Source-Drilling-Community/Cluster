using System;
using System.Collections.Generic;
using OSDC.DotnetLibraries.Drilling.DrillingProperties;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;

namespace OSDC.Drilling.Cluster.Model;

/// <summary>Slot location and selection context, omitting feature assignment collections.</summary>
[Semantic(Concepts.WellSlot)]
public sealed class SlotLight
{
    public Guid ID { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? CreationDate { get; set; }
    public DateTimeOffset? LastModificationDate { get; set; }
    public GaussianDrillingProperty? Latitude { get; set; }
    public GaussianDrillingProperty? Longitude { get; set; }
}

public sealed class SlotDiscoveryPage
{
    public List<SlotLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}
