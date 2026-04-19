using NORCE.Drilling.Cluster.WebPages;

namespace NORCE.Drilling.Cluster.WebApp;

public class WebPagesHostConfiguration :
    IClusterWebPagesConfiguration,
    NORCE.Drilling.Field.WebPages.IFieldWebPagesConfiguration,
    NORCE.Drilling.CartographicProjection.WebPages.ICartographicProjectionWebPagesConfiguration,
    NORCE.Drilling.GeodeticDatum.WebPages.IGeodeticDatumWebPagesConfiguration
{
    public string ClusterHostURL { get; set; } = string.Empty;
    public string FieldHostURL { get; set; } = string.Empty;
    public string RigHostURL { get; set; } = string.Empty;
    public string TrajectoryHostURL { get; set; } = string.Empty;
    public string CartographicProjectionHostURL { get; set; } = string.Empty;
    public string GeodeticDatumHostURL { get; set; } = string.Empty;
    public string UnitConversionHostURL { get; set; } = string.Empty;
}
