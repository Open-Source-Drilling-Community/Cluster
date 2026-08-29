using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OSDC.Drilling.Cluster.Model;

namespace OSDC.Drilling.Cluster.Service;

public sealed class ClusterExternalReferenceResolutionOutcome
{
    public List<ClusterBatchExternalReferenceMapping> Mappings { get; init; } = [];
    public List<ClusterBatchError> Errors { get; init; } = [];
    public bool IsSuccess => Errors.Count == 0;
}

public interface IClusterExternalReferenceResolver
{
    Task<List<ClusterBatchError>> PopulateExportManifestAsync(ClusterBatchExportDocument document, CancellationToken cancellationToken);
    Task<ClusterExternalReferenceResolutionOutcome> ResolveRestoreManifestAsync(ClusterBatchExportDocument document, CancellationToken cancellationToken);
}

/// <summary>Resolves Field and Rig references live; it never creates or modifies external resources.</summary>
public sealed class ClusterExternalReferenceResolver : IClusterExternalReferenceResolver
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ClusterExternalReferenceResolver(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<List<ClusterBatchError>> PopulateExportManifestAsync(
        ClusterBatchExportDocument document, CancellationToken cancellationToken)
    {
        List<ClusterBatchError> errors = [];
        for (int index = 0; index < document.Clusters.Count; index++)
        {
            if (document.Clusters[index].FieldID == Guid.Empty)
                errors.Add(Error(index, "Document.Clusters.FieldID", "empty_uuid", "FieldID must be null or a non-empty UUID."));
            if (document.Clusters[index].RigID == Guid.Empty)
                errors.Add(Error(index, "Document.Clusters.RigID", "empty_uuid", "RigID must be null or a non-empty UUID."));
        }
        List<Guid?> fieldIds = document.Clusters.Select(value => value.FieldID).ToList();
        List<Guid?> rigIds = document.Clusters.Select(value => value.RigID).ToList();
        IReadOnlyList<ExternalResource> fields = fieldIds.Any(IsReference)
            ? await ReadCatalogAsync("FieldHostURL", "Field/api/Field/LightData", "Field", cancellationToken) : [];
        IReadOnlyList<ExternalResource> rigs = rigIds.Any(IsReference)
            ? await ReadCatalogAsync("RigHostURL", "Rig/api/Rig/LightData", "Rig", cancellationToken) : [];
        document.ExternalReferences = new ClusterBatchExternalReferences
        {
            Fields = BuildManifest(fieldIds, fields, "FieldID", errors),
            Rigs = BuildManifest(rigIds, rigs, "RigID", errors)
        };
        return errors;
    }

    public async Task<ClusterExternalReferenceResolutionOutcome> ResolveRestoreManifestAsync(
        ClusterBatchExportDocument document, CancellationToken cancellationToken)
    {
        List<ClusterBatchError> errors = [];
        List<ClusterBatchExternalReferenceMapping> mappings = [];
        IReadOnlyList<ExternalResource> fields = document.ExternalReferences.Fields.Count != 0
            ? await ReadCatalogAsync("FieldHostURL", "Field/api/Field/LightData", "Field", cancellationToken) : [];
        IReadOnlyList<ExternalResource> rigs = document.ExternalReferences.Rigs.Count != 0
            ? await ReadCatalogAsync("RigHostURL", "Rig/api/Rig/LightData", "Rig", cancellationToken) : [];
        Resolve(document.ExternalReferences.Fields, fields, "Field", mappings, errors);
        Resolve(document.ExternalReferences.Rigs, rigs, "Rig", mappings, errors);
        foreach (IGrouping<(string Resource, Guid LocalID), ClusterBatchExternalReferenceMapping> collision in mappings
                     .GroupBy(value => (value.Resource, value.LocalID)).Where(group => group.Select(value => value.SourceID).Distinct().Count() > 1))
            errors.Add(Error(null, $"Document.ExternalReferences.{collision.Key.Resource}s", "external_reference_collision",
                $"Several source {collision.Key.Resource} UUIDs resolve to destination UUID '{collision.Key.LocalID}'."));
        return new ClusterExternalReferenceResolutionOutcome { Mappings = mappings, Errors = errors };
    }

    private async Task<IReadOnlyList<ExternalResource>> ReadCatalogAsync(
        string configurationKey, string relativePath, string resource, CancellationToken cancellationToken)
    {
        string? host = _configuration[configurationKey];
        if (string.IsNullOrWhiteSpace(host))
            throw new HttpRequestException($"{configurationKey} is not configured for {resource} reference resolution.");

        using HttpClient client = _httpClientFactory.CreateClient(nameof(ClusterExternalReferenceResolver));
        client.BaseAddress = new Uri(EnsureTrailingSlash(host));
        using HttpResponseMessage response = await client.GetAsync(relativePath, cancellationToken);
        response.EnsureSuccessStatusCode();
        List<ExternalResourceDto>? values = await response.Content.ReadFromJsonAsync<List<ExternalResourceDto>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return (values ?? []).Where(value => value.MetaInfo?.ID is Guid id && id != Guid.Empty)
            .Select(value => new ExternalResource(value.MetaInfo!.ID, value.Name ?? string.Empty)).ToList();
    }

    private static List<ClusterBatchExternalReference> BuildManifest(IEnumerable<Guid?> referencedIds,
        IReadOnlyList<ExternalResource> available, string property, List<ClusterBatchError> errors)
    {
        Dictionary<Guid, ExternalResource> byId = available.GroupBy(value => value.ID)
            .ToDictionary(group => group.Key, group => group.First());
        List<ClusterBatchExternalReference> result = [];
        foreach (Guid id in referencedIds.Where(value => value is Guid guid && guid != Guid.Empty)
                     .Select(value => value!.Value).Distinct().Order())
        {
            if (!byId.TryGetValue(id, out ExternalResource? value))
                errors.Add(Error(null, $"Document.ExternalReferences.{property}", "external_reference_not_found",
                    $"Referenced {property} UUID '{id}' does not exist on the source service."));
            else if (string.IsNullOrWhiteSpace(value.Name))
                errors.Add(Error(null, $"Document.ExternalReferences.{property}", "external_reference_name_missing",
                    $"Referenced {property} UUID '{id}' has no usable name."));
            else result.Add(new ClusterBatchExternalReference { SourceID = id, Name = value.Name });
        }
        return result;
    }

    private static void Resolve(IEnumerable<ClusterBatchExternalReference> sources,
        IReadOnlyList<ExternalResource> locals, string resource,
        List<ClusterBatchExternalReferenceMapping> mappings, List<ClusterBatchError> errors)
    {
        foreach (ClusterBatchExternalReference source in sources)
        {
            ExternalResource? exact = locals.SingleOrDefault(value => value.ID == source.SourceID);
            if (exact != null)
            {
                mappings.Add(Mapping(resource, source, exact.ID, "ExactUUID"));
                continue;
            }

            List<ExternalResource> nameMatches = locals.Where(value => SameName(value.Name, source.Name)).ToList();
            if (nameMatches.Count == 1)
                mappings.Add(Mapping(resource, source, nameMatches[0].ID, "NormalizedName"));
            else if (nameMatches.Count == 0)
                errors.Add(Error(null, $"Document.ExternalReferences.{resource}s[{source.SourceID}]",
                    "external_reference_not_found", $"No destination {resource} named '{source.Name}' exists for source UUID '{source.SourceID}'."));
            else
                errors.Add(Error(null, $"Document.ExternalReferences.{resource}s[{source.SourceID}]",
                    "ambiguous_external_reference", $"More than one destination {resource} has normalized name '{source.Name}'."));
        }
    }

    private static ClusterBatchExternalReferenceMapping Mapping(string resource,
        ClusterBatchExternalReference source, Guid localId, string resolution) => new()
        { Resource = resource, Name = source.Name, SourceID = source.SourceID, LocalID = localId, Resolution = resolution };

    private static string EnsureTrailingSlash(string value) => value.EndsWith('/') ? value : value + "/";
    private static bool IsReference(Guid? value) => value is Guid id && id != Guid.Empty;
    private static string Normalize(string? value) => string.Join(' ', (value ?? string.Empty).Normalize(NormalizationForm.FormKC)
        .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
    private static bool SameName(string? left, string? right) => Normalize(left) == Normalize(right);
    private static ClusterBatchError Error(int? index, string property, string code, string message) =>
        new() { PositionIndex = index, Property = property, Code = code, Message = message };

    private sealed class ExternalResourceDto
    {
        public ExternalMetaInfo? MetaInfo { get; set; }
        public string? Name { get; set; }
    }
    private sealed class ExternalMetaInfo { public Guid ID { get; set; } }
    private sealed record ExternalResource(Guid ID, string Name);
}
