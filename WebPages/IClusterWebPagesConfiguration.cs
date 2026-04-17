using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace NORCE.Drilling.Cluster.WebPages;

public interface IClusterWebPagesConfiguration :
    IClusterHostURL,
    IFieldHostURL,
    IRigHostURL,
    IUnitConversionHostURL
{
}
