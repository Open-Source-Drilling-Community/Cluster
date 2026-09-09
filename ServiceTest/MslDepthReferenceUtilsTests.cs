extern alias ClusterWebPages;

using MslDepthReferenceUtils = ClusterWebPages::OSDC.Drilling.Cluster.WebPages.MslDepthReferenceUtils;

namespace OSDC.Drilling.Cluster.ServiceTest;

public class MslDepthReferenceUtilsTests
{
    [Test]
    public void ToMeanSeaLevelDepthReference_UsesOppositeOfDatumWgs84Depth()
    {
        const double meanSeaLevelWgs84Depth = -43.4457049005008;

        double? reference = MslDepthReferenceUtils.ToMeanSeaLevelDepthReference(
            meanSeaLevelWgs84Depth);

        Assert.That(reference, Is.EqualTo(43.4457049005008).Within(1e-12));
    }

    [Test]
    public void ToMeanSeaLevelDepthReference_PreservesMissingDatum()
    {
        Assert.That(MslDepthReferenceUtils.ToMeanSeaLevelDepthReference(null), Is.Null);
    }
}
