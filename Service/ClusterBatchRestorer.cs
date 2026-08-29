using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using OSDC.Drilling.Cluster.Model;
using OSDC.Drilling.Cluster.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;

namespace OSDC.Drilling.Cluster.Service;

public enum ClusterBatchRestoreFailureKind { None, InvalidRequest, Conflict, StorageFailure }

public sealed class ClusterBatchRestoreOutcome
{
    public ClusterBatchRestoreResponse? Response { get; init; }
    public ClusterBatchErrorEnvelope? Error { get; init; }
    public ClusterBatchRestoreFailureKind FailureKind { get; init; }
    public bool IsSuccess => Response != null && FailureKind == ClusterBatchRestoreFailureKind.None;
}

public static class ClusterBatchRestorer
{
    public static ClusterBatchRestoreOutcome StorageFailure(string message) => Failure(
        ClusterBatchRestoreFailureKind.StorageFailure, "cluster_restore_failed", message,
        [Error(null, "Document", "storage_failure", "No restore changes were committed.")]);

    public static ClusterBatchRestoreOutcome Restore(SqliteConnection connection,
        ClusterBatchRestoreRequest? request, DateTimeOffset restoredAtUtc,
        IReadOnlyList<ClusterBatchExternalReferenceMapping>? externalMappings = null)
    {
        List<ClusterBatchError> errors = ValidateRequest(request);
        if (errors.Count != 0)
            return Failure(ClusterBatchRestoreFailureKind.InvalidRequest, "invalid_batch_restore_request",
                "The cluster batch-restore request is invalid. No changes were made.", errors);

        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            ClusterBatchExportDocument document = request!.Document!;
            List<Model.Cluster> clusters = CloneClusters(document.Clusters);
            RewriteExternalReferences(clusters, externalMappings ?? []);
            CatalogState state = CatalogState.Load(connection, transaction);
            List<ClusterBatchCatalogMapping> mappings = [];
            int createdDefinitions = 0;
            int createdOptions = 0;
            bool createMissing = request.CatalogPolicy == ClusterBatchCatalogRestorePolicy.MapOrCreateMissing;

            ResolveIdentities(document.CatalogDependencies.Identities, state, createMissing,
                restoredAtUtc, mappings, errors, ref createdDefinitions);
            ResolveClusterCategories(document.CatalogDependencies.ClusterFeatureCategories, state,
                createMissing, restoredAtUtc, mappings, errors, ref createdDefinitions, ref createdOptions);
            ResolveSlotCategories(document.CatalogDependencies.SlotFeatureCategories, state,
                createMissing, restoredAtUtc, mappings, errors, ref createdDefinitions, ref createdOptions);
            if (errors.Count != 0)
            {
                transaction.Rollback();
                return Failure(ClusterBatchRestoreFailureKind.Conflict, "catalog_mapping_failed",
                    "Local catalog dependencies could not be resolved. No changes were made.", errors);
            }

            RewriteReferences(clusters, mappings);
            List<Guid> existing = clusters.Select(value => value.MetaInfo!.ID)
                .Where(id => RowExists(connection, transaction, "ClusterTable", id)).ToList();
            if (existing.Count != 0 && request.ConflictPolicy == ClusterBatchRestoreConflictPolicy.FailIfExists)
            {
                transaction.Rollback();
                return Failure(ClusterBatchRestoreFailureKind.Conflict, "cluster_already_exists",
                    "One or more cluster UUIDs already exist. No changes were made.",
                    existing.Select(id => Error(document.Clusters.FindIndex(value => value.MetaInfo?.ID == id),
                        "Document.Clusters.MetaInfo.ID", "uuid_conflict", $"Cluster UUID '{id}' already exists.")).ToList());
            }

            state.Save(connection, transaction);
            SaveClusters(connection, transaction, clusters, request.ConflictPolicy);
            transaction.Commit();
            return new ClusterBatchRestoreOutcome
            {
                Response = new ClusterBatchRestoreResponse
                {
                    RestoredAtUtc = restoredAtUtc.ToUniversalTime(),
                    CreatedCount = clusters.Count - existing.Count,
                    ReplacedCount = existing.Count,
                    CreatedCatalogDefinitionCount = createdDefinitions,
                    CreatedCatalogOptionCount = createdOptions,
                    CatalogMappings = mappings,
                    ExternalReferenceMappings = (externalMappings ?? []).ToList(),
                    ClusterIDs = clusters.Select(value => value.MetaInfo!.ID).ToList()
                }
            };
        }
        catch (Exception ex) when (ex is SqliteException or JsonException or InvalidOperationException or KeyNotFoundException)
        {
            try { transaction.Rollback(); } catch { }
            return StorageFailure($"The atomic cluster restore failed: {ex.Message}");
        }
    }

    private static void ResolveIdentities(IEnumerable<ClusterIdentity> sourceValues, CatalogState state,
        bool createMissing, DateTimeOffset now, List<ClusterBatchCatalogMapping> mappings,
        List<ClusterBatchError> errors, ref int createdDefinitions)
    {
        foreach (ClusterIdentity source in sourceValues)
        {
            Guid sourceId = source.MetaInfo!.ID;
            ClusterIdentity? local = state.Identities.SingleOrDefault(value => value.MetaInfo?.ID == sourceId);
            string resolution = "ExactUUID";
            if (local != null && !SameName(local.Name, source.Name))
            {
                AddSemanticConflict(errors, "cluster identity", sourceId, source.Name);
                continue;
            }
            if (local == null)
            {
                List<ClusterIdentity> matches = state.Identities.Where(value => SameName(value.Name, source.Name)).ToList();
                if (matches.Count > 1) { AddAmbiguous(errors, "cluster identity", sourceId, source.Name); continue; }
                if (matches.Count == 1) { local = matches[0]; resolution = "NormalizedName"; }
                else if (!createMissing) { AddMissing(errors, "cluster identity", sourceId, source.Name); continue; }
                else
                {
                    local = new ClusterIdentity
                    {
                        MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, Name = source.Name,
                        CreationDate = now.ToUniversalTime(), LastModificationDate = now.ToUniversalTime()
                    };
                    state.Identities.Add(local); state.DirtyIdentities.Add(local);
                    createdDefinitions++; resolution = "Created";
                }
            }
            AddMapping(mappings, "ClusterIdentity", source.Name, sourceId, local.MetaInfo!.ID, resolution);
        }
    }

    private static void ResolveClusterCategories(IEnumerable<ClusterFeatureCategory> sources, CatalogState state,
        bool createMissing, DateTimeOffset now, List<ClusterBatchCatalogMapping> mappings,
        List<ClusterBatchError> errors, ref int createdDefinitions, ref int createdOptions)
    {
        foreach (ClusterFeatureCategory source in sources)
        {
            ClusterFeatureCategory? local = ResolveCategory(source.MetaInfo!.ID, source.Name,
                source.IsExclusive, source.HasValidityPeriod, state.ClusterCategories,
                value => value.MetaInfo?.ID, value => value.Name, value => value.IsExclusive,
                value => value.HasValidityPeriod, "cluster feature category", createMissing, errors,
                () => new ClusterFeatureCategory
                {
                    MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, Name = source.Name,
                    IsExclusive = source.IsExclusive, HasValidityPeriod = source.HasValidityPeriod,
                    Options = [], CreationDate = now.ToUniversalTime(), LastModificationDate = now.ToUniversalTime()
                }, out string resolution);
            if (local == null) continue;
            if (resolution == "Created") { state.ClusterCategories.Add(local); state.DirtyClusterCategories.Add(local); createdDefinitions++; }
            AddMapping(mappings, "ClusterFeatureCategory", source.Name, source.MetaInfo.ID, local.MetaInfo!.ID, resolution);
            ResolveOptions(source.Options ?? [], local.Options ??= [], value => value.ID, value => value.Name,
                (id, name) => new ClusterFeatureOption { ID = id, Name = name }, "ClusterFeatureOption",
                source.Name, createMissing, mappings, errors, state.DirtyClusterCategories, local, ref createdOptions);
        }
    }

    private static void ResolveSlotCategories(IEnumerable<SlotFeatureCategory> sources, CatalogState state,
        bool createMissing, DateTimeOffset now, List<ClusterBatchCatalogMapping> mappings,
        List<ClusterBatchError> errors, ref int createdDefinitions, ref int createdOptions)
    {
        foreach (SlotFeatureCategory source in sources)
        {
            SlotFeatureCategory? local = ResolveCategory(source.MetaInfo!.ID, source.Name,
                source.IsExclusive, source.HasValidityPeriod, state.SlotCategories,
                value => value.MetaInfo?.ID, value => value.Name, value => value.IsExclusive,
                value => value.HasValidityPeriod, "slot feature category", createMissing, errors,
                () => new SlotFeatureCategory
                {
                    MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, Name = source.Name,
                    IsExclusive = source.IsExclusive, HasValidityPeriod = source.HasValidityPeriod,
                    Options = [], CreationDate = now.ToUniversalTime(), LastModificationDate = now.ToUniversalTime()
                }, out string resolution);
            if (local == null) continue;
            if (resolution == "Created") { state.SlotCategories.Add(local); state.DirtySlotCategories.Add(local); createdDefinitions++; }
            AddMapping(mappings, "SlotFeatureCategory", source.Name, source.MetaInfo.ID, local.MetaInfo!.ID, resolution);
            ResolveOptions(source.Options ?? [], local.Options ??= [], value => value.ID, value => value.Name,
                (id, name) => new SlotFeatureOption { ID = id, Name = name }, "SlotFeatureOption",
                source.Name, createMissing, mappings, errors, state.DirtySlotCategories, local, ref createdOptions);
        }
    }

    private static T? ResolveCategory<T>(Guid sourceId, string? sourceName, bool exclusive, bool validity,
        List<T> localValues, Func<T, Guid?> id, Func<T, string?> name, Func<T, bool> isExclusive,
        Func<T, bool> hasValidity, string kind, bool createMissing, List<ClusterBatchError> errors,
        Func<T> create, out string resolution) where T : class
    {
        T? exact = localValues.SingleOrDefault(value => id(value) == sourceId);
        if (exact != null)
        {
            resolution = "ExactUUID";
            if (!SameName(name(exact), sourceName) || isExclusive(exact) != exclusive || hasValidity(exact) != validity)
            { AddSemanticConflict(errors, kind, sourceId, sourceName); return null; }
            return exact;
        }
        List<T> matches = localValues.Where(value => SameName(name(value), sourceName)).ToList();
        if (matches.Count > 1) { resolution = ""; AddAmbiguous(errors, kind, sourceId, sourceName); return null; }
        if (matches.Count == 1)
        {
            resolution = "NormalizedName";
            T match = matches[0];
            if (isExclusive(match) != exclusive || hasValidity(match) != validity)
            { AddSemanticConflict(errors, kind, sourceId, sourceName); return null; }
            return match;
        }
        if (!createMissing) { resolution = ""; AddMissing(errors, kind, sourceId, sourceName); return null; }
        resolution = "Created";
        return create();
    }

    private static void ResolveOptions<TOption, TCategory>(IEnumerable<TOption> sources, List<TOption> locals,
        Func<TOption, Guid> id, Func<TOption, string?> name, Func<Guid, string?, TOption> create,
        string catalog, string? categoryName, bool createMissing, List<ClusterBatchCatalogMapping> mappings,
        List<ClusterBatchError> errors, HashSet<TCategory> dirty, TCategory category, ref int createdOptions)
        where TOption : class where TCategory : class
    {
        foreach (TOption source in sources)
        {
            Guid sourceId = id(source);
            TOption? local = locals.SingleOrDefault(value => id(value) == sourceId);
            string resolution = "ExactUUID";
            if (local != null && !SameName(name(local), name(source)))
            { AddSemanticConflict(errors, $"{catalog} in category '{categoryName}'", sourceId, name(source)); continue; }
            if (local == null)
            {
                List<TOption> matches = locals.Where(value => SameName(name(value), name(source))).ToList();
                if (matches.Count > 1) { AddAmbiguous(errors, catalog, sourceId, name(source)); continue; }
                if (matches.Count == 1) { local = matches[0]; resolution = "NormalizedName"; }
                else if (!createMissing) { AddMissing(errors, catalog, sourceId, name(source)); continue; }
                else
                {
                    local = create(Guid.NewGuid(), name(source)); locals.Add(local); dirty.Add(category);
                    createdOptions++; resolution = "Created";
                }
            }
            AddMapping(mappings, catalog, name(source), sourceId, id(local), resolution);
        }
    }

    private static void RewriteReferences(IEnumerable<Model.Cluster> clusters, IEnumerable<ClusterBatchCatalogMapping> mappings)
    {
        Dictionary<Guid, Guid> map = mappings.ToDictionary(value => value.SourceID, value => value.LocalID);
        foreach (Model.Cluster cluster in clusters)
        {
            foreach (ClusterIdentityAssignment assignment in cluster.ClusterIdentityAssignments ?? [])
                assignment.IdentityID = map[assignment.IdentityID!.Value];
            foreach (ClusterFeatureAssignment assignment in cluster.ClusterFeatureAssignments ?? [])
            { assignment.FeatureCategoryID = map[assignment.FeatureCategoryID!.Value]; assignment.FeatureOptionID = map[assignment.FeatureOptionID!.Value]; }
            foreach (Slot slot in cluster.Slots?.Values ?? Enumerable.Empty<Slot>())
                foreach (SlotFeatureAssignment assignment in slot.SlotFeatureAssignments ?? [])
                { assignment.FeatureCategoryID = map[assignment.FeatureCategoryID!.Value]; assignment.FeatureOptionID = map[assignment.FeatureOptionID!.Value]; }
        }
    }

    private static void RewriteExternalReferences(IEnumerable<Model.Cluster> clusters,
        IEnumerable<ClusterBatchExternalReferenceMapping> mappings)
    {
        Dictionary<Guid, Guid> fields = mappings.Where(value => value.Resource == "Field")
            .ToDictionary(value => value.SourceID, value => value.LocalID);
        Dictionary<Guid, Guid> rigs = mappings.Where(value => value.Resource == "Rig")
            .ToDictionary(value => value.SourceID, value => value.LocalID);
        foreach (Model.Cluster cluster in clusters)
        {
            if (cluster.FieldID is Guid fieldId && fieldId != Guid.Empty) cluster.FieldID = fields[fieldId];
            if (cluster.RigID is Guid rigId && rigId != Guid.Empty) cluster.RigID = rigs[rigId];
        }
    }

    public static List<ClusterBatchError> ValidateRequest(ClusterBatchRestoreRequest? request)
    {
        if (request == null) return [Error(null, "Request", "required", "A batch-restore request is required.")];
        List<ClusterBatchError> errors = [];
        if (request.ConflictPolicy is not ClusterBatchRestoreConflictPolicy.FailIfExists and not ClusterBatchRestoreConflictPolicy.ReplaceExisting)
            errors.Add(Error(null, "ConflictPolicy", "invalid_conflict_policy", "ConflictPolicy must be FailIfExists or ReplaceExisting."));
        if (request.CatalogPolicy is not ClusterBatchCatalogRestorePolicy.MapExisting and not ClusterBatchCatalogRestorePolicy.MapOrCreateMissing)
            errors.Add(Error(null, "CatalogPolicy", "invalid_catalog_policy", "CatalogPolicy must be MapExisting or MapOrCreateMissing."));
        ClusterBatchExportDocument? document = request.Document;
        if (document == null) { errors.Add(Error(null, "Document", "required", "A batch-export document is required.")); return errors; }
        if (document.FormatIdentifier != ClusterBatchExportDocument.CurrentFormatIdentifier)
            errors.Add(Error(null, "Document.FormatIdentifier", "unsupported_format", $"FormatIdentifier must be '{ClusterBatchExportDocument.CurrentFormatIdentifier}'."));
        if (document.SchemaVersion != ClusterBatchExportDocument.CurrentSchemaVersion)
            errors.Add(Error(null, "Document.SchemaVersion", "unsupported_schema_version", $"SchemaVersion must be {ClusterBatchExportDocument.CurrentSchemaVersion}."));
        if (document.ExportedAtUtc == default || document.ExportedAtUtc.Offset != TimeSpan.Zero)
            errors.Add(Error(null, "Document.ExportedAtUtc", "invalid_export_timestamp", "ExportedAtUtc must be a non-default UTC timestamp with offset +00:00."));
        if (document.CatalogDependencies == null)
            errors.Add(Error(null, "Document.CatalogDependencies", "required", "CatalogDependencies is required."));
        if (document.ExternalReferences == null)
            errors.Add(Error(null, "Document.ExternalReferences", "required", "ExternalReferences is required."));
        if (document.Clusters == null || document.Clusters.Count == 0)
        { errors.Add(Error(null, "Document.Clusters", "required", "At least one cluster is required.")); return errors; }

        HashSet<Guid> clusterIds = [];
        for (int index = 0; index < document.Clusters.Count; index++)
        {
            Model.Cluster? cluster = document.Clusters[index];
            if (cluster == null) { errors.Add(Error(index, "Document.Clusters", "null_cluster", "A restored cluster must not be null.")); continue; }
            Guid? id = cluster.MetaInfo?.ID;
            if (id == null || id == Guid.Empty) errors.Add(Error(index, "Document.Clusters.MetaInfo.ID", "empty_uuid", "Every cluster must have a non-empty UUID."));
            else if (!clusterIds.Add(id.Value)) errors.Add(Error(index, "Document.Clusters.MetaInfo.ID", "duplicate_uuid", $"Cluster UUID '{id}' occurs more than once."));
            if (cluster.FieldID == Guid.Empty) errors.Add(Error(index, "Document.Clusters.FieldID", "empty_uuid", "FieldID must be null or a non-empty UUID."));
            if (cluster.RigID == Guid.Empty) errors.Add(Error(index, "Document.Clusters.RigID", "empty_uuid", "RigID must be null or a non-empty UUID."));
        }
        if (document.CatalogDependencies != null)
        {
            ValidateDependencies(document.CatalogDependencies, errors);
            ValidateReferences(document.Clusters, document.CatalogDependencies, errors);
        }
        if (document.ExternalReferences != null)
            ValidateExternalReferences(document.Clusters, document.ExternalReferences, errors);
        return errors;
    }

    private static void ValidateExternalReferences(IReadOnlyList<Model.Cluster> clusters,
        ClusterBatchExternalReferences references, List<ClusterBatchError> errors)
    {
        ValidateExternalList(references.Fields, "Fields", errors);
        ValidateExternalList(references.Rigs, "Rigs", errors);
        HashSet<Guid> fields = (references.Fields ?? []).Select(value => value.SourceID).ToHashSet();
        HashSet<Guid> rigs = (references.Rigs ?? []).Select(value => value.SourceID).ToHashSet();
        for (int index = 0; index < clusters.Count; index++)
        {
            if (clusters[index].FieldID is Guid field && field != Guid.Empty && !fields.Contains(field))
                errors.Add(Error(index, "Document.Clusters.FieldID", "external_reference_manifest_missing", $"Field UUID '{field}' is absent from ExternalReferences.Fields."));
            if (clusters[index].RigID is Guid rig && rig != Guid.Empty && !rigs.Contains(rig))
                errors.Add(Error(index, "Document.Clusters.RigID", "external_reference_manifest_missing", $"Rig UUID '{rig}' is absent from ExternalReferences.Rigs."));
        }
    }

    private static void ValidateExternalList(IEnumerable<ClusterBatchExternalReference>? references,
        string property, List<ClusterBatchError> errors)
    {
        HashSet<Guid> ids = [];
        foreach (ClusterBatchExternalReference value in references ?? [])
        {
            if (value.SourceID == Guid.Empty) errors.Add(Error(null, $"Document.ExternalReferences.{property}.SourceID", "empty_uuid", "External source UUIDs must be non-empty."));
            else if (!ids.Add(value.SourceID)) errors.Add(Error(null, $"Document.ExternalReferences.{property}.SourceID", "duplicate_uuid", $"External source UUID '{value.SourceID}' occurs more than once."));
            if (string.IsNullOrWhiteSpace(value.Name)) errors.Add(Error(null, $"Document.ExternalReferences.{property}.Name", "required", "External reference names must not be empty."));
        }
    }

    private static void ValidateDependencies(ClusterBatchCatalogDependencies dependencies, List<ClusterBatchError> errors)
    {
        HashSet<Guid> ids = [];
        void Check(Guid id, string? name, string property)
        {
            if (id == Guid.Empty) errors.Add(Error(null, property, "empty_uuid", "Catalog UUIDs must be non-empty."));
            else if (!ids.Add(id)) errors.Add(Error(null, property, "duplicate_uuid", $"Catalog UUID '{id}' occurs more than once."));
            if (string.IsNullOrWhiteSpace(name)) errors.Add(Error(null, property + ".Name", "required", "Catalog names must not be empty."));
        }
        foreach (ClusterIdentity value in dependencies.Identities ?? []) Check(value?.MetaInfo?.ID ?? Guid.Empty, value?.Name, "Document.CatalogDependencies.Identities");
        foreach (ClusterFeatureCategory value in dependencies.ClusterFeatureCategories ?? [])
        { Check(value?.MetaInfo?.ID ?? Guid.Empty, value?.Name, "Document.CatalogDependencies.ClusterFeatureCategories"); foreach (ClusterFeatureOption option in value?.Options ?? []) Check(option.ID, option.Name, "Document.CatalogDependencies.ClusterFeatureCategories.Options"); }
        foreach (SlotFeatureCategory value in dependencies.SlotFeatureCategories ?? [])
        { Check(value?.MetaInfo?.ID ?? Guid.Empty, value?.Name, "Document.CatalogDependencies.SlotFeatureCategories"); foreach (SlotFeatureOption option in value?.Options ?? []) Check(option.ID, option.Name, "Document.CatalogDependencies.SlotFeatureCategories.Options"); }
    }

    private static void ValidateReferences(IReadOnlyList<Model.Cluster> clusters, ClusterBatchCatalogDependencies dependencies,
        List<ClusterBatchError> errors)
    {
        HashSet<Guid> identities = dependencies.Identities.Select(value => value.MetaInfo!.ID).ToHashSet();
        Dictionary<Guid, HashSet<Guid>> clusterOptions = dependencies.ClusterFeatureCategories.ToDictionary(
            value => value.MetaInfo!.ID, value => (value.Options ?? []).Select(option => option.ID).ToHashSet());
        Dictionary<Guid, HashSet<Guid>> slotOptions = dependencies.SlotFeatureCategories.ToDictionary(
            value => value.MetaInfo!.ID, value => (value.Options ?? []).Select(option => option.ID).ToHashSet());
        for (int index = 0; index < clusters.Count; index++)
        {
            foreach (ClusterIdentityAssignment assignment in clusters[index].ClusterIdentityAssignments ?? [])
                RequireReference(assignment.IdentityID, identities, index, "ClusterIdentityAssignments.IdentityID", errors);
            foreach (ClusterFeatureAssignment assignment in clusters[index].ClusterFeatureAssignments ?? [])
                RequireCategoryReference(assignment.FeatureCategoryID, assignment.FeatureOptionID, clusterOptions, index, "ClusterFeatureAssignments", errors);
            foreach (Slot slot in clusters[index].Slots?.Values ?? Enumerable.Empty<Slot>())
                foreach (SlotFeatureAssignment assignment in slot.SlotFeatureAssignments ?? [])
                    RequireCategoryReference(assignment.FeatureCategoryID, assignment.FeatureOptionID, slotOptions, index, "Slots.SlotFeatureAssignments", errors);
        }
    }

    private static void RequireReference(Guid? id, HashSet<Guid> available, int index, string property, List<ClusterBatchError> errors)
    {
        if (id is not Guid value || value == Guid.Empty || !available.Contains(value))
            errors.Add(Error(index, $"Document.Clusters.{property}", "catalog_dependency_missing", $"Referenced UUID '{id}' is absent from CatalogDependencies."));
    }

    private static void RequireCategoryReference(Guid? categoryId, Guid? optionId,
        Dictionary<Guid, HashSet<Guid>> available, int index, string property, List<ClusterBatchError> errors)
    {
        if (categoryId is not Guid category || category == Guid.Empty || !available.TryGetValue(category, out HashSet<Guid>? options))
        { errors.Add(Error(index, $"Document.Clusters.{property}.FeatureCategoryID", "catalog_dependency_missing", $"Referenced category '{categoryId}' is absent from CatalogDependencies.")); return; }
        if (optionId is not Guid option || option == Guid.Empty || !options.Contains(option))
            errors.Add(Error(index, $"Document.Clusters.{property}.FeatureOptionID", "catalog_dependency_missing", $"Referenced option '{optionId}' is absent from category '{category}'."));
    }

    private static List<Model.Cluster> CloneClusters(List<Model.Cluster> values) =>
        JsonSerializer.Deserialize<List<Model.Cluster>>(JsonSerializer.Serialize(values, JsonSettings.Options), JsonSettings.Options)
        ?? throw new JsonException("Clusters could not be cloned.");

    private static bool RowExists(SqliteConnection connection, SqliteTransaction transaction, string table, Guid id)
    {
        using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = $"SELECT COUNT(*) FROM {table} WHERE ID=$id"; command.Parameters.AddWithValue("$id", id.ToString());
        return Convert.ToInt64(command.ExecuteScalar()) != 0;
    }

    private static void SaveClusters(SqliteConnection connection, SqliteTransaction transaction,
        IEnumerable<Model.Cluster> clusters, ClusterBatchRestoreConflictPolicy policy)
    {
        foreach (Model.Cluster cluster in clusters)
        {
            using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = policy == ClusterBatchRestoreConflictPolicy.ReplaceExisting
                ? "INSERT INTO ClusterTable (ID,MetaInfo,FieldID,IsSingleWell,RigID,IsFixedPlatform,Cluster) VALUES ($id,$meta,$field,$single,$rig,$fixed,$doc) ON CONFLICT(ID) DO UPDATE SET MetaInfo=excluded.MetaInfo,FieldID=excluded.FieldID,IsSingleWell=excluded.IsSingleWell,RigID=excluded.RigID,IsFixedPlatform=excluded.IsFixedPlatform,Cluster=excluded.Cluster"
                : "INSERT INTO ClusterTable (ID,MetaInfo,FieldID,IsSingleWell,RigID,IsFixedPlatform,Cluster) VALUES ($id,$meta,$field,$single,$rig,$fixed,$doc)";
            command.Parameters.AddWithValue("$id", cluster.MetaInfo!.ID.ToString());
            command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(cluster.MetaInfo, JsonSettings.Options));
            command.Parameters.AddWithValue("$field", cluster.FieldID?.ToString() ?? "");
            command.Parameters.AddWithValue("$single", cluster.IsSingleWell ? 1 : 0);
            command.Parameters.AddWithValue("$rig", cluster.RigID?.ToString() ?? "");
            command.Parameters.AddWithValue("$fixed", cluster.IsFixedPlatform ? 1 : 0);
            command.Parameters.AddWithValue("$doc", JsonSerializer.Serialize(cluster, JsonSettings.Options));
            command.ExecuteNonQuery();
        }
    }

    private static string Normalize(string? value) => string.Join(' ', (value ?? string.Empty).Normalize(NormalizationForm.FormKC)
        .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
    private static bool SameName(string? left, string? right) => Normalize(left) == Normalize(right);
    private static void AddMissing(List<ClusterBatchError> errors, string kind, Guid id, string? name) => errors.Add(Error(null, $"Document.CatalogDependencies[{id}]", "catalog_definition_missing", $"No compatible local {kind} exists for '{name}' ({id}), and creation is disabled."));
    private static void AddAmbiguous(List<ClusterBatchError> errors, string kind, Guid id, string? name) => errors.Add(Error(null, $"Document.CatalogDependencies[{id}]", "ambiguous_catalog_match", $"More than one local {kind} has normalized name '{name}' for source UUID '{id}'."));
    private static void AddSemanticConflict(List<ClusterBatchError> errors, string kind, Guid id, string? name) => errors.Add(Error(null, $"Document.CatalogDependencies[{id}]", "catalog_semantic_conflict", $"The local {kind} corresponding to '{name}' ({id}) has incompatible semantics."));
    private static void AddMapping(List<ClusterBatchCatalogMapping> mappings, string catalog, string? name, Guid source, Guid local, string resolution) => mappings.Add(new() { Catalog = catalog, Name = name ?? string.Empty, SourceID = source, LocalID = local, Resolution = resolution });
    private static ClusterBatchRestoreOutcome Failure(ClusterBatchRestoreFailureKind kind, string error, string message, List<ClusterBatchError> errors) => new() { FailureKind = kind, Error = new() { Error = error, Message = message, Errors = errors } };
    private static ClusterBatchError Error(int? index, string property, string code, string message) => new() { PositionIndex = index, Property = property, Code = code, Message = message };

    private sealed class CatalogState
    {
        public List<ClusterIdentity> Identities { get; } = [];
        public List<ClusterFeatureCategory> ClusterCategories { get; } = [];
        public List<SlotFeatureCategory> SlotCategories { get; } = [];
        public HashSet<ClusterIdentity> DirtyIdentities { get; } = [];
        public HashSet<ClusterFeatureCategory> DirtyClusterCategories { get; } = [];
        public HashSet<SlotFeatureCategory> DirtySlotCategories { get; } = [];

        public static CatalogState Load(SqliteConnection connection, SqliteTransaction transaction)
        {
            CatalogState state = new();
            state.Identities.AddRange(Read<ClusterIdentity>(connection, transaction, "ClusterIdentityTable", "ClusterIdentity"));
            state.ClusterCategories.AddRange(Read<ClusterFeatureCategory>(connection, transaction, "ClusterFeatureCategoryTable", "ClusterFeatureCategory"));
            state.SlotCategories.AddRange(Read<SlotFeatureCategory>(connection, transaction, "SlotFeatureCategoryTable", "SlotFeatureCategory"));
            return state;
        }

        private static List<T> Read<T>(SqliteConnection connection, SqliteTransaction transaction, string table, string column)
        {
            using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = $"SELECT {column} FROM {table}"; using SqliteDataReader reader = command.ExecuteReader();
            List<T> result = []; while (reader.Read()) result.Add(JsonSerializer.Deserialize<T>(reader.GetString(0), JsonSettings.Options) ?? throw new JsonException($"Invalid {table} document."));
            return result;
        }

        public void Save(SqliteConnection connection, SqliteTransaction transaction)
        {
            foreach (ClusterIdentity value in DirtyIdentities)
                InsertIdentity(connection, transaction, value);
            foreach (ClusterFeatureCategory value in DirtyClusterCategories)
                UpsertCategory(connection, transaction, "ClusterFeatureCategoryTable", "ClusterFeatureCategory", value.MetaInfo!, value.Name, value.IsExclusive, value.HasValidityPeriod, value.CreationDate, value.LastModificationDate, value);
            foreach (SlotFeatureCategory value in DirtySlotCategories)
                UpsertCategory(connection, transaction, "SlotFeatureCategoryTable", "SlotFeatureCategory", value.MetaInfo!, value.Name, value.IsExclusive, value.HasValidityPeriod, value.CreationDate, value.LastModificationDate, value);
        }

        private static void InsertIdentity(SqliteConnection connection, SqliteTransaction transaction, ClusterIdentity value)
        {
            using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "INSERT INTO ClusterIdentityTable (ID,MetaInfo,Name,CreationDate,LastModificationDate,ClusterIdentity) VALUES ($id,$meta,$name,$created,$modified,$doc)";
            AddCommon(command, value.MetaInfo!, value.Name, value.CreationDate, value.LastModificationDate, value); command.ExecuteNonQuery();
        }

        private static void UpsertCategory(SqliteConnection connection, SqliteTransaction transaction, string table,
            string column, MetaInfo meta, string? name, bool exclusive, bool validity,
            DateTimeOffset? created, DateTimeOffset? modified, object document)
        {
            using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = $"INSERT INTO {table} (ID,MetaInfo,Name,IsExclusive,HasValidityPeriod,CreationDate,LastModificationDate,{column}) VALUES ($id,$meta,$name,$exclusive,$validity,$created,$modified,$doc) ON CONFLICT(ID) DO UPDATE SET MetaInfo=excluded.MetaInfo,Name=excluded.Name,IsExclusive=excluded.IsExclusive,HasValidityPeriod=excluded.HasValidityPeriod,CreationDate=excluded.CreationDate,LastModificationDate=excluded.LastModificationDate,{column}=excluded.{column}";
            AddCommon(command, meta, name, created, modified, document);
            command.Parameters.AddWithValue("$exclusive", exclusive ? 1 : 0); command.Parameters.AddWithValue("$validity", validity ? 1 : 0); command.ExecuteNonQuery();
        }

        private static void AddCommon(SqliteCommand command, MetaInfo meta, string? name,
            DateTimeOffset? created, DateTimeOffset? modified, object document)
        {
            command.Parameters.AddWithValue("$id", meta.ID.ToString());
            command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(meta, JsonSettings.Options));
            command.Parameters.AddWithValue("$name", name ?? string.Empty);
            command.Parameters.AddWithValue("$created", created?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? string.Empty);
            command.Parameters.AddWithValue("$modified", modified?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? string.Empty);
            command.Parameters.AddWithValue("$doc", JsonSerializer.Serialize(document, JsonSettings.Options));
        }
    }
}
