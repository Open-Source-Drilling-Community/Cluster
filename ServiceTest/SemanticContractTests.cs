using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using NUnit.Framework;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.DotnetLibraries.General.Math;
using OSDC.Drilling.Cluster.Model;
using OSDC.Drilling.Cluster.Service;
using OSDC.Drilling.Cluster.Service.Mcp;
using OSDC.Drilling.Cluster.Service.Mcp.Tools;
using Swashbuckle.AspNetCore.SwaggerGen;
using Model = OSDC.Drilling.Cluster.Model;

namespace OSDC.Drilling.Cluster.SemanticTests;

public class SemanticContractTests
{
    private const string Extension = SemanticMetadata.ExtensionName;

    private static JsonObject Rest(Type type)
    {
        var options = new SchemaGeneratorOptions { SchemaIdSelector = t => t.FullName! };
        options.SchemaFilters.Add(new SemanticSchemaFilter());
        var generator = new SchemaGenerator(options, new JsonSerializerDataContractResolver(new JsonSerializerOptions()));
        var repository = new SchemaRepository();
        generator.GenerateSchema(type, repository);
        using var text = new StringWriter();
        var writer = new OpenApiJsonWriter(text);
        repository.Schemas[type.FullName!].SerializeAsV3(writer);
        writer.Flush();
        return JsonNode.Parse(text.ToString())!.AsObject();
    }

    private static JsonObject Mcp(string name, bool output = true)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddClusterRestMcpTools();
        using var provider = services.BuildServiceProvider();
        var tool = provider.GetServices<IMcpTool>().Single(t => t.Name == name);
        return (output ? tool.OutputSchema : tool.InputSchema).AsObject();
    }

    [Test]
    public void Mcp_inputs_declare_generic_resource_operation_roles()
    {
        Assert.That(Mcp("cluster_get_all", false)[Extension]!["role"]!.GetValue<string>(), Is.EqualTo(Concepts.ResourceCollectionRetrieval));
        Assert.That(Mcp("cluster_get_by_id", false)[Extension]!["role"]!.GetValue<string>(), Is.EqualTo(Concepts.ResourceRetrieval));
        Assert.That(Mcp("cluster_create", false)[Extension]!["role"]!.GetValue<string>(), Is.EqualTo(Concepts.ResourceCreation));
        Assert.That(Mcp("cluster_update_by_id", false)[Extension]!["role"]!.GetValue<string>(), Is.EqualTo(Concepts.ResourceReplacement));
        Assert.That(Mcp("cluster_delete_by_id", false)[Extension]!["role"]!.GetValue<string>(), Is.EqualTo(Concepts.ResourceDeletion));
    }

    [Test]
    public void ResourceAndInheritedClassificationsPublishCuratedCatalogue()
    {
        var rest = Rest(typeof(Model.Cluster));
        var mcp = Mcp("cluster_get_by_id")["properties"]!["data"]!;
        Assert.That(rest[Extension]!["catalogueVersion"]!.GetValue<string>(), Is.EqualTo("0.18.0"));
        Assert.That(JsonNode.DeepEquals(rest[Extension], mcp[Extension]), Is.True);
        var category = Rest(typeof(Model.ClusterFeatureCategory));
        Assert.That(category[Extension]!["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.FeatureCategory));
        Assert.That(category["properties"]!["IsExclusive"]![Extension]!["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.CategoryExclusivity));
        var assignment = Rest(typeof(Model.ClusterFeatureAssignment));
        Assert.That(assignment["properties"]!["FromDate"]![Extension]!["role"]!.GetValue<string>(), Is.EqualTo(Concepts.ValidityStart));
        Assert.That(assignment["properties"]!["ToDate"]![Extension]!["role"]!.GetValue<string>(), Is.EqualTo(Concepts.ValidityEnd));
        Assert.That(assignment["properties"]!["FeatureOptionID"]![Extension]!["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.ResourceIdentifier));
    }

    [Test]
    public void ArcCoordinatesAreNotProjectionCoordinatesAndReferenceRoleSurvivesRecursion()
    {
        var rest = Rest(typeof(Point3DGlobalCoordinates));
        var mcp = Mcp("cluster_get_by_id")["properties"]!["data"]!["properties"]!["ReferencePoint"]!;
        Assert.That(mcp[Extension]!["role"]!.GetValue<string>(), Is.EqualTo(Concepts.ReferenceLocation));
        foreach (string name in new[] { "X", "Y", "Z", "RiemannianNorth", "RiemannianEast", "Latitude", "Longitude", "TVD" })
            Assert.That(JsonNode.DeepEquals(rest["properties"]![name]![Extension], mcp["properties"]![name]![Extension]), Is.True, name);
        Assert.That(rest["properties"]!["X"]![Extension]!["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.RiemannianNorth));
        Assert.That(rest["properties"]!["Y"]![Extension]!["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.RiemannianEast));
        Assert.That(rest["properties"]!["Z"]![Extension]!["physicalQuantity"]!["name"]!.GetValue<string>(), Is.EqualTo("DepthDrilling"));
    }

    [Test]
    public void EveryPublishedBindingResolvesToReviewedVocabulary()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddClusterRestMcpTools();
        using var provider = services.BuildServiceProvider();
        int count = 0;
        void Check(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                if (obj["catalogue"] != null)
                {
                    if (obj["concept"] != null)
                    {
                        count++;
                        Assert.That(obj["catalogueVersion"]!.GetValue<string>(), Is.EqualTo("0.18.0"));
                        Assert.That(obj["curationStatus"]!.GetValue<string>(), Is.EqualTo("Reviewed"));
                        Assert.That(SemanticCatalogue.Default.Get(obj["concept"]!.GetValue<string>()).Status, Is.EqualTo(CurationStatus.Reviewed));
                    }
                }
                foreach (var child in obj) Check(child.Value);
            }
            else if (node is JsonArray array) foreach (var child in array) Check(child);
        }
        foreach (var tool in provider.GetServices<IMcpTool>()) { Check(tool.InputSchema); Check(tool.OutputSchema); }
        Assert.That(count, Is.GreaterThan(100));
    }

    [Test]
    public void GaussianBindingsAreContextualAndIdenticalInRestAndMcp()
    {
        var cluster = Rest(typeof(Model.Cluster));
        var slot = Rest(typeof(Model.Slot));
        var mcp = Mcp("cluster_get_by_id")["properties"]!["data"]!["properties"]!;
        foreach (var (name, isSlot, quantity, sigmaQuantity) in new[]
        {
            ("GroundMudLineDepth", false, "DepthDrilling", "LengthStandard"),
            ("TopWaterDepth", false, "DepthDrilling", "LengthStandard"),
            ("Latitude", true, "PlaneAngleGeodesic", "PlaneAngleGeodesic"),
            ("Longitude", true, "PlaneAngleGeodesic", "PlaneAngleGeodesic")
        })
        {
            var rest = (isSlot ? slot : cluster)["properties"]![name]!;
            var wire = isSlot ? mcp["Slots"]!["additionalProperties"]!["properties"]![name]! : mcp[name]!;
            var bindings = rest[ProviderSemantics.NestedBindingsExtension]!;
            Assert.That(JsonNode.DeepEquals(bindings, wire[ProviderSemantics.NestedBindingsExtension]), Is.True, name);
            Assert.That(rest[Extension]!["physicalQuantity"], Is.Null, "Gaussian wrappers have no universal quantity");
            Assert.That(bindings["/GaussianValue/Mean"]!["physicalQuantity"]!["name"]!.GetValue<string>(), Is.EqualTo(quantity));
            Assert.That(bindings["/GaussianValue/StandardDeviation"]!["physicalQuantity"]!["name"]!.GetValue<string>(), Is.EqualTo(sigmaQuantity));
            Assert.That(bindings["/GaussianValue/StandardDeviation"]!["reference"], Is.Null, "Uncertainty is not offset by a datum");
            foreach (var scalar in new[] { "Mean", "StandardDeviation", "MinValue", "MaxValue" })
                Assert.That(JsonNode.DeepEquals(bindings["/GaussianValue/" + scalar], wire["properties"]!["GaussianValue"]!["properties"]![scalar]![Extension]), Is.True, name + "." + scalar);
        }
    }
}
