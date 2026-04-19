using NORCE.Drilling.Cluster.ModelShared;
using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace NORCE.Drilling.Cluster.WebPages;

public class ClusterAPIUtils : APIUtils, IClusterAPIUtils
{
    public ClusterAPIUtils(IClusterWebPagesConfiguration configuration)
    {
        HostNameCluster = Require(configuration.ClusterHostURL, nameof(configuration.ClusterHostURL));
        HttpClientCluster = SetHttpClient(HostNameCluster, HostBasePathCluster);
        ClientCluster = new Client(HttpClientCluster.BaseAddress!.ToString(), HttpClientCluster);

        HostNameField = Require(configuration.FieldHostURL, nameof(configuration.FieldHostURL));
        HttpClientField = SetHttpClient(HostNameField, HostBasePathField);
        ClientField = new Client(HttpClientField.BaseAddress!.ToString(), HttpClientField);

        HostNameRig = string.IsNullOrWhiteSpace(configuration.RigHostURL) ? HostNameCluster : configuration.RigHostURL;
        HttpClientRig = SetHttpClient(HostNameRig, HostBasePathRig);
        ClientRig = new Client(HttpClientRig.BaseAddress!.ToString(), HttpClientRig);

        HostNameTrajectory = string.IsNullOrWhiteSpace(configuration.TrajectoryHostURL) ? HostNameCluster : configuration.TrajectoryHostURL;
        HttpClientTrajectory = SetHttpClient(HostNameTrajectory, HostBasePathTrajectory);
        ClientTrajectory = new Client(HttpClientTrajectory.BaseAddress!.ToString(), HttpClientTrajectory);

        HostNameUnitConversion = Require(configuration.UnitConversionHostURL, nameof(configuration.UnitConversionHostURL));
    }

    private static string Require(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Configuration value '{propertyName}' must be assigned before WebPages is used.");
        }

        return value;
    }

    public string HostNameCluster { get; }
    public string HostBasePathCluster { get; } = "Cluster/api/";
    public HttpClient HttpClientCluster { get; }
    public Client ClientCluster { get; }

    public string HostNameField { get; }
    public string HostBasePathField { get; } = "Field/api/";
    public HttpClient HttpClientField { get; }
    public Client ClientField { get; }

    public string HostNameRig { get; }
    public string HostBasePathRig { get; } = "Rig/api/";
    public HttpClient HttpClientRig { get; }
    public Client ClientRig { get; }

    public string HostNameTrajectory { get; }
    public string HostBasePathTrajectory { get; } = "Trajectory/api/";
    public HttpClient HttpClientTrajectory { get; }
    public Client ClientTrajectory { get; }

    public string HostNameUnitConversion { get; }
    public string HostBasePathUnitConversion { get; } = "UnitConversion/api/";

    public double EarthRadiusWGS84 { get; } = 6378137.0;
}
