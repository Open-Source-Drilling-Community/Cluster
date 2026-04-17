using OSDC.UnitConversion.DrillingRazorMudComponents;

namespace NORCE.Drilling.Cluster.WebPages;

public class CartographicReference : ICartographicGridPositionReferenceSource
{
    public double? CartographicGridNorthPositionReference { get; set; }
    public double? CartographicGridEastPositionReference { get; set; }
}

public class ClusterReference : IClusterPositionReferenceSource
{
    public double? ClusterNorthPositionReference { get; set; }
    public double? ClusterEastPositionReference { get; set; }
}
