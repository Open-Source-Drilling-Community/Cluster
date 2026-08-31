using OSDC.Drilling.Cluster.ModelShared;

namespace ServiceTest;

[TestFixture]
public sealed class RigDependencyContractTests
{
    [Test]
    public void Generated_client_contains_current_rig_read_write_and_backup_contracts()
    {
        string[] clientMethods = typeof(Client).GetMethods().Select(method => method.Name).ToArray();

        Assert.That(clientMethods, Does.Contain("PostRigAsync"));
        Assert.That(clientMethods, Does.Contain("PutRigByIdAsync"));
        Assert.That(clientMethods, Does.Contain("GetAllRigLightAsync"));
        Assert.That(clientMethods, Does.Contain("BatchExportRigsAsync"));
        Assert.That(clientMethods, Does.Contain("BatchRestoreRigsAsync"));
        Assert.That(clientMethods, Does.Contain("GetAllRigFeatureCategoryAsync"));
        Assert.That(typeof(RigReadResponse).GetProperty("Photos"), Is.Not.Null);
        Assert.That(typeof(RigBatchExportDocument).GetProperty("Rigs"), Is.Not.Null);
    }

    [Test]
    public void Rig_light_contains_current_classification_fields()
    {
        string[] properties = typeof(RigLight).GetProperties().Select(property => property.Name).ToArray();

        Assert.That(properties, Does.Contain("RigType"));
        Assert.That(properties, Does.Contain("OperatingEnvironment"));
        Assert.That(properties, Does.Contain("MobilityType"));
    }
}
