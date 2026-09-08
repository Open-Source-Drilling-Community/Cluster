using ClusterModelShared = OSDC.Drilling.Cluster.ModelShared;

namespace OSDC.Drilling.Cluster.WebPages;

internal static class ClusterVisualizationOverlayUtils
{
    public static double ResolveSlotDepthWgs84(
        ClusterModelShared.Cluster cluster,
        double zeroMeanSeaLevelDepthWgs84) =>
        cluster.ReferencePoint?.TVD ??
        cluster.GroundMudLineDepth?.GaussianValue?.Mean ??
        zeroMeanSeaLevelDepthWgs84;

    public static bool TryGetSlotNorthEast(
        ClusterModelShared.Slot slot,
        out double north,
        out double east)
    {
        north = default;
        east = default;
        if (slot.Latitude?.GaussianValue?.Mean is not double latitude ||
            slot.Longitude?.GaussianValue?.Mean is not double longitude)
        {
            return false;
        }

        north = MeridianDistance(latitude);
        east = ParallelRadius(latitude) * longitude;
        return double.IsFinite(north) && double.IsFinite(east);
    }

    private static double MeridianDistance(double latitude)
    {
        if (Math.Abs(latitude) < 1e-15)
        {
            return 0.0;
        }

        const int intervals = 200;
        double step = latitude / intervals;
        double sum = 0.0;
        for (int i = 0; i <= intervals; i++)
        {
            double phi = i * step;
            double weight = i == 0 || i == intervals ? 1.0 : i % 2 == 0 ? 2.0 : 4.0;
            sum += weight * MeridianRadiusOfCurvature(phi);
        }

        return sum * step / 3.0;
    }

    private static double MeridianRadiusOfCurvature(double latitude)
    {
        double sinLatitude = Math.Sin(latitude);
        return EarthSemiMajorAxisWgs84 * (1.0 - EarthFirstEccentricitySquaredWgs84) /
            Math.Pow(1.0 - EarthFirstEccentricitySquaredWgs84 * sinLatitude * sinLatitude, 1.5);
    }

    private static double ParallelRadius(double latitude)
    {
        double sinLatitude = Math.Sin(latitude);
        return EarthSemiMajorAxisWgs84 * Math.Cos(latitude) /
            Math.Sqrt(1.0 - EarthFirstEccentricitySquaredWgs84 * sinLatitude * sinLatitude);
    }

    private const double EarthSemiMajorAxisWgs84 = 6378137.0;
    private const double EarthFirstEccentricitySquaredWgs84 = 6.6943799901413165e-3;
}
