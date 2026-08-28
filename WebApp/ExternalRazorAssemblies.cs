using System.Reflection;

namespace NORCE.Drilling.Cluster.WebApp;

public static class ExternalRazorAssemblies
{
    public static IReadOnlyList<Assembly> All { get; } =
    [
        typeof(NORCE.Drilling.Cluster.WebPages.ClusterMain).Assembly,
        typeof(OSDC.Drilling.Field.WebPages.Field).Assembly,
    ];
}
