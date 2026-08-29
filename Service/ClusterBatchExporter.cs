using System;
using System.Collections.Generic;
using System.Linq;
using OSDC.Drilling.Cluster.Model;

namespace OSDC.Drilling.Cluster.Service;

public enum ClusterBatchExportFailureKind { None, InvalidRequest, ClusterNotFound, StorageFailure }

public sealed class ClusterBatchExportOutcome
{
    public ClusterBatchExportDocument? Document { get; init; }
    public ClusterBatchErrorEnvelope? Error { get; init; }
    public ClusterBatchExportFailureKind FailureKind { get; init; }
    public bool IsSuccess => Document != null && FailureKind == ClusterBatchExportFailureKind.None;
}

public static class ClusterBatchExporter
{
    public static ClusterBatchExportOutcome StorageFailure(string message) => Failure(
        ClusterBatchExportFailureKind.StorageFailure, "cluster_export_failed", message,
        [Error(null, "Document", "storage_failure", "The export snapshot could not be produced.")]);

    public static ClusterBatchExportOutcome Create(
        ClusterBatchExportRequest? request,
        IEnumerable<Model.Cluster?> snapshot,
        DateTimeOffset exportedAtUtc,
        IEnumerable<ClusterIdentity> identities,
        IEnumerable<ClusterFeatureCategory> clusterFeatureCategories,
        IEnumerable<SlotFeatureCategory> slotFeatureCategories)
    {
        List<ClusterBatchError> errors = ValidateRequest(request);
        if (errors.Count != 0)
            return Failure(ClusterBatchExportFailureKind.InvalidRequest, "invalid_batch_export_request",
                "The cluster batch-export request is invalid.", errors);

        Dictionary<Guid, Model.Cluster> byId = [];
        int position = 0;
        foreach (Model.Cluster? cluster in snapshot)
        {
            Guid? id = cluster?.MetaInfo?.ID;
            if (cluster == null || id == null || id == Guid.Empty || !byId.TryAdd(id.Value, cluster))
                return Failure(ClusterBatchExportFailureKind.StorageFailure, "cluster_export_failed",
                    "A stored cluster could not be represented in the export.",
                    [Error(position, "Clusters", "invalid_stored_cluster", "A stored cluster is null, has no UUID, or duplicates another UUID.")]);
            position++;
        }

        List<Model.Cluster> selected;
        if (request!.Scope == ClusterBatchExportScope.All)
            selected = byId.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToList();
        else
        {
            selected = [];
            for (int index = 0; index < request.ClusterIDs!.Count; index++)
            {
                Guid id = request.ClusterIDs[index];
                if (byId.TryGetValue(id, out Model.Cluster? cluster)) selected.Add(cluster);
                else errors.Add(Error(index, "ClusterIDs", "cluster_not_found", $"No stored cluster has UUID '{id}'."));
            }
            if (errors.Count != 0)
                return Failure(ClusterBatchExportFailureKind.ClusterNotFound, "cluster_not_found",
                    "The selected batch could not be exported because one or more clusters do not exist.", errors);
        }

        ClusterBatchCatalogDependencies dependencies = BuildDependencies(selected, identities,
            clusterFeatureCategories, slotFeatureCategories, errors);
        if (errors.Count != 0)
            return Failure(ClusterBatchExportFailureKind.StorageFailure, "cluster_export_dependency_missing",
                "The export could not include every referenced local catalog definition.", errors);

        return new ClusterBatchExportOutcome
        {
            Document = new ClusterBatchExportDocument
            {
                ExportedAtUtc = exportedAtUtc.ToUniversalTime(),
                CatalogDependencies = dependencies,
                Clusters = selected
            }
        };
    }

    private static ClusterBatchCatalogDependencies BuildDependencies(
        IReadOnlyList<Model.Cluster> clusters,
        IEnumerable<ClusterIdentity> identities,
        IEnumerable<ClusterFeatureCategory> clusterCategories,
        IEnumerable<SlotFeatureCategory> slotCategories,
        List<ClusterBatchError> errors)
    {
        Dictionary<Guid, ClusterIdentity> identityIndex = Index(identities, value => value.MetaInfo?.ID);
        Dictionary<Guid, ClusterFeatureCategory> clusterIndex = Index(clusterCategories, value => value.MetaInfo?.ID);
        Dictionary<Guid, SlotFeatureCategory> slotIndex = Index(slotCategories, value => value.MetaInfo?.ID);
        HashSet<Guid> identityIds = [];
        Dictionary<Guid, HashSet<Guid>> clusterOptions = [];
        Dictionary<Guid, HashSet<Guid>> slotOptions = [];

        for (int index = 0; index < clusters.Count; index++)
        {
            foreach (ClusterIdentityAssignment assignment in clusters[index].ClusterIdentityAssignments ?? [])
                AddFlatReference(assignment.IdentityID, identityIds, index, "ClusterIdentityAssignments.IdentityID", errors);
            foreach (ClusterFeatureAssignment assignment in clusters[index].ClusterFeatureAssignments ?? [])
                AddCategoryReference(assignment.FeatureCategoryID, assignment.FeatureOptionID, clusterOptions, index,
                    "ClusterFeatureAssignments", errors);
            foreach (Slot slot in clusters[index].Slots?.Values ?? Enumerable.Empty<Slot>())
                foreach (SlotFeatureAssignment assignment in slot.SlotFeatureAssignments ?? [])
                    AddCategoryReference(assignment.FeatureCategoryID, assignment.FeatureOptionID, slotOptions, index,
                        "Slots.SlotFeatureAssignments", errors);
        }

        ClusterBatchCatalogDependencies result = new();
        foreach (Guid id in identityIds.Order())
        {
            if (identityIndex.TryGetValue(id, out ClusterIdentity? value)) result.Identities.Add(value);
            else errors.Add(Error(null, "CatalogDependencies.Identities", "referenced_definition_missing", $"Referenced cluster identity '{id}' does not exist."));
        }
        foreach ((Guid id, HashSet<Guid> optionIds) in clusterOptions.OrderBy(pair => pair.Key))
            AddCategoryDependency(id, optionIds, clusterIndex, result.ClusterFeatureCategories,
                "cluster feature", "CatalogDependencies.ClusterFeatureCategories", errors);
        foreach ((Guid id, HashSet<Guid> optionIds) in slotOptions.OrderBy(pair => pair.Key))
            AddCategoryDependency(id, optionIds, slotIndex, result.SlotFeatureCategories,
                "slot feature", "CatalogDependencies.SlotFeatureCategories", errors);
        return result;
    }

    private static void AddCategoryDependency<TCategory>(Guid id, HashSet<Guid> requiredOptions,
        Dictionary<Guid, TCategory> index, List<TCategory> target, string kind, string property,
        List<ClusterBatchError> errors)
        where TCategory : class
    {
        if (!index.TryGetValue(id, out TCategory? raw))
        {
            errors.Add(Error(null, property, "referenced_definition_missing", $"Referenced {kind} category '{id}' does not exist."));
            return;
        }

        if (raw is ClusterFeatureCategory cluster)
        {
            Dictionary<Guid, ClusterFeatureOption> options = (cluster.Options ?? []).ToDictionary(value => value.ID);
            List<ClusterFeatureOption> selected = SelectOptions(requiredOptions, options, kind, id, property, errors);
            target.Add((TCategory)(object)new ClusterFeatureCategory
            {
                MetaInfo = cluster.MetaInfo, Name = cluster.Name, IsExclusive = cluster.IsExclusive,
                HasValidityPeriod = cluster.HasValidityPeriod, Options = selected,
                CreationDate = cluster.CreationDate, LastModificationDate = cluster.LastModificationDate
            });
        }
        else if (raw is SlotFeatureCategory slot)
        {
            Dictionary<Guid, SlotFeatureOption> options = (slot.Options ?? []).ToDictionary(value => value.ID);
            List<SlotFeatureOption> selected = SelectOptions(requiredOptions, options, kind, id, property, errors);
            target.Add((TCategory)(object)new SlotFeatureCategory
            {
                MetaInfo = slot.MetaInfo, Name = slot.Name, IsExclusive = slot.IsExclusive,
                HasValidityPeriod = slot.HasValidityPeriod, Options = selected,
                CreationDate = slot.CreationDate, LastModificationDate = slot.LastModificationDate
            });
        }
    }

    private static List<TOption> SelectOptions<TOption>(HashSet<Guid> required, Dictionary<Guid, TOption> available,
        string kind, Guid categoryId, string property, List<ClusterBatchError> errors) where TOption : class
    {
        List<TOption> result = [];
        foreach (Guid optionId in required.Order())
        {
            if (available.TryGetValue(optionId, out TOption? option)) result.Add(option);
            else errors.Add(Error(null, property + ".Options", "referenced_option_missing",
                $"Referenced {kind} option '{optionId}' does not exist in category '{categoryId}'."));
        }
        return result;
    }

    private static Dictionary<Guid, T> Index<T>(IEnumerable<T> values, Func<T, Guid?> id) where T : class =>
        values.Where(value => id(value) is Guid key && key != Guid.Empty)
            .GroupBy(value => id(value)!.Value).ToDictionary(group => group.Key, group => group.First());

    private static void AddFlatReference(Guid? id, HashSet<Guid> target, int position, string property, List<ClusterBatchError> errors)
    {
        if (id is Guid value && value != Guid.Empty) target.Add(value);
        else errors.Add(Error(position, $"Clusters.{property}", "invalid_catalog_reference", "Catalog references must be non-empty UUIDs."));
    }

    private static void AddCategoryReference(Guid? categoryId, Guid? optionId,
        Dictionary<Guid, HashSet<Guid>> target, int position, string property, List<ClusterBatchError> errors)
    {
        if (categoryId is not Guid category || category == Guid.Empty || optionId is not Guid option || option == Guid.Empty)
        {
            errors.Add(Error(position, $"Clusters.{property}", "invalid_catalog_reference", "Category and option references must be non-empty UUIDs."));
            return;
        }
        if (!target.TryGetValue(category, out HashSet<Guid>? options)) target.Add(category, options = []);
        options.Add(option);
    }

    private static List<ClusterBatchError> ValidateRequest(ClusterBatchExportRequest? request)
    {
        if (request == null) return [Error(null, "Request", "required", "A batch-export request is required.")];
        List<ClusterBatchError> errors = [];
        if (request.Scope == ClusterBatchExportScope.All)
        {
            if (request.ClusterIDs is { Count: > 0 }) errors.Add(Error(null, "ClusterIDs", "forbidden", "ClusterIDs must be omitted for an All export."));
        }
        else if (request.Scope == ClusterBatchExportScope.Selected)
        {
            if (request.ClusterIDs == null || request.ClusterIDs.Count == 0) errors.Add(Error(null, "ClusterIDs", "required", "Selected export requires at least one UUID."));
            else
            {
                HashSet<Guid> ids = [];
                for (int index = 0; index < request.ClusterIDs.Count; index++)
                {
                    Guid id = request.ClusterIDs[index];
                    if (id == Guid.Empty) errors.Add(Error(index, "ClusterIDs", "empty_uuid", "Cluster UUIDs must be non-empty."));
                    else if (!ids.Add(id)) errors.Add(Error(index, "ClusterIDs", "duplicate_uuid", $"Cluster UUID '{id}' occurs more than once."));
                }
            }
        }
        else errors.Add(Error(null, "Scope", "invalid_scope", "Scope must be All or Selected."));
        return errors;
    }

    private static ClusterBatchExportOutcome Failure(ClusterBatchExportFailureKind kind, string error, string message, List<ClusterBatchError> errors) =>
        new() { FailureKind = kind, Error = new() { Error = error, Message = message, Errors = errors } };
    private static ClusterBatchError Error(int? index, string property, string code, string message) =>
        new() { PositionIndex = index, Property = property, Code = code, Message = message };
}
