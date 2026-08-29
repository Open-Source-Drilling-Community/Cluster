using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace OSDC.Drilling.Cluster.WebPages;

public interface IClusterWebPagesConfiguration :
    IClusterHostURL,
    IFieldHostURL,
    IRigHostURL,
    ITrajectoryHostURL,
    IUnitConversionHostURL
{
    string? EarthVerticalDatumHostURL { get; set; }
}
