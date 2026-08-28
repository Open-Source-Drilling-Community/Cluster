using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using NORCE.Drilling.Cluster.Service.Mcp;
using NORCE.Drilling.Cluster.Service.Mcp.Tools;

namespace NORCE.Drilling.Cluster.ServiceTest;

[TestFixture]
public sealed class McpToolRegistrationTests
{
    private ServiceProvider _provider = null!;
    private IReadOnlyDictionary<string, IMcpTool> _tools = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLegacyMcpTool<PingMcpTool>();
        services.AddClusterRestMcpTools();
        _provider = services.BuildServiceProvider();
        _tools = _provider.GetServices<IMcpTool>().ToDictionary(tool => tool.Name);
    }

    [TearDown]
    public void TearDown() => _provider.Dispose();

    [Test]
    public void Protocol_tool_names_are_valid_and_unique()
    {
        string[] names = _provider.GetServices<McpServerTool>().Select(tool => tool.ProtocolTool.Name).ToArray();
        Assert.That(names, Has.Length.EqualTo(_tools.Count));
        Assert.That(names, Is.Unique);
        Assert.That(names.All(name => !name.Contains('.')), Is.True);
    }

    [Test]
    public void Rest_tools_have_detailed_descriptions_and_explicit_schemas()
    {
        foreach (IMcpTool tool in _tools.Values.Where(tool => tool.Name != "ping"))
        {
            Assert.That(tool.Description, Has.Length.GreaterThan(100), tool.Name);
            Assert.That(tool.InputSchema, Is.TypeOf<JsonObject>(), tool.Name);
        }
    }

    [TestCase("cluster_get_all_ids")]
    [TestCase("cluster_get_all_meta_info")]
    [TestCase("cluster_get_all")]
    [TestCase("cluster_get_all_light")]
    [TestCase("cluster_identity_get_all")]
    [TestCase("cluster_feature_category_get_all")]
    [TestCase("slot_feature_category_get_all")]
    public void Parameterless_tools_publish_an_explicit_empty_object_schema(string toolName)
    {
        JsonObject schema = RequireObject(_tools[toolName].InputSchema);
        Assert.That(schema["type"]?.GetValue<string>(), Is.EqualTo("object"));
        Assert.That(schema["additionalProperties"]?.GetValue<bool>(), Is.False);
    }

    [Test]
    public void Cluster_create_schema_describes_complete_payload_and_coordinate_contract()
    {
        JsonObject root = RequireObject(_tools["cluster_create"].InputSchema);
        Assert.That(RequiredNames(root), Is.EquivalentTo(new[] { "cluster" }));

        JsonObject cluster = Property(root, "cluster");
        Assert.That(RequiredNames(cluster), Does.Contain("MetaInfo"));
        Assert.That(PropertyNames(cluster), Is.EquivalentTo(new[]
        {
            "MetaInfo", "Name", "Description", "CreationDate", "LastModificationDate",
            "FieldID", "IsSingleWell", "RigID", "IsFixedPlatform",
            "ClusterIdentityAssignments", "ClusterFeatureAssignments", "ReferencePoint",
            "GroundMudLineDepth", "TopWaterDepth", "Slots"
        }));
        Assert.That(cluster["additionalProperties"]?.GetValue<bool>(), Is.False);
        Assert.That(Property(Property(cluster, "MetaInfo"), "ID")["format"]?.GetValue<string>(), Is.EqualTo("uuid"));
        Assert.That(Property(cluster, "FieldID")["format"]?.GetValue<string>(), Is.EqualTo("uuid"));

        JsonObject referencePoint = Property(cluster, "ReferencePoint");
        Assert.Multiple(() =>
        {
            Assert.That(referencePoint["description"]?.GetValue<string>(), Does.Contain("WGS84"));
            Assert.That(referencePoint["description"]?.GetValue<string>(), Does.Contain("SI"));
            Assert.That(Property(cluster, "GroundMudLineDepth")["description"]?.GetValue<string>(), Does.Contain("meters (SI)"));
            Assert.That(Property(cluster, "GroundMudLineDepth")["description"]?.GetValue<string>(), Does.Contain("WGS84 vertical datum"));
        });

        JsonObject slots = Property(cluster, "Slots");
        JsonObject slot = RequireObject(slots["additionalProperties"]);
        Assert.That(Property(slot, "Latitude")["description"]?.GetValue<string>(), Does.Contain("radians"));
    }

    [TestCase("cluster_feature_category_create", "clusterFeatureCategory")]
    [TestCase("cluster_identity_create", "clusterIdentity")]
    [TestCase("slot_feature_category_create", "slotFeatureCategory")]
    public void Definition_create_tools_publish_complete_domain_schemas(string toolName, string bodyName)
    {
        JsonObject root = RequireObject(_tools[toolName].InputSchema);
        JsonObject body = Property(root, bodyName);
        Assert.That(RequiredNames(body), Does.Contain("MetaInfo"));
        Assert.That(Property(Property(body, "MetaInfo"), "ID")["format"]?.GetValue<string>(), Is.EqualTo("uuid"));
        Assert.That(body["additionalProperties"]?.GetValue<bool>(), Is.False);
    }

    [TestCase("cluster_update_by_id", "cluster")]
    [TestCase("cluster_feature_category_update_by_id", "clusterFeatureCategory")]
    [TestCase("cluster_identity_update_by_id", "clusterIdentity")]
    [TestCase("slot_feature_category_update_by_id", "slotFeatureCategory")]
    public void Update_schemas_require_matching_top_level_identifier(string toolName, string bodyName)
    {
        JsonObject root = RequireObject(_tools[toolName].InputSchema);
        Assert.That(RequiredNames(root), Is.EquivalentTo(new[] { bodyName, "id" }));
        Assert.That(Property(root, "id")["format"]?.GetValue<string>(), Is.EqualTo("uuid"));
        Assert.That(Property(root, "id")["description"]?.GetValue<string>(), Does.Contain($"{bodyName}.MetaInfo.ID"));
    }

    private static JsonObject RequireObject(JsonNode? node)
    {
        Assert.That(node, Is.TypeOf<JsonObject>());
        return (JsonObject)node!;
    }

    private static JsonObject Property(JsonObject schema, string name) =>
        RequireObject(RequireObject(schema["properties"])[name]);

    private static string[] PropertyNames(JsonObject schema) =>
        RequireObject(schema["properties"]).Select(property => property.Key).ToArray();

    private static string[] RequiredNames(JsonObject schema)
    {
        Assert.That(schema["required"], Is.TypeOf<JsonArray>());
        return ((JsonArray)schema["required"]!).Select(node => node!.GetValue<string>()).ToArray();
    }
}
