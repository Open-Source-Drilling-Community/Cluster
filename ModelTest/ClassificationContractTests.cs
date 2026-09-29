using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.General.ResourceClassification;
using OSDC.Drilling.Cluster.Model;

namespace OSDC.Drilling.Cluster.ModelTest;

public class ClassificationContractTests
{
    private const string CategoryJson = """
        {"MetaInfo":{"ID":"080d1c26-876d-446f-8ab3-0e151e42ce36","HttpHostName":null,"HttpHostBasePath":null,"HttpEndPoint":null},
        "Name":"Type","IsExclusive":true,"HasValidityPeriod":true,
        "Options":[{"ID":"e48af852-3b03-4daa-bb52-2baf92d9c521","Name":"Option"}],
        "CreationDate":null,"LastModificationDate":null}
        """;

    [Test]
    public void IdentityKeepsItsWireShapeAndExistingInterface()
    {
        const string json = """
            {"ID":"e48af852-3b03-4daa-bb52-2baf92d9c521","IdentityID":null,"Value":"ABC"}
            """;
        var value = JsonSerializer.Deserialize<ClusterIdentityAssignment>(json)!;
        Assert.That(value, Is.InstanceOf<IIdentityAssignment>());
        Assert.That(value, Is.InstanceOf<IdentityAssignment>());
        Assert.That(JsonNode.DeepEquals(JsonNode.Parse(json), JsonSerializer.SerializeToNode(value)), Is.True);
        Assert.That(new ClusterIdentity(), Is.InstanceOf<IdentityDefinition>());
    }

    [Test]
    public void ClusterFeatureRetainsConcreteOptionsAndRoundTripsStoredJson()
    {
        var category = JsonSerializer.Deserialize<ClusterFeatureCategory>(CategoryJson)!;
        Assert.That(category, Is.InstanceOf<FeatureCategory<ClusterFeatureOption>>());
        Assert.That(category.Options!.Single(), Is.TypeOf<ClusterFeatureOption>());
        Assert.That(JsonNode.DeepEquals(JsonNode.Parse(CategoryJson), JsonSerializer.SerializeToNode(category)), Is.True);
        IFeatureCategory contract = category;
        contract.Options = [new FeatureOption { ID = Guid.NewGuid(), Name = "Other" }];
        Assert.That(category.Options!.Single(), Is.TypeOf<ClusterFeatureOption>());
        Assert.That(category.Options!.Single().Name, Is.EqualTo("Other"));
        contract.Options = null;
        Assert.That(category.Options, Is.Null);
    }

    [Test]
    public void ClusterFeatureAssignmentRetainsNullableReferencesAndValidityFields()
    {
        const string json = """
            {"ID":"e48af852-3b03-4daa-bb52-2baf92d9c521","FeatureCategoryID":null,"FeatureOptionID":null,"FromDate":null,"ToDate":null}
            """;
        var value = JsonSerializer.Deserialize<ClusterFeatureAssignment>(json)!;
        Assert.That(value, Is.InstanceOf<IFeatureAssignment>());
        Assert.That(JsonNode.DeepEquals(JsonNode.Parse(json), JsonSerializer.SerializeToNode(value)), Is.True);
    }

    [Test]
    public void SlotFeatureRetainsConcreteOptionsAndRoundTripsStoredJson()
    {
        var category = JsonSerializer.Deserialize<SlotFeatureCategory>(CategoryJson)!;
        Assert.That(category, Is.InstanceOf<FeatureCategory<SlotFeatureOption>>());
        Assert.That(category.Options!.Single(), Is.TypeOf<SlotFeatureOption>());
        Assert.That(JsonNode.DeepEquals(JsonNode.Parse(CategoryJson), JsonSerializer.SerializeToNode(category)), Is.True);
        IFeatureCategory contract = category;
        contract.Options = [new FeatureOption { ID = Guid.NewGuid(), Name = "Other" }];
        Assert.That(category.Options!.Single(), Is.TypeOf<SlotFeatureOption>());
        Assert.That(category.Options!.Single().Name, Is.EqualTo("Other"));
        contract.Options = null;
        Assert.That(category.Options, Is.Null);
    }

    [Test]
    public void SlotFeatureAssignmentRetainsNullableReferencesAndValidityFields()
    {
        const string json = """
            {"ID":"e48af852-3b03-4daa-bb52-2baf92d9c521","FeatureCategoryID":null,"FeatureOptionID":null,"FromDate":null,"ToDate":null}
            """;
        var value = JsonSerializer.Deserialize<SlotFeatureAssignment>(json)!;
        Assert.That(value, Is.InstanceOf<IFeatureAssignment>());
        Assert.That(JsonNode.DeepEquals(JsonNode.Parse(json), JsonSerializer.SerializeToNode(value)), Is.True);
    }
}
