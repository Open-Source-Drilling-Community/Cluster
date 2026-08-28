using ModelShared = NORCE.Drilling.Cluster.ModelShared;
using NORCE.Drilling.Cluster.ModelShared;

namespace NORCE.Drilling.Cluster.WebPages;

public static class MslDepthReferenceUtils
{
    public static Task<double?> ResolveMeanSeaLevelDepthReferenceAsync(IClusterAPIUtils api, ModelShared.Cluster? cluster) =>
        CalculateMeanSeaLevelDepthReferenceAsync(
            api,
            cluster?.ReferencePoint?.Latitude,
            cluster?.ReferencePoint?.Longitude);

    public static Task<double?> ResolveMeanSeaLevelDepthReferenceAsync(IClusterAPIUtils api, ModelShared.ClusterLight? cluster) =>
        CalculateMeanSeaLevelDepthReferenceAsync(
            api,
            cluster?.ReferencePoint?.Latitude,
            cluster?.ReferencePoint?.Longitude);

    public static async Task<double?> CalculateMeanSeaLevelDepthReferenceAsync(
        IClusterAPIUtils api,
        double? latitude,
        double? longitude)
    {
        if (latitude == null || longitude == null)
        {
            return null;
        }

        MeanSeaLevelToWgs84Request request = new()
        {
            Positions =
            [
                new EarthVerticalDatumPosition
                {
                    Latitude = latitude.Value,
                    Longitude = longitude.Value,
                    MeanSeaLevelDepth = 0
                }
            ]
        };
        MeanSeaLevelToWgs84Response response =
            await api.ClientEarthVerticalDatum.ConvertMeanSeaLevelToWgs84Async(request);
        return response.Samples?.FirstOrDefault()?.Wgs84EllipsoidalDepth;
    }
}
