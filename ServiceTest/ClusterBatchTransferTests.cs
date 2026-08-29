using System.Text.Json;
using Microsoft.Data.Sqlite;
using OSDC.Drilling.Cluster.Model;
using OSDC.Drilling.Cluster.Service;
using OSDC.DotnetLibraries.General.DataManagement;
using ClusterModel = OSDC.Drilling.Cluster.Model.Cluster;

namespace OSDC.Drilling.Cluster.ServiceTest;

[TestFixture]
public sealed class ClusterBatchTransferTests
{
    [Test]
    public void Export_contains_only_referenced_local_catalog_dependencies()
    {
        Guid identityId = Guid.NewGuid(); Guid categoryId = Guid.NewGuid(); Guid optionId = Guid.NewGuid();
        Guid slotCategoryId = Guid.NewGuid(); Guid slotOptionId = Guid.NewGuid();
        ClusterModel cluster = Cluster(Guid.NewGuid(), identityId, categoryId, optionId, slotCategoryId, slotOptionId);
        ClusterBatchExportOutcome outcome = ClusterBatchExporter.Create(
            new ClusterBatchExportRequest { Scope = ClusterBatchExportScope.All }, [cluster], DateTimeOffset.UtcNow,
            [new ClusterIdentity { MetaInfo = Meta(identityId), Name = "Operator code" }, new ClusterIdentity { MetaInfo = Meta(Guid.NewGuid()), Name = "Unused" }],
            [new ClusterFeatureCategory { MetaInfo = Meta(categoryId), Name = "Type", Options = [new() { ID = optionId, Name = "Platform" }, new() { ID = Guid.NewGuid(), Name = "Unused" }] }],
            [new SlotFeatureCategory { MetaInfo = Meta(slotCategoryId), Name = "Status", Options = [new() { ID = slotOptionId, Name = "Available" }] }]);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.IsSuccess, Is.True);
            Assert.That(outcome.Document!.CatalogDependencies.Identities, Has.Count.EqualTo(1));
            Assert.That(outcome.Document.CatalogDependencies.ClusterFeatureCategories.Single().Options, Has.Count.EqualTo(1));
            Assert.That(outcome.Document.CatalogDependencies.SlotFeatureCategories.Single().Options, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Restore_maps_local_catalogs_and_external_references_then_rewrites_cluster()
    {
        using SqliteConnection connection = OpenDatabase();
        Guid sourceIdentity = Guid.NewGuid(), localIdentity = Guid.NewGuid();
        Guid sourceCategory = Guid.NewGuid(), localCategory = Guid.NewGuid();
        Guid sourceOption = Guid.NewGuid(), localOption = Guid.NewGuid();
        Guid sourceSlotCategory = Guid.NewGuid(), localSlotCategory = Guid.NewGuid();
        Guid sourceSlotOption = Guid.NewGuid(), localSlotOption = Guid.NewGuid();
        InsertIdentity(connection, new ClusterIdentity { MetaInfo = Meta(localIdentity), Name = " Operator   Code " });
        InsertClusterCategory(connection, new ClusterFeatureCategory { MetaInfo = Meta(localCategory), Name = "TYPE", Options = [new() { ID = localOption, Name = "Platform" }] });
        InsertSlotCategory(connection, new SlotFeatureCategory { MetaInfo = Meta(localSlotCategory), Name = "Status", Options = [new() { ID = localSlotOption, Name = "Available" }] });

        Guid clusterId = Guid.NewGuid(), sourceField = Guid.NewGuid(), localField = Guid.NewGuid(), sourceRig = Guid.NewGuid(), localRig = Guid.NewGuid();
        ClusterModel cluster = Cluster(clusterId, sourceIdentity, sourceCategory, sourceOption, sourceSlotCategory, sourceSlotOption);
        cluster.FieldID = sourceField; cluster.RigID = sourceRig;
        ClusterBatchRestoreRequest request = Request(cluster,
            new ClusterIdentity { MetaInfo = Meta(sourceIdentity), Name = "operator code" },
            new ClusterFeatureCategory { MetaInfo = Meta(sourceCategory), Name = "Type", Options = [new() { ID = sourceOption, Name = "Platform" }] },
            new SlotFeatureCategory { MetaInfo = Meta(sourceSlotCategory), Name = "Status", Options = [new() { ID = sourceSlotOption, Name = "Available" }] },
            sourceField, sourceRig);

        ClusterBatchRestoreOutcome outcome = ClusterBatchRestorer.Restore(connection, request, DateTimeOffset.UtcNow,
            [External("Field", "Alve Nord", sourceField, localField), External("Rig", "Rig A", sourceRig, localRig)]);
        ClusterModel restored = ReadCluster(connection, clusterId);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.IsSuccess, Is.True);
            Assert.That(restored.FieldID, Is.EqualTo(localField));
            Assert.That(restored.RigID, Is.EqualTo(localRig));
            Assert.That(restored.ClusterIdentityAssignments!.Single().IdentityID, Is.EqualTo(localIdentity));
            Assert.That(restored.ClusterFeatureAssignments!.Single().FeatureCategoryID, Is.EqualTo(localCategory));
            Assert.That(restored.ClusterFeatureAssignments!.Single().FeatureOptionID, Is.EqualTo(localOption));
            Assert.That(restored.Slots!.Values.Single().SlotFeatureAssignments!.Single().FeatureCategoryID, Is.EqualTo(localSlotCategory));
            Assert.That(restored.Slots.Values.Single().SlotFeatureAssignments!.Single().FeatureOptionID, Is.EqualTo(localSlotOption));
        });
    }

    [Test]
    public void Conflict_rolls_back_catalog_creation_and_cluster_writes()
    {
        using SqliteConnection connection = OpenDatabase();
        Guid clusterId = Guid.NewGuid(), identityId = Guid.NewGuid();
        InsertCluster(connection, new ClusterModel { MetaInfo = Meta(clusterId), Name = "Existing" });
        ClusterModel incoming = new() { MetaInfo = Meta(clusterId), Name = "Replacement", ClusterIdentityAssignments = [new() { ID = Guid.NewGuid(), IdentityID = identityId }] };
        ClusterBatchRestoreRequest request = new()
        {
            ConflictPolicy = ClusterBatchRestoreConflictPolicy.FailIfExists,
            CatalogPolicy = ClusterBatchCatalogRestorePolicy.MapOrCreateMissing,
            Document = new ClusterBatchExportDocument
            {
                ExportedAtUtc = DateTimeOffset.UtcNow,
                CatalogDependencies = new() { Identities = [new ClusterIdentity { MetaInfo = Meta(identityId), Name = "New identity" }] },
                ExternalReferences = new(), Clusters = [incoming]
            }
        };

        ClusterBatchRestoreOutcome outcome = ClusterBatchRestorer.Restore(connection, request, DateTimeOffset.UtcNow);
        Assert.Multiple(() =>
        {
            Assert.That(outcome.FailureKind, Is.EqualTo(ClusterBatchRestoreFailureKind.Conflict));
            Assert.That(Scalar(connection, "SELECT COUNT(*) FROM ClusterIdentityTable"), Is.EqualTo(0));
            Assert.That(ReadCluster(connection, clusterId).Name, Is.EqualTo("Existing"));
        });
    }

    [Test]
    public void Map_or_create_missing_assigns_local_catalog_uuids_and_commits_atomically()
    {
        using SqliteConnection connection = OpenDatabase();
        Guid clusterId = Guid.NewGuid(), sourceIdentity = Guid.NewGuid();
        ClusterModel cluster = new()
        { MetaInfo = Meta(clusterId), Name = "Imported", ClusterIdentityAssignments = [new() { ID = Guid.NewGuid(), IdentityID = sourceIdentity }] };
        ClusterBatchRestoreRequest request = new()
        {
            ConflictPolicy = ClusterBatchRestoreConflictPolicy.FailIfExists,
            CatalogPolicy = ClusterBatchCatalogRestorePolicy.MapOrCreateMissing,
            Document = new ClusterBatchExportDocument
            {
                ExportedAtUtc = DateTimeOffset.UtcNow,
                CatalogDependencies = new() { Identities = [new ClusterIdentity { MetaInfo = Meta(sourceIdentity), Name = "Imported identity" }] },
                ExternalReferences = new(), Clusters = [cluster]
            }
        };

        ClusterBatchRestoreOutcome outcome = ClusterBatchRestorer.Restore(connection, request, DateTimeOffset.UtcNow);
        Guid localIdentity = ReadCluster(connection, clusterId).ClusterIdentityAssignments!.Single().IdentityID!.Value;
        Assert.Multiple(() =>
        {
            Assert.That(outcome.IsSuccess, Is.True);
            Assert.That(outcome.Response!.CreatedCatalogDefinitionCount, Is.EqualTo(1));
            Assert.That(localIdentity, Is.Not.EqualTo(sourceIdentity));
            Assert.That(outcome.Response.CatalogMappings.Single().Resolution, Is.EqualTo("Created"));
            Assert.That(Scalar(connection, "SELECT COUNT(*) FROM ClusterIdentityTable"), Is.EqualTo(1));
        });
    }

    [Test]
    public void Restore_validation_rejects_mismatched_slot_dictionary_key()
    {
        Guid clusterId = Guid.NewGuid();
        Guid key = Guid.NewGuid();
        ClusterBatchRestoreRequest request = new()
        {
            ConflictPolicy = ClusterBatchRestoreConflictPolicy.FailIfExists,
            CatalogPolicy = ClusterBatchCatalogRestorePolicy.MapExisting,
            Document = new ClusterBatchExportDocument
            {
                ExportedAtUtc = DateTimeOffset.UtcNow,
                CatalogDependencies = new(), ExternalReferences = new(),
                Clusters = [new ClusterModel { MetaInfo = Meta(clusterId), Slots = new Dictionary<Guid, Slot> { [key] = new() { ID = Guid.NewGuid() } } }]
            }
        };

        List<ClusterBatchError> errors = ClusterBatchRestorer.ValidateRequest(request);
        Assert.That(errors.Any(error => error.Code == "slot_id_mismatch" && error.PositionIndex == 0), Is.True);
    }

    private static ClusterBatchRestoreRequest Request(ClusterModel cluster, ClusterIdentity identity,
        ClusterFeatureCategory category, SlotFeatureCategory slotCategory, Guid field, Guid rig) => new()
    {
        ConflictPolicy = ClusterBatchRestoreConflictPolicy.FailIfExists,
        CatalogPolicy = ClusterBatchCatalogRestorePolicy.MapExisting,
        Document = new ClusterBatchExportDocument
        {
            ExportedAtUtc = DateTimeOffset.UtcNow,
            CatalogDependencies = new() { Identities = [identity], ClusterFeatureCategories = [category], SlotFeatureCategories = [slotCategory] },
            ExternalReferences = new()
            {
                Fields = [new() { SourceID = field, Name = "Alve Nord" }],
                Rigs = [new() { SourceID = rig, Name = "Rig A" }]
            },
            Clusters = [cluster]
        }
    };

    private static ClusterModel Cluster(Guid id, Guid identity, Guid category, Guid option, Guid slotCategory, Guid slotOption)
    {
        Guid slotId = Guid.NewGuid();
        return new ClusterModel
        {
            MetaInfo = Meta(id), Name = "Cluster",
            ClusterIdentityAssignments = [new() { ID = Guid.NewGuid(), IdentityID = identity }],
            ClusterFeatureAssignments = [new() { ID = Guid.NewGuid(), FeatureCategoryID = category, FeatureOptionID = option }],
            Slots = new Dictionary<Guid, Slot> { [slotId] = new() { ID = slotId, SlotFeatureAssignments = [new() { ID = Guid.NewGuid(), FeatureCategoryID = slotCategory, FeatureOptionID = slotOption }] } }
        };
    }

    private static ClusterBatchExternalReferenceMapping External(string resource, string name, Guid source, Guid local) =>
        new() { Resource = resource, Name = name, SourceID = source, LocalID = local, Resolution = "NormalizedName" };
    private static MetaInfo Meta(Guid id) => new() { ID = id };

    private static SqliteConnection OpenDatabase()
    {
        SqliteConnection connection = new("Data Source=:memory:"); connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE ClusterTable (ID text primary key, MetaInfo text, FieldID text, IsSingleWell bool, RigID text, IsFixedPlatform bool, Cluster text);
            CREATE TABLE ClusterIdentityTable (ID text primary key, MetaInfo text, Name text, CreationDate text, LastModificationDate text, ClusterIdentity text);
            CREATE TABLE ClusterFeatureCategoryTable (ID text primary key, MetaInfo text, Name text, IsExclusive integer, HasValidityPeriod integer, CreationDate text, LastModificationDate text, ClusterFeatureCategory text);
            CREATE TABLE SlotFeatureCategoryTable (ID text primary key, MetaInfo text, Name text, IsExclusive integer, HasValidityPeriod integer, CreationDate text, LastModificationDate text, SlotFeatureCategory text);
            """;
        command.ExecuteNonQuery(); return connection;
    }

    private static void InsertIdentity(SqliteConnection connection, ClusterIdentity value) => InsertDocument(connection, "ClusterIdentityTable", "ClusterIdentity", value.MetaInfo!.ID, value.MetaInfo, value.Name, value);
    private static void InsertClusterCategory(SqliteConnection connection, ClusterFeatureCategory value) => InsertCategory(connection, "ClusterFeatureCategoryTable", "ClusterFeatureCategory", value.MetaInfo!.ID, value.MetaInfo, value.Name, value.IsExclusive, value.HasValidityPeriod, value);
    private static void InsertSlotCategory(SqliteConnection connection, SlotFeatureCategory value) => InsertCategory(connection, "SlotFeatureCategoryTable", "SlotFeatureCategory", value.MetaInfo!.ID, value.MetaInfo, value.Name, value.IsExclusive, value.HasValidityPeriod, value);
    private static void InsertDocument(SqliteConnection c, string table, string column, Guid id, MetaInfo meta, string? name, object value)
    { using SqliteCommand cmd = c.CreateCommand(); cmd.CommandText = $"INSERT INTO {table}(ID,MetaInfo,Name,CreationDate,LastModificationDate,{column}) VALUES($id,$meta,$name,'','',$doc)"; cmd.Parameters.AddWithValue("$id", id.ToString()); cmd.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(meta, JsonSettings.Options)); cmd.Parameters.AddWithValue("$name", name ?? ""); cmd.Parameters.AddWithValue("$doc", JsonSerializer.Serialize(value, JsonSettings.Options)); cmd.ExecuteNonQuery(); }
    private static void InsertCategory(SqliteConnection c, string table, string column, Guid id, MetaInfo meta, string? name, bool exclusive, bool validity, object value)
    { using SqliteCommand cmd = c.CreateCommand(); cmd.CommandText = $"INSERT INTO {table}(ID,MetaInfo,Name,IsExclusive,HasValidityPeriod,CreationDate,LastModificationDate,{column}) VALUES($id,$meta,$name,$e,$v,'','',$doc)"; cmd.Parameters.AddWithValue("$id", id.ToString()); cmd.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(meta, JsonSettings.Options)); cmd.Parameters.AddWithValue("$name", name ?? ""); cmd.Parameters.AddWithValue("$e", exclusive); cmd.Parameters.AddWithValue("$v", validity); cmd.Parameters.AddWithValue("$doc", JsonSerializer.Serialize(value, JsonSettings.Options)); cmd.ExecuteNonQuery(); }
    private static void InsertCluster(SqliteConnection c, ClusterModel value)
    { using SqliteCommand cmd = c.CreateCommand(); cmd.CommandText = "INSERT INTO ClusterTable(ID,MetaInfo,FieldID,IsSingleWell,RigID,IsFixedPlatform,Cluster) VALUES($id,$meta,'',0,'',0,$doc)"; cmd.Parameters.AddWithValue("$id", value.MetaInfo!.ID.ToString()); cmd.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(value.MetaInfo, JsonSettings.Options)); cmd.Parameters.AddWithValue("$doc", JsonSerializer.Serialize(value, JsonSettings.Options)); cmd.ExecuteNonQuery(); }
    private static ClusterModel ReadCluster(SqliteConnection c, Guid id)
    { using SqliteCommand cmd = c.CreateCommand(); cmd.CommandText = "SELECT Cluster FROM ClusterTable WHERE ID=$id"; cmd.Parameters.AddWithValue("$id", id.ToString()); return JsonSerializer.Deserialize<ClusterModel>((string)cmd.ExecuteScalar()!, JsonSettings.Options)!; }
    private static long Scalar(SqliteConnection c, string sql) { using SqliteCommand cmd = c.CreateCommand(); cmd.CommandText = sql; return (long)cmd.ExecuteScalar()!; }
}
