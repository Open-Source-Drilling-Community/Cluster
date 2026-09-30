using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.DotnetLibraries.General.ResourceClassification;

namespace OSDC.Drilling.Cluster.Model;

/// <summary>ClusterIdentityAssignment contract using the shared resource classification implementation.</summary>
[Semantic(Concepts.IdentityAssignment)]
public class ClusterIdentityAssignment : IdentityAssignment
{
}
