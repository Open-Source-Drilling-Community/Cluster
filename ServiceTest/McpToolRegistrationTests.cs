using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using OSDC.Drilling.Cluster.Service.Mcp;
using OSDC.Drilling.Cluster.Service.Mcp.Tools;

namespace OSDC.Drilling.Cluster.ServiceTest;

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
        Assert.That(names, Does.Not.Contain("cluster_usage_statistics_get"));
    }

    [Test]
    public void Every_tool_publishes_a_precise_output_schema_and_behavior_metadata()
    {
        foreach (IMcpTool tool in _tools.Values)
        {
            JsonObject output = RequireObject(tool.OutputSchema);
            Assert.That(output["type"]?.GetValue<string>(), Is.EqualTo("object"), tool.Name);
            Assert.That(output["additionalProperties"]?.GetValue<bool>(), Is.False, tool.Name);
            Assert.That(tool.Behavior.Title, Is.Not.Empty, tool.Name);

            if (tool.Name == "ping") continue;
            JsonObject properties = RequireObject(output["properties"]);
            Assert.That(properties, Does.ContainKey("status"), tool.Name);
            if (properties["data"] is JsonObject data)
                Assert.That(data.Count, Is.GreaterThan(0), $"{tool.Name} has an unconstrained data schema");
        }
    }

    [TestCase("cluster_get_by_id")]
    [TestCase("cluster_get_all")]
    [TestCase("cluster_identity_get_all")]
    [TestCase("cluster_feature_category_get_by_id")]
    public void Read_tools_are_read_only_idempotent_and_closed_world(string toolName)
    {
        McpToolBehavior behavior = _tools[toolName].Behavior;
        Assert.Multiple(() =>
        {
            Assert.That(behavior.ReadOnlyHint, Is.True);
            Assert.That(behavior.DestructiveHint, Is.False);
            Assert.That(behavior.IdempotentHint, Is.True);
            Assert.That(behavior.OpenWorldHint, Is.False);
        });
    }

    [TestCase("cluster_update_by_id")]
    [TestCase("cluster_delete_by_id")]
    [TestCase("cluster_identity_update_by_id")]
    [TestCase("slot_feature_category_delete_by_id")]
    public void Update_and_delete_tools_are_destructive_idempotent_and_closed_world(string toolName)
    {
        McpToolBehavior behavior = _tools[toolName].Behavior;
        Assert.Multiple(() =>
        {
            Assert.That(behavior.ReadOnlyHint, Is.False);
            Assert.That(behavior.DestructiveHint, Is.True);
            Assert.That(behavior.IdempotentHint, Is.True);
            Assert.That(behavior.OpenWorldHint, Is.False);
        });
    }

    [Test]
    public void Batch_transfer_tools_publish_portable_contracts_and_annotations()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_tools, Does.ContainKey("cluster_batch_export"));
            Assert.That(_tools, Does.ContainKey("cluster_batch_restore"));
            Assert.That(_tools["cluster_batch_export"].Behavior.ReadOnlyHint, Is.True);
            Assert.That(_tools["cluster_batch_restore"].Behavior.DestructiveHint, Is.True);
            Assert.That(_tools["cluster_batch_restore"].Behavior.OpenWorldHint, Is.True);
            Assert.That(_tools["cluster_batch_export"].OutputSchema, Is.TypeOf<JsonObject>());
            Assert.That(_tools["cluster_batch_restore"].OutputSchema, Is.TypeOf<JsonObject>());
        });

        JsonObject restoreRequest = Property(RequireObject(_tools["cluster_batch_restore"].InputSchema), "request");
        JsonObject document = Property(restoreRequest, "Document");
        Assert.Multiple(() =>
        {
            Assert.That(RequiredNames(restoreRequest), Is.EquivalentTo(new[] { "ConflictPolicy", "CatalogPolicy", "Document" }));
            Assert.That(RequiredNames(document), Does.Contain("CatalogDependencies"));
            Assert.That(RequiredNames(document), Does.Contain("ExternalReferences"));
        });
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

    [Test]
    public void Cluster_read_outputs_distinguish_complete_and_lightweight_resources()
    {
        JsonObject completeData = Property(RequireObject(_tools["cluster_get_all"].OutputSchema), "data");
        JsonObject completeItem = RequireObject(completeData["items"]);
        JsonObject lightData = Property(RequireObject(_tools["cluster_get_all_light"].OutputSchema), "data");
        JsonObject lightItem = RequireObject(lightData["items"]);
        Assert.Multiple(() =>
        {
            Assert.That(PropertyNames(completeItem), Does.Contain("Slots"));
            Assert.That(PropertyNames(completeItem), Does.Contain("ClusterIdentityAssignments"));
            Assert.That(PropertyNames(lightItem), Does.Not.Contain("Slots"));
            Assert.That(PropertyNames(lightItem), Does.Not.Contain("ClusterIdentityAssignments"));
        });
    }

    [TestCase("cluster_create")]
    [TestCase("cluster_update_by_id")]
    [TestCase("cluster_delete_by_id")]
    [TestCase("cluster_identity_create")]
    public void Mutations_that_return_no_resource_publish_status_only_success_schema(string toolName)
    {
        JsonObject output = RequireObject(_tools[toolName].OutputSchema);
        Assert.That(PropertyNames(output), Is.EquivalentTo(new[] { "status" }));
        Assert.That(RequiredNames(output), Is.EquivalentTo(new[] { "status" }));
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
