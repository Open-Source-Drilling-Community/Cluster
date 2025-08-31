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
        public static string? DepthReferenceName { get; set; }
        public static string? PositionReferenceName { get; set; }
        public static string? AzimuthReferenceName { get; set; }
        public static string? PressureReferenceName { get; set; }
        public static string? DateReferenceName { get; set; }
    }

    public static void UpdateUnitSystemName(string val)
    {
        UnitAndReferenceParameters.UnitSystemName = (string)val;
    }

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
}