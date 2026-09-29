using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.DotnetLibraries.General.ResourceClassification;

namespace OSDC.Drilling.Cluster.Model;

/// <summary>ClusterIdentity contract using the shared resource classification implementation.</summary>
[Semantic(Concepts.IdentityDefinition)]
public class ClusterIdentity : IdentityDefinition
{
}
