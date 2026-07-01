using System.Net.Http.Json;
using System.Text.Json;
using ModelShared = NORCE.Drilling.Cluster.ModelShared;

namespace NORCE.Drilling.Cluster.WebPages;

public static class MslDepthReferenceUtils
{
    public static Task<double?> ResolveMeanSeaLevelDepthReferenceAsync(IClusterAPIUtils api, ModelShared.Cluster? cluster)
    {
        double? latitude = cluster?.ReferenceLatitude?.GaussianValue?.Mean;
        double? longitude = cluster?.ReferenceLongitude?.GaussianValue?.Mean;
        return CalculateMeanSeaLevelDepthReferenceAsync(api.HttpClientVerticalDatum, api.HostNameVerticalDatum, api.HostBasePathVerticalDatum, latitude, longitude);
    }

    public static async Task<double?> CalculateMeanSeaLevelDepthReferenceAsync(HttpClient client, string hostName, string hostBasePath, double? latitude, double? longitude)
    {
        if (latitude == null || longitude == null)
        {
            return null;
        }

        Guid orderId = Guid.NewGuid();
        object order = new
        {
            MetaInfo = new
            {
                ID = orderId,
                HttpHostName = hostName,
                HttpHostBasePath = hostBasePath,
                HttpEndPoint = "VerticalDatumOrder/"
            },
            Name = $"MSL reference {orderId}",
            Description = "Temporary MSL-to-WGS84 conversion.",
            CreationDate = DateTimeOffset.UtcNow,
            LastModificationDate = DateTimeOffset.UtcNow,
            VerticalDatum = new
            {
                MetaInfo = new
                {
                    ID = Guid.NewGuid(),
                    HttpHostName = hostName,
                    HttpHostBasePath = hostBasePath,
                    HttpEndPoint = "VerticalDatum/"
                },
                Name = $"MSL reference {orderId}",
                Description = "Temporary MSL-to-WGS84 conversion.",
                CreationDate = DateTimeOffset.UtcNow,
                LastModificationDate = DateTimeOffset.UtcNow,
                DatumSet = new[]
                {
                    new
                    {
                        Latitude = latitude.Value,
                        Longitude = longitude.Value,
                        GenericVerticalDatum = 0
                    }
                },
                ConversionFrom = "FromMeanSeaLevel",
                Type = "Raw"
            }
        };

        try
        {
            using HttpResponseMessage postResponse = await client.PostAsJsonAsync("VerticalDatumOrder", order);
            postResponse.EnsureSuccessStatusCode();

            using JsonDocument document = await client.GetFromJsonAsync<JsonDocument>($"VerticalDatumOrder/{orderId}") ?? throw new InvalidOperationException("VerticalDatumOrder response was empty.");
            JsonElement datumSet = document.RootElement.GetProperty("VerticalDatum").GetProperty("DatumSet");
            if (datumSet.GetArrayLength() == 0 ||
                !datumSet[0].TryGetProperty("VerticalDatumWGS64", out JsonElement valueElement) ||
                valueElement.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return -valueElement.GetDouble();
        }
        finally
        {
            try
            {
                await client.DeleteAsync($"VerticalDatumOrder/{orderId}");
            }
            catch
            {
                // Best-effort cleanup of a temporary calculation order.
            }
        }
    }
}
