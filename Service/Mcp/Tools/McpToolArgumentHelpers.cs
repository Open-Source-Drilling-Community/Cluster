using System;
using System.Text.Json.Nodes;

namespace NORCE.Drilling.Cluster.Service.Mcp.Tools;

internal static class McpToolArgumentHelpers
{
    public static JsonObject CreateEmptySchema() => new()
    {
        ["type"] = "object",
        ["additionalProperties"] = false
    };

    public static JsonObject CreateGuidSchema(string key, string description)
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                [key] = new JsonObject
                {
                    ["type"] = "string",
                    ["format"] = "uuid",
                    ["description"] = description
                }
            },
            ["required"] = new JsonArray
            {
                key
            },
            ["additionalProperties"] = false
        };
    }

    public static JsonObject CreateClusterSchema(bool includeId = false) =>
        WrapBody("cluster", CreateClusterObjectSchema(), includeId, "cluster.MetaInfo.ID");

    public static JsonObject CreateClusterResourceSchema() => CreateClusterObjectSchema();

    public static JsonObject CreateClusterLightResourceSchema() => CreateClusterLightObjectSchema();

    public static JsonObject CreateClusterFeatureCategorySchema(bool includeId = false) =>
        WrapBody("clusterFeatureCategory", CreateFeatureCategoryObjectSchema("cluster"), includeId, "clusterFeatureCategory.MetaInfo.ID");

    public static JsonObject CreateClusterFeatureCategoryResourceSchema() => CreateFeatureCategoryObjectSchema("cluster");

    public static JsonObject CreateClusterIdentitySchema(bool includeId = false) =>
        WrapBody("clusterIdentity", CreateClusterIdentityObjectSchema(), includeId, "clusterIdentity.MetaInfo.ID");

    public static JsonObject CreateClusterIdentityResourceSchema() => CreateClusterIdentityObjectSchema();

    public static JsonObject CreateSlotFeatureCategorySchema(bool includeId = false) =>
        WrapBody("slotFeatureCategory", CreateFeatureCategoryObjectSchema("slot"), includeId, "slotFeatureCategory.MetaInfo.ID");

    public static JsonObject CreateSlotFeatureCategoryResourceSchema() => CreateFeatureCategoryObjectSchema("slot");

    public static JsonObject CreateStatusOnlyOutputSchema() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject { ["status"] = SuccessStatus() },
        ["required"] = new JsonArray("status"),
        ["additionalProperties"] = false
    };

    public static JsonObject CreateIdsOutputSchema() => SuccessEnvelope(
        ArraySchema("Stored resource UUIDs.", Uuid("Stored resource UUID.")));

    public static JsonObject CreateMetaInfoListOutputSchema() => SuccessEnvelope(
        ArraySchema("Stored resource metadata.", CreateMetaInfoSchema("stored resource")));

    public static JsonObject CreateResourceOutputSchema(JsonObject resourceSchema) => SuccessEnvelope(resourceSchema);

    public static JsonObject CreateResourceListOutputSchema(JsonObject resourceSchema) => SuccessEnvelope(
        ArraySchema("Stored resources.", resourceSchema));

    public static JsonObject CreateClusterBatchExportSchema() => WrapBody("request", new JsonObject
    {
        ["type"] = "object",
        ["description"] = "Select all clusters or an explicitly ordered set. Selected requires unique ClusterIDs; All forbids a non-empty ClusterIDs array.",
        ["properties"] = new JsonObject
        {
            ["Scope"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("All", "Selected") },
            ["ClusterIDs"] = new JsonObject { ["type"] = new JsonArray("array", "null"), ["uniqueItems"] = true, ["items"] = Uuid("Cluster UUID to export.") }
        },
        ["required"] = new JsonArray("Scope"), ["additionalProperties"] = false
    }, false, "request");

    public static JsonObject CreateClusterBatchRestoreSchema() => WrapBody("request", new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["ConflictPolicy"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("FailIfExists", "ReplaceExisting") },
            ["CatalogPolicy"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("MapExisting", "MapOrCreateMissing") },
            ["Document"] = CreateBatchDocumentSchema(1)
        },
        ["required"] = new JsonArray("ConflictPolicy", "CatalogPolicy", "Document"), ["additionalProperties"] = false
    }, false, "request");

    public static JsonObject CreateClusterBatchExportOutputSchema() => SuccessEnvelope(CreateBatchDocumentSchema(0));

    public static JsonObject CreateClusterBatchRestoreOutputSchema() => SuccessEnvelope(new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["RestoredAtUtc"] = new JsonObject { ["type"] = "string", ["format"] = "date-time" },
            ["CreatedCount"] = NonNegativeInteger(), ["ReplacedCount"] = NonNegativeInteger(),
            ["CreatedCatalogDefinitionCount"] = NonNegativeInteger(), ["CreatedCatalogOptionCount"] = NonNegativeInteger(),
            ["CatalogMappings"] = new JsonObject { ["type"] = "array", ["items"] = CreateMappingSchema("Catalog") },
            ["ExternalReferenceMappings"] = new JsonObject { ["type"] = "array", ["items"] = CreateMappingSchema("Resource") },
            ["ClusterIDs"] = new JsonObject { ["type"] = "array", ["items"] = Uuid("Restored cluster UUID.") }
        },
        ["required"] = new JsonArray("RestoredAtUtc", "CreatedCount", "ReplacedCount", "CreatedCatalogDefinitionCount",
            "CreatedCatalogOptionCount", "CatalogMappings", "ExternalReferenceMappings", "ClusterIDs"),
        ["additionalProperties"] = false
    });

    private static JsonObject CreateBatchDocumentSchema(int minimumClusters) => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["FormatIdentifier"] = new JsonObject { ["type"] = "string", ["const"] = "OSDC.Drilling.Cluster.BatchExport" },
            ["SchemaVersion"] = new JsonObject { ["type"] = "integer", ["const"] = 1 },
            ["ExportedAtUtc"] = new JsonObject { ["type"] = "string", ["format"] = "date-time" },
            ["CatalogDependencies"] = new JsonObject
            {
                ["type"] = "object", ["properties"] = new JsonObject
                {
                    ["Identities"] = new JsonObject { ["type"] = "array", ["items"] = CreateClusterIdentityObjectSchema() },
                    ["ClusterFeatureCategories"] = new JsonObject { ["type"] = "array", ["items"] = CreateFeatureCategoryObjectSchema("cluster") },
                    ["SlotFeatureCategories"] = new JsonObject { ["type"] = "array", ["items"] = CreateFeatureCategoryObjectSchema("slot") }
                },
                ["required"] = new JsonArray("Identities", "ClusterFeatureCategories", "SlotFeatureCategories"), ["additionalProperties"] = false
            },
            ["ExternalReferences"] = new JsonObject
            {
                ["type"] = "object", ["properties"] = new JsonObject
                {
                    ["Fields"] = new JsonObject { ["type"] = "array", ["items"] = CreateExternalReferenceSchema() },
                    ["Rigs"] = new JsonObject { ["type"] = "array", ["items"] = CreateExternalReferenceSchema() }
                },
                ["required"] = new JsonArray("Fields", "Rigs"), ["additionalProperties"] = false
            },
            ["Clusters"] = new JsonObject { ["type"] = "array", ["minItems"] = minimumClusters, ["items"] = CreateClusterObjectSchema() }
        },
        ["required"] = new JsonArray("FormatIdentifier", "SchemaVersion", "ExportedAtUtc", "CatalogDependencies", "ExternalReferences", "Clusters"),
        ["additionalProperties"] = false
    };

    private static JsonObject CreateExternalReferenceSchema() => new()
    {
        ["type"] = "object", ["properties"] = new JsonObject
        { ["SourceID"] = Uuid("Source service UUID."), ["Name"] = new JsonObject { ["type"] = "string", ["minLength"] = 1 } },
        ["required"] = new JsonArray("SourceID", "Name"), ["additionalProperties"] = false
    };

    private static JsonObject CreateMappingSchema(string discriminator) => new()
    {
        ["type"] = "object", ["properties"] = new JsonObject
        {
            [discriminator] = new JsonObject { ["type"] = "string" }, ["Name"] = new JsonObject { ["type"] = "string" },
            ["SourceID"] = Uuid("Source UUID."), ["LocalID"] = Uuid("Resolved destination UUID."),
            ["Resolution"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("ExactUUID", "NormalizedName", "Created") }
        },
        ["required"] = new JsonArray(discriminator, "Name", "SourceID", "LocalID", "Resolution"), ["additionalProperties"] = false
    };

    private static JsonObject SuccessEnvelope(JsonObject data) => new()
    {
        ["type"] = "object", ["properties"] = new JsonObject
        { ["status"] = SuccessStatus(), ["data"] = data },
        ["required"] = new JsonArray("status", "data"), ["additionalProperties"] = false
    };

    private static JsonObject SuccessStatus() => new()
    { ["type"] = "integer", ["minimum"] = 200, ["maximum"] = 299 };

    private static JsonObject ArraySchema(string description, JsonObject itemSchema, int? minimumItems = null)
    {
        JsonObject schema = new()
        {
            ["type"] = "array",
            ["description"] = description,
            ["items"] = itemSchema
        };
        if (minimumItems != null) schema["minItems"] = minimumItems.Value;
        return schema;
    }

    private static JsonObject NonNegativeInteger() => new() { ["type"] = "integer", ["minimum"] = 0 };

    private static JsonObject WrapBody(string key, JsonObject bodySchema, bool includeId, string bodyIdPath)
    {
        var properties = new JsonObject
        {
            [key] = bodySchema
        };
        var required = new JsonArray
        {
            key
        };

        if (includeId)
        {
            properties["id"] = new JsonObject
            {
                ["type"] = "string",
                ["format"] = "uuid",
                ["description"] = $"Identifier of the stored record to update. It must equal {bodyIdPath}."
            };
            required.Add("id");
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required,
            ["additionalProperties"] = false
        };
    }

    public static JsonObject CreateBooleanSchema(string key, string description)
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                [key] = new JsonObject
                {
                    ["type"] = "boolean",
                    ["description"] = description
                }
            },
            ["required"] = new JsonArray
            {
                key
            },
            ["additionalProperties"] = false
        };
    }

    private static JsonObject CreateClusterObjectSchema() => new()
    {
        ["type"] = "object",
        ["description"] = "Complete Cluster resource representing a drilling site, platform, or single-well location. MetaInfo.ID must be a caller-generated, non-empty UUID.",
        ["properties"] = new JsonObject
        {
            ["MetaInfo"] = CreateMetaInfoSchema("cluster"),
            ["Name"] = NullableString("Human-readable cluster name."),
            ["Description"] = NullableString("Human-readable description of the cluster."),
            ["CreationDate"] = NullableDateTime("UTC or offset timestamp at which the cluster record was created."),
            ["LastModificationDate"] = NullableDateTime("UTC or offset timestamp of the most recent modification."),
            ["FieldID"] = NullableUuid("Identifier of the Field resource to which the cluster belongs."),
            ["IsSingleWell"] = Boolean("True when this record represents a single well rather than a multi-well cluster.", false),
            ["RigID"] = NullableUuid("Identifier of the associated Rig resource, or null when no rig is assigned."),
            ["IsFixedPlatform"] = Boolean("True for a fixed platform; false for a floating or movable installation.", false),
            ["ClusterIdentityAssignments"] = NullableArray("Identity values assigned to this cluster.", CreateIdentityAssignmentSchema()),
            ["ClusterFeatureAssignments"] = NullableArray("Feature options assigned to this cluster, optionally with validity periods.", CreateFeatureAssignmentSchema("cluster")),
            ["ReferencePoint"] = CreateReferencePointSchema(),
            ["GroundMudLineDepth"] = CreateDepthSchema("Vertical depth of ground level or mud line. Values are in meters (SI) and referenced to the fixed WGS84 vertical datum."),
            ["TopWaterDepth"] = CreateDepthSchema("Vertical depth of the top water level. Values are in meters (SI) and referenced to the fixed WGS84 vertical datum."),
            ["Slots"] = new JsonObject
            {
                ["type"] = new JsonArray { "object", "null" },
                ["description"] = "Slots belonging to the cluster, encoded as an object whose property names are slot UUIDs and whose values are complete Slot records.",
                ["additionalProperties"] = CreateSlotSchema()
            }
        },
        ["required"] = new JsonArray { "MetaInfo" },
        ["additionalProperties"] = false
    };

    private static JsonObject CreateClusterLightObjectSchema() => new()
    {
        ["type"] = "object",
        ["description"] = "Lightweight Cluster resource for discovery and selection without nested identities, feature assignments, or slots.",
        ["properties"] = new JsonObject
        {
            ["MetaInfo"] = CreateMetaInfoSchema("cluster"),
            ["Name"] = NullableString("Human-readable cluster name."),
            ["Description"] = NullableString("Human-readable cluster description."),
            ["CreationDate"] = NullableDateTime("UTC or offset timestamp at which the cluster was created."),
            ["LastModificationDate"] = NullableDateTime("UTC or offset timestamp of the most recent modification."),
            ["FieldID"] = NullableUuid("Identifier of the Field resource to which the cluster belongs."),
            ["IsSingleWell"] = Boolean("True when this record represents a single well rather than a multi-well cluster.", false),
            ["RigID"] = NullableUuid("Identifier of the associated Rig resource, or null when no rig is assigned."),
            ["IsFixedPlatform"] = Boolean("True for a fixed platform; false for a floating or movable installation.", false),
            ["ReferencePoint"] = CreateReferencePointSchema(),
            ["GroundMudLineDepth"] = CreateDepthSchema("Vertical depth of ground level or mud line in metres (SI), referenced to the fixed WGS84 vertical datum."),
            ["TopWaterDepth"] = CreateDepthSchema("Vertical depth of the top water level in metres (SI), referenced to the fixed WGS84 vertical datum.")
        },
        ["required"] = new JsonArray("MetaInfo"),
        ["additionalProperties"] = false
    };

    private static JsonObject CreateClusterIdentityObjectSchema() => new()
    {
        ["type"] = "object",
        ["description"] = "Definition of a symbolic identity type that clusters can populate through ClusterIdentityAssignments. MetaInfo.ID must be a caller-generated, non-empty UUID.",
        ["properties"] = new JsonObject
        {
            ["MetaInfo"] = CreateMetaInfoSchema("cluster identity"),
            ["Name"] = NullableString("Symbolic name of the cluster identity, such as an operator-specific identifier type."),
            ["CreationDate"] = NullableDateTime("UTC or offset timestamp at which the identity definition was created."),
            ["LastModificationDate"] = NullableDateTime("UTC or offset timestamp of the most recent modification.")
        },
        ["required"] = new JsonArray { "MetaInfo" },
        ["additionalProperties"] = false
    };

    private static JsonObject CreateFeatureCategoryObjectSchema(string target) => new()
    {
        ["type"] = "object",
        ["description"] = $"Definition of a feature category and its allowed options for assignment to a {target}. MetaInfo.ID must be a caller-generated, non-empty UUID.",
        ["properties"] = new JsonObject
        {
            ["MetaInfo"] = CreateMetaInfoSchema($"{target} feature category"),
            ["Name"] = NullableString($"Human-readable name of the {target} feature category."),
            ["IsExclusive"] = Boolean("True when at most one option from this category may be assigned at a time.", false),
            ["HasValidityPeriod"] = Boolean("True when assignments from this category use FromDate and ToDate validity boundaries.", false),
            ["Options"] = NullableArray("Allowed options in this category.", CreateFeatureOptionSchema()),
            ["CreationDate"] = NullableDateTime("UTC or offset timestamp at which the category was created."),
            ["LastModificationDate"] = NullableDateTime("UTC or offset timestamp of the most recent modification.")
        },
        ["required"] = new JsonArray { "MetaInfo" },
        ["additionalProperties"] = false
    };

    private static JsonObject CreateMetaInfoSchema(string resource) => new()
    {
        ["type"] = "object",
        ["description"] = $"Identity and optional HTTP location metadata for the {resource}.",
        ["properties"] = new JsonObject
        {
            ["ID"] = Uuid($"Non-empty unique identifier of the {resource}."),
            ["HttpHostName"] = NullableString($"Optional host name from which the {resource} can be retrieved."),
            ["HttpHostBasePath"] = NullableString($"Optional service base path from which the {resource} can be retrieved."),
            ["HttpEndPoint"] = NullableString($"Optional HTTP endpoint for this {resource} resource.")
        },
        ["required"] = new JsonArray { "ID" },
        ["additionalProperties"] = false
    };

    private static JsonObject CreateIdentityAssignmentSchema() => new()
    {
        ["type"] = "object",
        ["description"] = "A cluster-specific value for a defined ClusterIdentity.",
        ["properties"] = new JsonObject
        {
            ["ID"] = Uuid("Stable identifier of this assignment."),
            ["IdentityID"] = NullableUuid("Identifier of the ClusterIdentity definition selected by this assignment."),
            ["Value"] = NullableString("Cluster-specific value for the selected identity.")
        },
        ["required"] = new JsonArray { "ID" },
        ["additionalProperties"] = false
    };

    private static JsonObject CreateFeatureAssignmentSchema(string target) => new()
    {
        ["type"] = "object",
        ["description"] = $"Selection of one feature option for the {target}, optionally constrained to a validity interval.",
        ["properties"] = new JsonObject
        {
            ["ID"] = Uuid("Stable identifier of this assignment."),
            ["FeatureCategoryID"] = NullableUuid($"Identifier of the {target} feature category."),
            ["FeatureOptionID"] = NullableUuid("Identifier of the selected option within that category."),
            ["FromDate"] = NullableDateTime("First instant at which the assignment is valid."),
            ["ToDate"] = NullableDateTime("Last instant at which the assignment is valid.")
        },
        ["required"] = new JsonArray { "ID" },
        ["additionalProperties"] = false
    };

    private static JsonObject CreateFeatureOptionSchema() => new()
    {
        ["type"] = "object",
        ["description"] = "One selectable option within the feature category.",
        ["properties"] = new JsonObject
        {
            ["ID"] = Uuid("Stable identifier of the option within its category."),
            ["Name"] = NullableString("Human-readable option name.")
        },
        ["required"] = new JsonArray { "ID" },
        ["additionalProperties"] = false
    };

    private static JsonObject CreateSlotSchema() => new()
    {
        ["type"] = "object",
        ["description"] = "A well slot belonging to the cluster. The ID should match the UUID used as its key in the parent Slots object.",
        ["properties"] = new JsonObject
        {
            ["ID"] = Uuid("Unique identifier of the slot."),
            ["Name"] = NullableString("Human-readable slot name."),
            ["Description"] = NullableString("Human-readable slot description."),
            ["CreationDate"] = NullableDateTime("UTC or offset timestamp at which the slot was created."),
            ["LastModificationDate"] = NullableDateTime("UTC or offset timestamp of the most recent modification."),
            ["Latitude"] = CreateAngleSchema("Slot latitude in radians (SI plane angle), referenced to WGS84."),
            ["Longitude"] = CreateAngleSchema("Slot longitude in radians (SI plane angle), referenced to WGS84."),
            ["SlotFeatureAssignments"] = NullableArray("Feature options assigned to this slot, optionally with validity periods.", CreateFeatureAssignmentSchema("slot"))
        },
        ["required"] = new JsonArray { "ID" },
        ["additionalProperties"] = false
    };

    private static JsonObject CreateReferencePointSchema() => new()
    {
        ["type"] = new JsonArray { "object", "null" },
        ["description"] = "Optional global cluster reference point using SI values and WGS84 references. Latitude and Longitude are radians; linear coordinates and TVD are meters.",
        ["properties"] = new JsonObject
        {
            ["X"] = NullableNumber("Riemannian north coordinate: meridian arc length from the equator, in meters; synonymous with RiemannianNorth."),
            ["Y"] = NullableNumber("Riemannian east coordinate: arc length from the Greenwich meridian along the latitude parallel, in meters; synonymous with RiemannianEast."),
            ["Z"] = NullableNumber("True vertical depth in meters, referenced to the WGS84 vertical datum; synonymous with TVD."),
            ["RiemannianNorth"] = NullableNumber("Riemannian north coordinate in meters."),
            ["RiemannianEast"] = NullableNumber("Riemannian east coordinate in meters."),
            ["Latitude"] = NullableNumber("Geodetic latitude in radians, referenced to WGS84."),
            ["Longitude"] = NullableNumber("Geodetic longitude in radians, referenced to WGS84."),
            ["TVD"] = NullableNumber("True vertical depth in meters, referenced to the WGS84 vertical datum.")
        },
        ["additionalProperties"] = false
    };

    private static JsonObject CreateDepthSchema(string description) => CreateGaussianSchema(description, "meters (SI)");

    private static JsonObject CreateAngleSchema(string description) => CreateGaussianSchema(description, "radians (SI)");

    private static JsonObject CreateGaussianSchema(string description, string unit) => new()
    {
        ["type"] = new JsonArray { "object", "null" },
        ["description"] = description,
        ["properties"] = new JsonObject
        {
            ["GaussianValue"] = new JsonObject
            {
                ["type"] = "object",
                ["description"] = $"Gaussian value and uncertainty expressed in {unit}.",
                ["properties"] = new JsonObject
                {
                    ["MinValue"] = Number($"Minimum value represented by the distribution, in {unit}."),
                    ["MaxValue"] = Number($"Maximum value represented by the distribution, in {unit}."),
                    ["Mean"] = NullableNumber($"Mean value of the distribution, in {unit}."),
                    ["StandardDeviation"] = NullableNumber($"Standard deviation of the distribution, in {unit}.")
                },
                ["required"] = new JsonArray { "MinValue", "MaxValue" },
                ["additionalProperties"] = false
            }
        },
        ["required"] = new JsonArray { "GaussianValue" },
        ["additionalProperties"] = false
    };

    private static JsonObject NullableArray(string description, JsonObject itemSchema) => new()
    {
        ["type"] = new JsonArray { "array", "null" },
        ["description"] = description,
        ["items"] = itemSchema
    };

    private static JsonObject Uuid(string description) => new()
    {
        ["type"] = "string",
        ["format"] = "uuid",
        ["description"] = description
    };

    private static JsonObject NullableUuid(string description) => new()
    {
        ["type"] = new JsonArray { "string", "null" },
        ["format"] = "uuid",
        ["description"] = description
    };

    private static JsonObject NullableString(string description) => new()
    {
        ["type"] = new JsonArray { "string", "null" },
        ["description"] = description
    };

    private static JsonObject NullableDateTime(string description) => new()
    {
        ["type"] = new JsonArray { "string", "null" },
        ["format"] = "date-time",
        ["description"] = description
    };

    private static JsonObject Number(string description) => new()
    {
        ["type"] = "number",
        ["description"] = description
    };

    private static JsonObject NullableNumber(string description) => new()
    {
        ["type"] = new JsonArray { "number", "null" },
        ["description"] = description
    };

    private static JsonObject Boolean(string description, bool defaultValue) => new()
    {
        ["type"] = "boolean",
        ["description"] = description,
        ["default"] = defaultValue
    };

    public static bool TryParseGuid(JsonObject? arguments, string key, out Guid value, out JsonNode? error)
    {
        value = Guid.Empty;
        error = null;

        var node = arguments?[key];
        if (node is null)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' is required.");
            return false;
        }

        if (!Guid.TryParse(node.ToString(), out value))
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' must be a valid UUID.");
            return false;
        }

        return true;
    }

    public static bool TryParseBool(JsonObject? arguments, string key, out bool value, out JsonNode? error)
    {
        value = false;
        error = null;

        var node = arguments?[key];
        if (node is null)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' is required.");
            return false;
        }

        try
        {
            value = node.GetValue<bool>();
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' must be a boolean.");
            return false;
        }
    }

    public static bool TryParseDouble(JsonObject? arguments, string key, out double value, out JsonNode? error)
    {
        value = 0d;
        error = null;

        var node = arguments?[key];
        if (node is null)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' is required.");
            return false;
        }

        try
        {
            value = node.GetValue<double>();
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' must be a number.");
            return false;
        }

        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' must be a finite number.");
            return false;
        }

        return true;
    }
}
