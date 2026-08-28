using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Cluster.Service.Controllers;
using NORCE.Drilling.Cluster.Service.Managers;
using ClusterModel = NORCE.Drilling.Cluster.Model.Cluster;
using ClusterFeatureCategoryModel = NORCE.Drilling.Cluster.Model.ClusterFeatureCategory;
using ClusterIdentityModel = NORCE.Drilling.Cluster.Model.ClusterIdentity;
using SlotFeatureCategoryModel = NORCE.Drilling.Cluster.Model.SlotFeatureCategory;

namespace NORCE.Drilling.Cluster.Service.Mcp.Tools;

public static class ClusterRestMcpToolRegistrations
{
    public static IServiceCollection AddClusterRestMcpTools(this IServiceCollection services)
    {
        AddClusterTools(services);
        AddClusterFeatureCategoryTools(services);
        AddClusterIdentityTools(services);
        AddSlotFeatureCategoryTools(services);
        AddUsageStatisticsTool(services);
        return services;
    }

    private static void AddClusterTools(IServiceCollection services)
    {
        services.AddLegacyMcpTool("cluster_get_all_ids", "List the UUID of every stored cluster without transferring complete records. Use these identifiers with cluster_get_by_id or other services that reference a cluster.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => ClusterController(sp).GetAllClusterId()));
        services.AddLegacyMcpTool("cluster_get_all_meta_info", "List identity and HTTP location metadata for every stored cluster without returning complete cluster data. Each result contains the cluster ID and may contain its host, base path, and endpoint.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => ClusterController(sp).GetAllClusterMetaInfo()));
        services.AddLegacyMcpTool("cluster_get_by_id", "Retrieve one complete cluster record by UUID, including field and rig associations, platform flags, identities, feature assignments, WGS84 reference data, depth uncertainty, and slots. Returns 404 when it does not exist and 400 for an empty UUID.", McpToolArgumentHelpers.CreateGuidSchema("id", "Unique identifier of the cluster to retrieve."),
            (sp, args, ct) => InvokeByGuidArgument(args, "id", ct, id => ClusterController(sp).GetClusterById(id)));
        services.AddLegacyMcpTool("cluster_get_all", "Retrieve every stored cluster as a complete record, including nested slots and assignments. Use cluster_get_all_light, cluster_get_all_ids, or cluster_get_all_meta_info when full nested data is unnecessary.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => ClusterController(sp).GetAllCluster()));
        services.AddLegacyMcpTool("cluster_get_all_light", "Retrieve lightweight records for every cluster. Results retain identity, field and rig associations, platform flags, reference point, and WGS84 depths while omitting nested identities, feature assignments, and slots.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => ClusterController(sp).GetAllClusterLight()));
        services.AddLegacyMcpTool("cluster_get_all_by_field_id", "Retrieve complete records for all clusters whose FieldID equals the supplied field UUID. An empty result means no stored cluster currently references that field.", McpToolArgumentHelpers.CreateGuidSchema("fieldId", "Identifier of the Field resource whose clusters should be returned."),
            (sp, args, ct) => InvokeByGuidArgument(args, "fieldId", ct, id => ClusterController(sp).GetAllClusterByFieldId(id)));
        services.AddLegacyMcpTool("cluster_get_all_by_rig_id", "Retrieve complete records for all clusters whose RigID equals the supplied rig UUID. An empty result means no stored cluster currently references that rig.", McpToolArgumentHelpers.CreateGuidSchema("rigId", "Identifier of the Rig resource whose associated clusters should be returned."),
            (sp, args, ct) => InvokeByGuidArgument(args, "rigId", ct, id => ClusterController(sp).GetAllClusterByRigId(id)));
        services.AddLegacyMcpTool("cluster_get_all_single_well", "Retrieve complete cluster records filtered by IsSingleWell. Pass true for records representing one well rather than a true multi-well cluster; pass false for multi-well clusters.", McpToolArgumentHelpers.CreateBooleanSchema("isSingleWell", "Required IsSingleWell value to match: true for single-well records, false for multi-well clusters."),
            (sp, args, ct) => InvokeByBoolArgument(args, "isSingleWell", ct, value => ClusterController(sp).GetAllSingleWellCluster(value)));
        services.AddLegacyMcpTool("cluster_get_all_fixed_platform", "Retrieve complete cluster records filtered by IsFixedPlatform. Pass true for fixed installations and false for clusters associated with floating or movable installations.", McpToolArgumentHelpers.CreateBooleanSchema("isFixedPlatform", "Required IsFixedPlatform value to match: true for fixed platforms, false for floating or movable installations."),
            (sp, args, ct) => InvokeByBoolArgument(args, "isFixedPlatform", ct, value => ClusterController(sp).GetAllFixedPlatformCluster(value)));
        services.AddLegacyMcpTool("cluster_create", "Create and persist a complete cluster record. cluster.MetaInfo.ID must be a caller-generated, non-empty UUID that is not already stored. Coordinates use SI and WGS84 references; depth and coordinate uncertainty is represented by Gaussian values. Returns 200 on success, 400 for malformed data, and 409 for a duplicate ID.", McpToolArgumentHelpers.CreateClusterSchema(),
            (sp, args, ct) => InvokeWithBody<ClusterModel>(args, "cluster", ct, data => ClusterController(sp).PostCluster(data)));
        services.AddLegacyMcpTool("cluster_update_by_id", "Replace an existing cluster with the complete supplied record. The top-level id must equal cluster.MetaInfo.ID; this is a full update, not a partial patch, so include all data that should remain stored. Returns 200 on success, 400 for malformed or mismatched IDs, and 404 when absent.", McpToolArgumentHelpers.CreateClusterSchema(includeId: true),
            (sp, args, ct) => InvokeWithIdAndBody<ClusterModel>(args, "cluster", ct, (id, data) => ClusterController(sp).PutClusterById(id, data)));
        services.AddLegacyMcpTool("cluster_delete_by_id", "Permanently delete one stored cluster by UUID. Confirm the target and consider services that reference the cluster before calling; the operation removes its persisted cluster record, including nested slots. Returns 200 on success and 404 when absent.", McpToolArgumentHelpers.CreateGuidSchema("id", "Unique identifier of the cluster to delete."),
            (sp, args, ct) => InvokeDelete(args, ct, id => ClusterController(sp).DeleteClusterById(id)));
    }

    private static void AddClusterFeatureCategoryTools(IServiceCollection services)
    {
        AddCrudTools<ClusterFeatureCategoryModel>(
            services,
            "cluster_feature_category",
            "clusterFeatureCategory",
            "cluster feature category",
            "a definition of allowed feature options that can be assigned to clusters",
            McpToolArgumentHelpers.CreateClusterFeatureCategorySchema,
            sp => ClusterFeatureCategoryController(sp).GetAllClusterFeatureCategoryId(),
            sp => ClusterFeatureCategoryController(sp).GetAllClusterFeatureCategoryMetaInfo(),
            (sp, id) => ClusterFeatureCategoryController(sp).GetClusterFeatureCategoryById(id),
            sp => ClusterFeatureCategoryController(sp).GetAllClusterFeatureCategory(),
            (sp, data) => ClusterFeatureCategoryController(sp).PostClusterFeatureCategory(data),
            (sp, id, data) => ClusterFeatureCategoryController(sp).PutClusterFeatureCategoryById(id, data),
            (sp, id) => ClusterFeatureCategoryController(sp).DeleteClusterFeatureCategoryById(id));
    }

    private static void AddClusterIdentityTools(IServiceCollection services)
    {
        AddCrudTools<ClusterIdentityModel>(
            services,
            "cluster_identity",
            "clusterIdentity",
            "cluster identity",
            "a symbolic identity definition whose values can be assigned to individual clusters",
            McpToolArgumentHelpers.CreateClusterIdentitySchema,
            sp => ClusterIdentityController(sp).GetAllClusterIdentityId(),
            sp => ClusterIdentityController(sp).GetAllClusterIdentityMetaInfo(),
            (sp, id) => ClusterIdentityController(sp).GetClusterIdentityById(id),
            sp => ClusterIdentityController(sp).GetAllClusterIdentity(),
            (sp, data) => ClusterIdentityController(sp).PostClusterIdentity(data),
            (sp, id, data) => ClusterIdentityController(sp).PutClusterIdentityById(id, data),
            (sp, id) => ClusterIdentityController(sp).DeleteClusterIdentityById(id));
    }

    private static void AddSlotFeatureCategoryTools(IServiceCollection services)
    {
        AddCrudTools<SlotFeatureCategoryModel>(
            services,
            "slot_feature_category",
            "slotFeatureCategory",
            "slot feature category",
            "a definition of allowed feature options that can be assigned to slots within clusters",
            McpToolArgumentHelpers.CreateSlotFeatureCategorySchema,
            sp => SlotFeatureCategoryController(sp).GetAllSlotFeatureCategoryId(),
            sp => SlotFeatureCategoryController(sp).GetAllSlotFeatureCategoryMetaInfo(),
            (sp, id) => SlotFeatureCategoryController(sp).GetSlotFeatureCategoryById(id),
            sp => SlotFeatureCategoryController(sp).GetAllSlotFeatureCategory(),
            (sp, data) => SlotFeatureCategoryController(sp).PostSlotFeatureCategory(data),
            (sp, id, data) => SlotFeatureCategoryController(sp).PutSlotFeatureCategoryById(id, data),
            (sp, id) => SlotFeatureCategoryController(sp).DeleteSlotFeatureCategoryById(id));
    }

    private static void AddUsageStatisticsTool(IServiceCollection services)
    {
        services.AddLegacyMcpTool("cluster_usage_statistics_get", "Retrieve the Cluster microservice usage counters collected for REST operations. This administrative result reports endpoint activity rather than cluster domain data and requires no arguments.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => ClusterUsageStatisticsController(sp).GetClusterUsageStatistics()));
    }

    private static void AddCrudTools<TModel>(
        IServiceCollection services,
        string prefix,
        string bodyName,
        string entityName,
        string entityPurpose,
        Func<bool, JsonObject> schemaFactory,
        Func<IServiceProvider, ActionResult<System.Collections.Generic.IEnumerable<Guid>>> getAllIds,
        Func<IServiceProvider, ActionResult<System.Collections.Generic.IEnumerable<OSDC.DotnetLibraries.General.DataManagement.MetaInfo?>>> getAllMetaInfo,
        Func<IServiceProvider, Guid, ActionResult<TModel?>> getById,
        Func<IServiceProvider, ActionResult<System.Collections.Generic.IEnumerable<TModel?>>> getAll,
        Func<IServiceProvider, TModel?, ActionResult> create,
        Func<IServiceProvider, Guid, TModel?, ActionResult> update,
        Func<IServiceProvider, Guid, ActionResult> delete)
    {
        services.AddLegacyMcpTool($"{prefix}_get_all_ids", $"List the UUID of every stored {entityName} without transferring complete records. These IDs identify {entityPurpose} and can be passed to {prefix}_get_by_id.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => getAllIds(sp)));
        services.AddLegacyMcpTool($"{prefix}_get_all_meta_info", $"List identity and optional HTTP location metadata for every stored {entityName} without returning complete definitions. Use this for resource discovery when full content is unnecessary.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => getAllMetaInfo(sp)));
        services.AddLegacyMcpTool($"{prefix}_get_by_id", $"Retrieve one complete {entityName} by UUID. The record represents {entityPurpose}. Returns status 404 when no matching record exists and 400 for an empty UUID.", McpToolArgumentHelpers.CreateGuidSchema("id", $"Unique identifier of the {entityName} to retrieve."),
            (sp, args, ct) => InvokeByGuidArgument(args, "id", ct, id => getById(sp, id)));
        services.AddLegacyMcpTool($"{prefix}_get_all", $"Retrieve every stored {entityName} as a complete definition. Each result represents {entityPurpose}; use the ID or metadata listing tools when complete content is unnecessary.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => getAll(sp)));
        services.AddLegacyMcpTool($"{prefix}_create", $"Create and persist {entityPurpose}. Supply the complete {bodyName} object; {bodyName}.MetaInfo.ID must be a caller-generated, non-empty UUID that is not already stored. Returns 200 on success, 400 for malformed data, and 409 for a duplicate ID.", schemaFactory(false),
            (sp, args, ct) => InvokeWithBody<TModel>(args, bodyName, ct, data => create(sp, data)));
        services.AddLegacyMcpTool($"{prefix}_update_by_id", $"Replace an existing {entityName} with the complete supplied definition. The top-level id must equal {bodyName}.MetaInfo.ID; this is a full update rather than a partial patch. Returns 200 on success, 400 for malformed or mismatched IDs, and 404 when absent.", schemaFactory(true),
            (sp, args, ct) => InvokeWithIdAndBody<TModel>(args, bodyName, ct, (id, data) => update(sp, id, data)));
        services.AddLegacyMcpTool($"{prefix}_delete_by_id", $"Permanently delete one stored {entityName} by UUID. Check assignments that may still reference this definition before deleting it. Returns 200 on success and 404 when no matching record exists.", McpToolArgumentHelpers.CreateGuidSchema("id", $"Unique identifier of the {entityName} to delete."),
            (sp, args, ct) => InvokeDelete(args, ct, id => delete(sp, id)));
    }

    private static Task<JsonNode?> Invoke<T>(CancellationToken cancellationToken, Func<ActionResult<T>> action)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action()));
    }

    private static Task<JsonNode?> InvokeByGuidArgument<T>(JsonObject? arguments, string argumentName, CancellationToken cancellationToken, Func<Guid, ActionResult<T>> action)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!McpToolArgumentHelpers.TryParseGuid(arguments, argumentName, out Guid id, out JsonNode? error))
        {
            return Task.FromResult<JsonNode?>(error);
        }
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id)));
    }

    private static Task<JsonNode?> InvokeByBoolArgument<T>(JsonObject? arguments, string argumentName, CancellationToken cancellationToken, Func<bool, ActionResult<T>> action)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!McpToolArgumentHelpers.TryParseBool(arguments, argumentName, out bool value, out JsonNode? error))
        {
            return Task.FromResult<JsonNode?>(error);
        }
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(value)));
    }

    private static Task<JsonNode?> InvokeDelete(JsonObject? arguments, CancellationToken cancellationToken, Func<Guid, ActionResult> action)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!McpToolArgumentHelpers.TryParseGuid(arguments, "id", out Guid id, out JsonNode? error))
        {
            return Task.FromResult<JsonNode?>(error);
        }
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id)));
    }

    private static Task<JsonNode?> InvokeWithBody<TModel>(JsonObject? arguments, string bodyName, CancellationToken cancellationToken, Func<TModel?, ActionResult> action)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryDeserialize(arguments, bodyName, out TModel? data, out JsonNode? error))
        {
            return Task.FromResult<JsonNode?>(error);
        }
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(data)));
    }

    private static Task<JsonNode?> InvokeWithIdAndBody<TModel>(JsonObject? arguments, string bodyName, CancellationToken cancellationToken, Func<Guid, TModel?, ActionResult> action)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!McpToolArgumentHelpers.TryParseGuid(arguments, "id", out Guid id, out JsonNode? idError))
        {
            return Task.FromResult<JsonNode?>(idError);
        }
        if (!TryDeserialize(arguments, bodyName, out TModel? data, out JsonNode? dataError))
        {
            return Task.FromResult<JsonNode?>(dataError);
        }
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id, data)));
    }

    private static bool TryDeserialize<TModel>(JsonObject? arguments, string bodyName, out TModel? data, out JsonNode? error)
    {
        data = default;
        error = null;

        if (arguments?[bodyName] is not JsonNode node)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{bodyName}' is required.");
            return false;
        }

        try
        {
            data = node.Deserialize<TModel>(JsonSettings.Options);
            if (data is null)
            {
                throw new InvalidOperationException();
            }
            return true;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{bodyName}' could not be deserialized.");
            return false;
        }
    }

    private static ClusterController ClusterController(IServiceProvider sp) =>
        new(sp.GetRequiredService<ILogger<ClusterManager>>(), sp.GetRequiredService<SqlConnectionManager>());

    private static ClusterFeatureCategoryController ClusterFeatureCategoryController(IServiceProvider sp) =>
        new(sp.GetRequiredService<ILogger<ClusterFeatureCategoryManager>>(), sp.GetRequiredService<SqlConnectionManager>());

    private static ClusterIdentityController ClusterIdentityController(IServiceProvider sp) =>
        new(sp.GetRequiredService<ILogger<ClusterIdentityManager>>(), sp.GetRequiredService<SqlConnectionManager>());

    private static SlotFeatureCategoryController SlotFeatureCategoryController(IServiceProvider sp) =>
        new(sp.GetRequiredService<ILogger<SlotFeatureCategoryManager>>(), sp.GetRequiredService<SqlConnectionManager>());

    private static ClusterUsageStatisticsController ClusterUsageStatisticsController(IServiceProvider sp) =>
        new(sp.GetRequiredService<ILogger<ClusterUsageStatisticsController>>());
}
