using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using NORCE.Drilling.Cluster.Model;
using NORCE.Drilling.Cluster.Service;
using OSDC.DotnetLibraries.General.DataManagement;
using ClusterModel = NORCE.Drilling.Cluster.Model.Cluster;

namespace NORCE.Drilling.Cluster.ServiceTest;

[TestFixture]
public sealed class ClusterExternalReferenceResolverTests
{
    [Test]
    public async Task Export_manifest_records_live_names_for_referenced_fields_and_rigs()
    {
        Guid field = Guid.NewGuid(), rig = Guid.NewGuid();
        ClusterExternalReferenceResolver resolver = Resolver(
            $"[{{\"MetaInfo\":{{\"ID\":\"{field}\"}},\"Name\":\"Alve Nord\"}}]",
            $"[{{\"MetaInfo\":{{\"ID\":\"{rig}\"}},\"Name\":\"Rig A\"}}]");
        ClusterBatchExportDocument document = new()
        {
            Clusters = [new ClusterModel { MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, FieldID = field, RigID = rig }]
        };

        List<ClusterBatchError> errors = await resolver.PopulateExportManifestAsync(document, CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(errors, Is.Empty);
            Assert.That(document.ExternalReferences.Fields.Single().Name, Is.EqualTo("Alve Nord"));
            Assert.That(document.ExternalReferences.Rigs.Single().Name, Is.EqualTo("Rig A"));
        });
    }

    [Test]
    public async Task Restore_uses_unique_normalized_name_when_source_uuid_is_absent()
    {
        Guid sourceField = Guid.NewGuid(), localField = Guid.NewGuid();
        ClusterExternalReferenceResolver resolver = Resolver(
            $"[{{\"MetaInfo\":{{\"ID\":\"{localField}\"}},\"Name\":\"  ALVE   NORD \"}}]", "[]");
        ClusterBatchExportDocument document = new()
        {
            ExternalReferences = new() { Fields = [new() { SourceID = sourceField, Name = "Alve Nord" }] }
        };

        ClusterExternalReferenceResolutionOutcome outcome = await resolver.ResolveRestoreManifestAsync(document, CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(outcome.IsSuccess, Is.True);
            Assert.That(outcome.Mappings.Single().LocalID, Is.EqualTo(localField));
            Assert.That(outcome.Mappings.Single().Resolution, Is.EqualTo("NormalizedName"));
        });
    }

    [Test]
    public async Task Restore_keeps_existing_uuid_when_display_name_changed()
    {
        Guid field = Guid.NewGuid();
        ClusterExternalReferenceResolver resolver = Resolver(
            $"[{{\"MetaInfo\":{{\"ID\":\"{field}\"}},\"Name\":\"Renamed field\"}}]", "[]");
        ClusterBatchExportDocument document = new()
        { ExternalReferences = new() { Fields = [new() { SourceID = field, Name = "Previous field name" }] } };

        ClusterExternalReferenceResolutionOutcome outcome = await resolver.ResolveRestoreManifestAsync(document, CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(outcome.IsSuccess, Is.True);
            Assert.That(outcome.Mappings.Single().LocalID, Is.EqualTo(field));
            Assert.That(outcome.Mappings.Single().Resolution, Is.EqualTo("ExactUUID"));
        });
    }

    [Test]
    public async Task Restore_rejects_ambiguous_normalized_name()
    {
        Guid source = Guid.NewGuid();
        string fields = $"[{{\"MetaInfo\":{{\"ID\":\"{Guid.NewGuid()}\"}},\"Name\":\"Same\"}},{{\"MetaInfo\":{{\"ID\":\"{Guid.NewGuid()}\"}},\"Name\":\"same\"}}]";
        ClusterExternalReferenceResolver resolver = Resolver(fields, "[]");
        ClusterBatchExportDocument document = new() { ExternalReferences = new() { Fields = [new() { SourceID = source, Name = "Same" }] } };

        ClusterExternalReferenceResolutionOutcome outcome = await resolver.ResolveRestoreManifestAsync(document, CancellationToken.None);
        Assert.That(outcome.Errors.Single().Code, Is.EqualTo("ambiguous_external_reference"));
    }

    private static ClusterExternalReferenceResolver Resolver(string fieldJson, string rigJson)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["FieldHostURL"] = "https://test/", ["RigHostURL"] = "https://test/" }).Build();
        return new ClusterExternalReferenceResolver(new StubHttpClientFactory(new StubHandler(fieldJson, rigJson)), configuration);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _fieldJson;
        private readonly string _rigJson;
        public StubHandler(string fieldJson, string rigJson) { _fieldJson = fieldJson; _rigJson = rigJson; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string json = request.RequestUri!.AbsolutePath.Contains("/Field/", StringComparison.Ordinal) ? _fieldJson : _rigJson;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }
}
