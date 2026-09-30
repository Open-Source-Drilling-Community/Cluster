using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.DotnetLibraries.General.ResourceClassification;

namespace OSDC.Drilling.Cluster.Model;

/// <summary>SlotFeatureCategory contract using the shared resource classification implementation.</summary>
[Semantic(Concepts.FeatureCategory)]
public class SlotFeatureCategory : FeatureCategory<SlotFeatureOption>
{
}
