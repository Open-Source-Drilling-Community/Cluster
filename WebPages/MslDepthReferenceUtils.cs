using ModelShared = OSDC.Drilling.Cluster.ModelShared;
using OSDC.Drilling.Cluster.ModelShared;

namespace OSDC.Drilling.Cluster.WebPages;

public static class MslDepthReferenceUtils
{
    public static Task<double?> ResolveMeanSeaLevelDepthReferenceAsync(IClusterAPIUtils api, ModelShared.Cluster? cluster) =>
        ResolveMeanSeaLevelDepthReferenceFromFullClusterAsync(api, cluster);

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
        return ToMeanSeaLevelDepthReference(
            response.Samples?.FirstOrDefault()?.Wgs84EllipsoidalDepth);
    }

    public static double? ToMeanSeaLevelDepthReference(double? meanSeaLevelWgs84Depth) =>
        -meanSeaLevelWgs84Depth;

    private static Task<double?> ResolveMeanSeaLevelDepthReferenceFromFullClusterAsync(
        IClusterAPIUtils api,
        ModelShared.Cluster? cluster)
    {
        if (cluster?.ReferencePoint?.Latitude is double latitude &&
            cluster.ReferencePoint.Longitude is double longitude)
        {
            return CalculateMeanSeaLevelDepthReferenceAsync(api, latitude, longitude);
        }

        List<(double Latitude, double Longitude)> positions = (cluster?.Slots?.Values ?? [])
            .Where(slot => slot.Latitude?.GaussianValue?.Mean != null && slot.Longitude?.GaussianValue?.Mean != null)
            .Select(slot => (
                slot.Latitude!.GaussianValue!.Mean!.Value,
                slot.Longitude!.GaussianValue!.Mean!.Value))
            .ToList();
        return positions.Count == 0
            ? Task.FromResult<double?>(null)
            : CalculateMeanSeaLevelDepthReferenceAsync(
                api,
                positions.Average(position => position.Latitude),
                positions.Average(position => position.Longitude));
    }
}
