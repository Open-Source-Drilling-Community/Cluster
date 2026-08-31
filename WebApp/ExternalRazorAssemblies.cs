using System.Reflection;

namespace OSDC.Drilling.Cluster.WebApp;

public static class ExternalRazorAssemblies
{
    public static IReadOnlyList<Assembly> All { get; } =
    [
        typeof(OSDC.Drilling.Cluster.WebPages.ClusterMain).Assembly,
        typeof(OSDC.Drilling.Field.WebPages.Field).Assembly,
        typeof(OSDC.Drilling.Rig.WebPages.Pages.RigMain).Assembly,
    ];
}
