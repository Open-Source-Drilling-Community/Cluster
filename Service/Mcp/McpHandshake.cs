using System.Text.Json.Nodes;

namespace OSDC.Drilling.Cluster.Service.Mcp;

public sealed record McpHandshake(
    string ProtocolVersion,
    string? ClientName,
    string? ClientVersion,
    string? SessionId,
    JsonObject? Capabilities);
