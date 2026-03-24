using NORCE.Drilling.Cluster.ModelShared;
using OSDC.UnitConversion.DrillingRazorMudComponents;

public static class DataUtils
{
    // default values
    public const double DEFAULT_VALUE = 999.25;
    public static string DEFAULT_NAME_Cluster = "Default Cluster Name";
    public static string DEFAULT_DESCR_Cluster = "Default Cluster Description";
    public static string DEFAULT_NAME_Slot = "Default Slot Name";
    public static string DEFAULT_DESCR_Slot = "Default Slot Description";

    // unit management
    public static class UnitAndReferenceParameters
    {
        public static string? UnitSystemName { get; set; } = "Metric";
        public static string? DepthReferenceName { get; set; } = "WGS84";
        public static string? PositionReferenceName { get; set; }
        public static string? AzimuthReferenceName { get; set; }
        public static string? PressureReferenceName { get; set; }
        public static string? DateReferenceName { get; set; }
        public static GroundMudLineDepthReferenceSource GroundMudLineDepthReferenceSource { get; set; } = new GroundMudLineDepthReferenceSource();
        public static SeaWaterLevelDepthReferenceSource SeaWaterLevelDepthReferenceSource { get; set; } = new SeaWaterLevelDepthReferenceSource();
    }

    public static void ApplyClusterReferenceValues(Cluster? cluster)
    {
        DataUtils.UnitAndReferenceParameters.GroundMudLineDepthReferenceSource.GroundMudLineDepthReference = 0;
        DataUtils.UnitAndReferenceParameters.SeaWaterLevelDepthReferenceSource.SeaWaterLevelDepthReference = 0;
        if (cluster != null)
        {
            if (cluster.GroundMudLineDepth != null && cluster.GroundMudLineDepth.GaussianValue != null && cluster.GroundMudLineDepth.GaussianValue.Mean != null)
            {
                ApplyGroundMudLineDepthWGS84(cluster.GroundMudLineDepth.GaussianValue.Mean);
            }
            if (cluster.TopWaterDepth != null && cluster.TopWaterDepth.GaussianValue != null && cluster.TopWaterDepth.GaussianValue.Mean != null)
            {
                ApplyTopWaterDepthWGS84(cluster.TopWaterDepth.GaussianValue.Mean);
            }
        }
    }

    public static void ApplyGroundMudLineDepthWGS84(double? val)
    {
        if (val != null)
        {
            DataUtils.UnitAndReferenceParameters.GroundMudLineDepthReferenceSource.GroundMudLineDepthReference = -val;
        }
    }

    public static void ApplyTopWaterDepthWGS84(double? val)
    {
        if (val != null)
        {
            DataUtils.UnitAndReferenceParameters.SeaWaterLevelDepthReferenceSource.SeaWaterLevelDepthReference = -val;
        }
    }

    public static void UpdateUnitSystemName(string value) => UnitAndReferenceParameters.UnitSystemName = value;
    public static void UpdateDepthReferenceName(string value) => UnitAndReferenceParameters.DepthReferenceName = value;


    // units and labels
    public static readonly string ClusterSlotListLabel = "SlotList";
    public static readonly string ClusterOutputParamLabel = "ClusterOutputParam";
    public static readonly string ClusterNameLabel = "Cluster name";
    public static readonly string ClusterDescrLabel = "Cluster description";
    public static readonly string ClusterOutputParamQty = "DepthDrilling";

    public static readonly string SlotNameLabel = "Slot name";
    public static readonly string SlotParamLabel = "SlotParam";
    public static readonly string SlotParamQty = "DepthDrilling";

    public static readonly string SlotsXValuesTitle = "Easting";
    public static readonly string SlotsXValuesQty = "PositionDrilling";
    public static readonly string SlotsYValuesTitle = "Northing";
    public static readonly string SlotsYValuesQty = "PositionDrilling";
    public static readonly string DepthReferencesXValuesTitle = "Departure";
    public static readonly string DepthReferencesXValuesQty = "LengthStandard";
    public static readonly string DepthReferencesYValuesTitle = "Depth";
    public static readonly string DepthReferencesYValuesQty = "DepthDrilling";

    public class GroundMudLineDepthReferenceSource : IGroundMudLineDepthReferenceSource
    {
        public double? GroundMudLineDepthReference { get; set; } = null;
    }

    public class SeaWaterLevelDepthReferenceSource : ISeaWaterLevelDepthReferenceSource
    {
        public double? SeaWaterLevelDepthReference { get; set; } = null;

    }

}