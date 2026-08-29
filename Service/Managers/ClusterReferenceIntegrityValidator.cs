using Microsoft.Data.Sqlite;
using OSDC.Drilling.Cluster.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OSDC.Drilling.Cluster.Service.Managers;

internal static class ClusterReferenceIntegrityValidator
{
    public static List<ClusterMutationError> ValidateCluster(SqliteConnection connection, SqliteTransaction transaction, Model.Cluster cluster)
    {
        Dictionary<Guid, HashSet<Guid>> clusterOptions = ReadCategoryOptions<ClusterFeatureCategory, ClusterFeatureOption>(
            connection, transaction, "ClusterFeatureCategoryTable", "ClusterFeatureCategory", value => value.MetaInfo?.ID, value => value.Options, value => value.ID);
        Dictionary<Guid, HashSet<Guid>> slotOptions = ReadCategoryOptions<SlotFeatureCategory, SlotFeatureOption>(
            connection, transaction, "SlotFeatureCategoryTable", "SlotFeatureCategory", value => value.MetaInfo?.ID, value => value.Options, value => value.ID);
        HashSet<Guid> identities = ReadDocuments<ClusterIdentity>(connection, transaction, "ClusterIdentityTable", "ClusterIdentity")
            .Select(value => value.MetaInfo?.ID).OfType<Guid>().Where(value => value != Guid.Empty).ToHashSet();
        List<ClusterMutationError> errors = [];

        for (int index = 0; index < (cluster.ClusterIdentityAssignments?.Count ?? 0); index++)
        {
            Guid? id = cluster.ClusterIdentityAssignments![index].IdentityID;
            if (id is Guid value && (value == Guid.Empty || !identities.Contains(value)))
                errors.Add(Error($"ClusterIdentityAssignments[{index}].IdentityID", "cluster_identity_not_found", $"No local Cluster identity has UUID {value}."));
        }
        for (int index = 0; index < (cluster.ClusterFeatureAssignments?.Count ?? 0); index++)
        {
            ClusterFeatureAssignment assignment = cluster.ClusterFeatureAssignments![index];
            ValidateCategory(assignment.FeatureCategoryID, assignment.FeatureOptionID, clusterOptions,
                $"ClusterFeatureAssignments[{index}]", errors);
        }
        foreach ((Guid key, Slot slot) in cluster.Slots ?? [])
        {
            if (key != slot.ID)
                errors.Add(Error($"Slots[{key}].ID", "slot_id_mismatch", $"Slot dictionary key {key} must equal Slot.ID {slot.ID}."));
            int index = 0;
            foreach (SlotFeatureAssignment assignment in slot.SlotFeatureAssignments ?? [])
            {
                ValidateCategory(assignment.FeatureCategoryID, assignment.FeatureOptionID, slotOptions,
                    $"Slots[{key}].SlotFeatureAssignments[{index}]", errors);
                index++;
            }
        }
        return errors;
    }

    public static ClusterMutationError? FindIdentityReferences(SqliteConnection connection, SqliteTransaction transaction, Guid id) =>
        FindReferences(connection, transaction, cluster => (cluster.ClusterIdentityAssignments ?? []).Any(value => value.IdentityID == id),
            "ClusterIdentityAssignments.IdentityID", "catalog_in_use", "The Cluster identity is referenced by one or more Clusters.");

    public static ClusterMutationError? FindClusterCategoryReferences(SqliteConnection connection, SqliteTransaction transaction,
        Guid id, IReadOnlyCollection<Guid>? permittedOptions = null) => FindReferences(connection, transaction,
            cluster => (cluster.ClusterFeatureAssignments ?? []).Any(value => value.FeatureCategoryID == id &&
                (permittedOptions == null || value.FeatureOptionID is Guid option && !permittedOptions.Contains(option))),
            permittedOptions == null ? "ClusterFeatureAssignments.FeatureCategoryID" : "ClusterFeatureAssignments.FeatureOptionID",
            permittedOptions == null ? "catalog_in_use" : "catalog_option_in_use",
            permittedOptions == null ? "The Cluster feature category is referenced by one or more Clusters." : "The update removes an option referenced by one or more Clusters.");

    public static ClusterMutationError? FindSlotCategoryReferences(SqliteConnection connection, SqliteTransaction transaction,
        Guid id, IReadOnlyCollection<Guid>? permittedOptions = null) => FindReferences(connection, transaction,
            cluster => (cluster.Slots ?? []).Values.SelectMany(slot => slot.SlotFeatureAssignments ?? []).Any(value => value.FeatureCategoryID == id &&
                (permittedOptions == null || value.FeatureOptionID is Guid option && !permittedOptions.Contains(option))),
            permittedOptions == null ? "Slots.SlotFeatureAssignments.FeatureCategoryID" : "Slots.SlotFeatureAssignments.FeatureOptionID",
            permittedOptions == null ? "catalog_in_use" : "catalog_option_in_use",
            permittedOptions == null ? "The Slot feature category is referenced by one or more Clusters." : "The update removes an option referenced by one or more Clusters.");

    private static void ValidateCategory(Guid? categoryId, Guid? optionId, IReadOnlyDictionary<Guid, HashSet<Guid>> optionsByCategory,
        string path, List<ClusterMutationError> errors)
    {
        if (categoryId == null && optionId == null) return;
        if (categoryId is not Guid category || category == Guid.Empty)
        {
            errors.Add(Error($"{path}.FeatureCategoryID", "category_id_required", "A category UUID is required when an option is selected."));
            return;
        }
        if (!optionsByCategory.TryGetValue(category, out HashSet<Guid>? options))
        {
            errors.Add(Error($"{path}.FeatureCategoryID", "category_not_found", $"No local category has UUID {category}."));
            return;
        }
        if (optionId is not Guid option || option == Guid.Empty)
            errors.Add(Error($"{path}.FeatureOptionID", "option_id_required", "An option UUID is required when a category is selected."));
        else if (!options.Contains(option))
            errors.Add(Error($"{path}.FeatureOptionID", "option_not_in_category", $"Option UUID {option} does not belong to category UUID {category}."));
    }

    private static ClusterMutationError? FindReferences(SqliteConnection connection, SqliteTransaction transaction,
        Func<Model.Cluster, bool> predicate, string property, string code, string message)
    {
        List<Guid> ids = ReadClusters(connection, transaction).Where(pair => predicate(pair.Value)).Select(pair => pair.Key).Distinct().Order().ToList();
        return ids.Count == 0 ? null : new ClusterMutationError { Property = property, Code = code, Message = message, ReferencingClusterIDs = ids };
    }

    private static Dictionary<Guid, Model.Cluster> ReadClusters(SqliteConnection connection, SqliteTransaction transaction)
    {
        Dictionary<Guid, Model.Cluster> result = [];
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT ID, Cluster FROM ClusterTable";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            Model.Cluster? value = JsonSerializer.Deserialize<Model.Cluster>(reader.GetString(1), JsonSettings.Options);
            if (value != null && Guid.TryParse(reader.GetString(0), out Guid id)) result[id] = value;
        }
        return result;
    }

    private static Dictionary<Guid, HashSet<Guid>> ReadCategoryOptions<TCategory, TOption>(SqliteConnection connection,
        SqliteTransaction transaction, string table, string column, Func<TCategory, Guid?> categoryId,
        Func<TCategory, List<TOption>?> options, Func<TOption, Guid> optionId)
    {
        Dictionary<Guid, HashSet<Guid>> result = [];
        foreach (TCategory category in ReadDocuments<TCategory>(connection, transaction, table, column))
            if (categoryId(category) is Guid id && id != Guid.Empty)
                result[id] = (options(category) ?? []).Select(optionId).Where(value => value != Guid.Empty).ToHashSet();
        return result;
    }

    private static List<T> ReadDocuments<T>(SqliteConnection connection, SqliteTransaction transaction, string table, string column)
    {
        List<T> result = [];
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT {column} FROM {table}";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
            if (JsonSerializer.Deserialize<T>(reader.GetString(0), JsonSettings.Options) is T value) result.Add(value);
        return result;
    }

    private static ClusterMutationError Error(string property, string code, string message) => new() { Property = property, Code = code, Message = message };
}
