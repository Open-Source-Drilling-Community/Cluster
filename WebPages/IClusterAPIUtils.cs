using NORCE.Drilling.Cluster.ModelShared;

namespace NORCE.Drilling.Cluster.WebPages;

public interface IClusterAPIUtils
{
    string HostNameCluster { get; }
    string HostBasePathCluster { get; }
    HttpClient HttpClientCluster { get; }
    Client ClientCluster { get; }

    string HostNameField { get; }
    string HostBasePathField { get; }
    HttpClient HttpClientField { get; }
    Client ClientField { get; }

    string HostNameRig { get; }
    string HostBasePathRig { get; }
    HttpClient HttpClientRig { get; }
    Client ClientRig { get; }

    string HostNameTrajectory { get; }
    string HostBasePathTrajectory { get; }
    HttpClient HttpClientTrajectory { get; }
    Client ClientTrajectory { get; }

    string HostNameUnitConversion { get; }
    string HostBasePathUnitConversion { get; }

    double EarthRadiusWGS84 { get; }
}
