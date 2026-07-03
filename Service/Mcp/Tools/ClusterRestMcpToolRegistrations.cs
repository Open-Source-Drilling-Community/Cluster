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

internal static class ClusterRestMcpToolRegistrations
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
        services.AddLegacyMcpTool("cluster.get_all_ids", "Retrieve all cluster identifiers.", null,
            (sp, _, ct) => Invoke(ct, () => ClusterController(sp).GetAllClusterId()));
        services.AddLegacyMcpTool("cluster.get_all_meta_info", "Retrieve metadata for all clusters.", null,
            (sp, _, ct) => Invoke(ct, () => ClusterController(sp).GetAllClusterMetaInfo()));
        services.AddLegacyMcpTool("cluster.get_by_id", "Retrieve a cluster by identifier.", McpToolArgumentHelpers.CreateGuidSchema("id"),
            (sp, args, ct) => InvokeByGuidArgument(args, "id", ct, id => ClusterController(sp).GetClusterById(id)));
        services.AddLegacyMcpTool("cluster.get_all", "Retrieve all clusters with full data.", null,
            (sp, _, ct) => Invoke(ct, () => ClusterController(sp).GetAllCluster()));
        services.AddLegacyMcpTool("cluster.get_all_light", "Retrieve all clusters as lightweight records.", null,
            (sp, _, ct) => Invoke(ct, () => ClusterController(sp).GetAllClusterLight()));
        services.AddLegacyMcpTool("cluster.get_all_by_field_id", "Retrieve all clusters linked to a field identifier.", McpToolArgumentHelpers.CreateGuidSchema("fieldId"),
            (sp, args, ct) => InvokeByGuidArgument(args, "fieldId", ct, id => ClusterController(sp).GetAllClusterByFieldId(id)));
        services.AddLegacyMcpTool("cluster.get_all_by_rig_id", "Retrieve all clusters linked to a rig identifier.", McpToolArgumentHelpers.CreateGuidSchema("rigId"),
            (sp, args, ct) => InvokeByGuidArgument(args, "rigId", ct, id => ClusterController(sp).GetAllClusterByRigId(id)));
        services.AddLegacyMcpTool("cluster.get_all_single_well", "Retrieve clusters filtered by single-well status.", McpToolArgumentHelpers.CreateBooleanSchema("isSingleWell"),
            (sp, args, ct) => InvokeByBoolArgument(args, "isSingleWell", ct, value => ClusterController(sp).GetAllSingleWellCluster(value)));
        services.AddLegacyMcpTool("cluster.get_all_fixed_platform", "Retrieve clusters filtered by fixed-platform status.", McpToolArgumentHelpers.CreateBooleanSchema("isFixedPlatform"),
            (sp, args, ct) => InvokeByBoolArgument(args, "isFixedPlatform", ct, value => ClusterController(sp).GetAllFixedPlatformCluster(value)));
        services.AddLegacyMcpTool("cluster.create", "Create a cluster.", McpToolArgumentHelpers.CreateObjectSchema("cluster"),
            (sp, args, ct) => InvokeWithBody<ClusterModel>(args, "cluster", ct, data => ClusterController(sp).PostCluster(data)));
        services.AddLegacyMcpTool("cluster.update_by_id", "Update an existing cluster identified by id.", McpToolArgumentHelpers.CreateObjectSchema("cluster", includeId: true),
            (sp, args, ct) => InvokeWithIdAndBody<ClusterModel>(args, "cluster", ct, (id, data) => ClusterController(sp).PutClusterById(id, data)));
        services.AddLegacyMcpTool("cluster.delete_by_id", "Delete a cluster by identifier.", McpToolArgumentHelpers.CreateGuidSchema("id"),
            (sp, args, ct) => InvokeDelete(args, ct, id => ClusterController(sp).DeleteClusterById(id)));
    }

    private static void AddClusterFeatureCategoryTools(IServiceCollection services)
    {
        AddCrudTools<ClusterFeatureCategoryModel>(
            services,
            "cluster_feature_category",
            "clusterFeatureCategory",
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
        services.AddLegacyMcpTool("cluster_usage_statistics.get", "Retrieve usage statistics for the Cluster microservice.", null,
            (sp, _, ct) => Invoke(ct, () => ClusterUsageStatisticsController(sp).GetClusterUsageStatistics()));
    }

    private static void AddCrudTools<TModel>(
        IServiceCollection services,
        string prefix,
        string bodyName,
        Func<IServiceProvider, ActionResult<System.Collections.Generic.IEnumerable<Guid>>> getAllIds,
        Func<IServiceProvider, ActionResult<System.Collections.Generic.IEnumerable<OSDC.DotnetLibraries.General.DataManagement.MetaInfo?>>> getAllMetaInfo,
        Func<IServiceProvider, Guid, ActionResult<TModel?>> getById,
        Func<IServiceProvider, ActionResult<System.Collections.Generic.IEnumerable<TModel?>>> getAll,
        Func<IServiceProvider, TModel?, ActionResult> create,
        Func<IServiceProvider, Guid, TModel?, ActionResult> update,
        Func<IServiceProvider, Guid, ActionResult> delete)
    {
        services.AddLegacyMcpTool($"{prefix}.get_all_ids", $"Retrieve all {prefix} identifiers.", null,
            (sp, _, ct) => Invoke(ct, () => getAllIds(sp)));
        services.AddLegacyMcpTool($"{prefix}.get_all_meta_info", $"Retrieve metadata for all {prefix} records.", null,
            (sp, _, ct) => Invoke(ct, () => getAllMetaInfo(sp)));
        services.AddLegacyMcpTool($"{prefix}.get_by_id", $"Retrieve a {prefix} record by identifier.", McpToolArgumentHelpers.CreateGuidSchema("id"),
            (sp, args, ct) => InvokeByGuidArgument(args, "id", ct, id => getById(sp, id)));
        services.AddLegacyMcpTool($"{prefix}.get_all", $"Retrieve all {prefix} records with full data.", null,
            (sp, _, ct) => Invoke(ct, () => getAll(sp)));
        services.AddLegacyMcpTool($"{prefix}.create", $"Create a {prefix} record.", McpToolArgumentHelpers.CreateObjectSchema(bodyName),
            (sp, args, ct) => InvokeWithBody<TModel>(args, bodyName, ct, data => create(sp, data)));
        services.AddLegacyMcpTool($"{prefix}.update_by_id", $"Update an existing {prefix} record identified by id.", McpToolArgumentHelpers.CreateObjectSchema(bodyName, includeId: true),
            (sp, args, ct) => InvokeWithIdAndBody<TModel>(args, bodyName, ct, (id, data) => update(sp, id, data)));
        services.AddLegacyMcpTool($"{prefix}.delete_by_id", $"Delete a {prefix} record by identifier.", McpToolArgumentHelpers.CreateGuidSchema("id"),
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
