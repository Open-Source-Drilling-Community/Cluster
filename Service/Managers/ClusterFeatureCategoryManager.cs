using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OSDC.Drilling.Cluster.Service.Managers
{
    public class ClusterFeatureCategoryManager
    {
        private static ClusterFeatureCategoryManager? _instance;
        private readonly ILogger<ClusterFeatureCategoryManager> _logger;
        private readonly SqlConnectionManager _connectionManager;
        private static readonly DefaultClusterFeatureCategory[] DefaultCategories =
        [
            new(
                "Cluster purpose",
                false,
                true,
                [
                    "Production",
                    "Injection",
                    "Observation",
                    "Appraisal",
                    "Exploration",
                    "Storage",
                    "Geothermal production",
                    "Geothermal injection",
                    "Mining access"
                ]),
            new(
                "Development role",
                true,
                true,
                [
                    "Main producing center",
                    "Satellite cluster",
                    "Tie-back cluster",
                    "Infill cluster",
                    "Pilot cluster",
                    "Appraisal cluster",
                    "Abandonment cluster"
                ]),
            new(
                "Installation type",
                true,
                true,
                [
                    "Subsea template",
                    "Fixed platform",
                    "Floating facility",
                    "Land pad",
                    "Artificial island",
                    "Jack-up supported",
                    "Single wellhead"
                ]),
            new(
                "Operational status",
                true,
                true,
                [
                    "Planned",
                    "Drilling",
                    "Active",
                    "Suspended",
                    "Shut-in",
                    "Abandoned",
                    "Decommissioned"
                ]),
            new(
                "Access mode",
                false,
                true,
                [
                    "Dry tree access",
                    "Wet tree access",
                    "Riser access",
                    "Intervention vessel access",
                    "Land rig access",
                    "Platform rig access"
                ]),
            new(
                "Drilling strategy",
                false,
                true,
                [
                    "Batch drilling",
                    "Sequential drilling",
                    "Extended reach drilling",
                    "Multilateral drilling",
                    "Sidetrack intensive",
                    "Re-entry intensive"
                ]),
            new(
                "Pressure / flow function",
                false,
                true,
                [
                    "Producer cluster",
                    "Water injector cluster",
                    "Gas injector cluster",
                    "CO2 injector cluster",
                    "Disposal cluster",
                    "Pressure observation cluster"
                ]),
            new(
                "Facility integration",
                false,
                true,
                [
                    "Standalone",
                    "Tied to host facility",
                    "Tied to FPSO",
                    "Tied to platform",
                    "Tied to subsea manifold",
                    "Tied to pipeline network"
                ]),
            new(
                "Constraint sensitivity",
                false,
                true,
                [
                    "Shallow hazard constrained",
                    "Geohazard constrained",
                    "Seabed infrastructure constrained",
                    "Environmental constrained",
                    "Lease-line constrained",
                    "No-drill-zone constrained"
                ]),
            new(
                "Environment",
                true,
                false,
                [
                    "Offshore shallow water",
                    "Offshore deepwater",
                    "Ultra-deepwater",
                    "Onshore",
                    "Arctic",
                    "Desert",
                    "Urban / restricted access"
                ]),
            new(
                "Energy / resource domain",
                false,
                true,
                [
                    "Oil and gas",
                    "Geothermal",
                    "CO2 storage",
                    "Hydrogen storage",
                    "Mining",
                    "Gas storage",
                    "Water production / disposal"
                ]),
            new(
                "Integrity risk class",
                false,
                true,
                [
                    "Normal",
                    "High pressure",
                    "High temperature",
                    "Sour service",
                    "CO2 service",
                    "Corrosive service",
                    "Shallow gas risk"
                ]),
            new(
                "Maturity",
                true,
                true,
                [
                    "Concept",
                    "Sanctioned",
                    "Under construction",
                    "Producing",
                    "Mature",
                    "Late life",
                    "Plug and abandonment"
                ])
        ];

        private ClusterFeatureCategoryManager(ILogger<ClusterFeatureCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static ClusterFeatureCategoryManager GetInstance(ILogger<ClusterFeatureCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new ClusterFeatureCategoryManager(logger, connectionManager);
            return _instance;
        }

        public List<Guid>? GetAllClusterFeatureCategoryId()
        {
            EnsureDefaultCategories();
            List<Guid> ids = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT ID FROM ClusterFeatureCategoryTable";
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    ids.Add(reader.GetGuid(0));
                }
                return ids;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get IDs from ClusterFeatureCategoryTable");
                return null;
            }
        }

        public List<MetaInfo?>? GetAllClusterFeatureCategoryMetaInfo()
        {
            EnsureDefaultCategories();
            List<MetaInfo?> metaInfos = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT MetaInfo FROM ClusterFeatureCategoryTable";
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    metaInfos.Add(JsonSerializer.Deserialize<MetaInfo>(reader.GetString(0), JsonSettings.Options));
                }
                return metaInfos;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get MetaInfo from ClusterFeatureCategoryTable");
                return null;
            }
        }

        public Model.ClusterFeatureCategory? GetClusterFeatureCategoryById(Guid guid)
        {
            if (guid == Guid.Empty)
            {
                return null;
            }

            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = $"SELECT ClusterFeatureCategory FROM ClusterFeatureCategoryTable WHERE ID = '{guid}'";
            try
            {
                using var reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    Model.ClusterFeatureCategory? data = JsonSerializer.Deserialize<Model.ClusterFeatureCategory>(reader.GetString(0), JsonSettings.Options);
                    if (data != null && data.MetaInfo != null && data.MetaInfo.ID != guid)
                    {
                        throw new SqliteException("SQLite database corrupted: returned ClusterFeatureCategory has the wrong ID.", 1);
                    }
                    return data;
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get ClusterFeatureCategory from ClusterFeatureCategoryTable");
            }

            return null;
        }

        public List<Model.ClusterFeatureCategory?>? GetAllClusterFeatureCategory()
        {
            EnsureDefaultCategories();
            List<Model.ClusterFeatureCategory?> values = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT ClusterFeatureCategory FROM ClusterFeatureCategoryTable";
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    values.Add(JsonSerializer.Deserialize<Model.ClusterFeatureCategory>(reader.GetString(0), JsonSettings.Options));
                }
                return values;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get ClusterFeatureCategory from ClusterFeatureCategoryTable");
                return null;
            }
        }

        public bool AddClusterFeatureCategory(Model.ClusterFeatureCategory? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return false;
            }
            if (GetClusterFeatureCategoryById(data.MetaInfo.ID) != null)
            {
                return false;
            }

            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return false;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                PrepareCategory(data);
                DateTimeOffset now = DateTimeOffset.UtcNow;
                data.CreationDate ??= now;
                data.LastModificationDate = now;
                string metaInfo = JsonSerializer.Serialize(data.MetaInfo, JsonSettings.Options);
                string serialized = JsonSerializer.Serialize(data, JsonSettings.Options);
                string? creationDate = data.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string? lastModificationDate = data.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO ClusterFeatureCategoryTable (" +
                    "ID, MetaInfo, Name, IsExclusive, HasValidityPeriod, CreationDate, LastModificationDate, ClusterFeatureCategory" +
                    ") VALUES (" +
                    $"'{data.MetaInfo.ID}', '{metaInfo}', '{data.Name}', {(data.IsExclusive ? 1 : 0)}, {(data.HasValidityPeriod ? 1 : 0)}, '{creationDate}', '{lastModificationDate}', '{serialized}')";
                int count = command.ExecuteNonQuery();
                if (count != 1)
                {
                    transaction.Rollback();
                    return false;
                }
                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to add ClusterFeatureCategory");
                return false;
            }
        }

        public bool UpdateClusterFeatureCategoryById(Guid guid, Model.ClusterFeatureCategory? data)
        {
            if (guid == Guid.Empty || data?.MetaInfo == null || data.MetaInfo.ID != guid)
            {
                return false;
            }

            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return false;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                PrepareCategory(data);
                data.LastModificationDate = DateTimeOffset.UtcNow;
                string metaInfo = JsonSerializer.Serialize(data.MetaInfo, JsonSettings.Options);
                string serialized = JsonSerializer.Serialize(data, JsonSettings.Options);
                string? creationDate = data.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string? lastModificationDate = data.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                var command = connection.CreateCommand();
                command.CommandText = $"UPDATE ClusterFeatureCategoryTable SET " +
                    $"MetaInfo = '{metaInfo}', " +
                    $"Name = '{data.Name}', " +
                    $"IsExclusive = {(data.IsExclusive ? 1 : 0)}, " +
                    $"HasValidityPeriod = {(data.HasValidityPeriod ? 1 : 0)}, " +
                    $"CreationDate = '{creationDate}', " +
                    $"LastModificationDate = '{lastModificationDate}', " +
                    $"ClusterFeatureCategory = '{serialized}' " +
                    $"WHERE ID = '{guid}'";
                int count = command.ExecuteNonQuery();
                if (count != 1)
                {
                    transaction.Rollback();
                    return false;
                }
                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to update ClusterFeatureCategory");
                return false;
            }
        }

        public bool DeleteClusterFeatureCategoryById(Guid guid)
        {
            if (guid == Guid.Empty)
            {
                return false;
            }

            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return false;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                var command = connection.CreateCommand();
                command.CommandText = $"DELETE FROM ClusterFeatureCategoryTable WHERE ID = '{guid}'";
                command.ExecuteNonQuery();
                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to delete ClusterFeatureCategory");
                return false;
            }
        }

        private void EnsureDefaultCategories()
        {
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM ClusterFeatureCategoryTable";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read() && reader.GetInt64(0) > 0)
                {
                    return;
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to count ClusterFeatureCategoryTable");
                return;
            }

            foreach (DefaultClusterFeatureCategory defaultCategory in DefaultCategories)
            {
                AddClusterFeatureCategory(CreateDefaultCategory(defaultCategory));
            }
        }

        private static Model.ClusterFeatureCategory CreateDefaultCategory(DefaultClusterFeatureCategory defaultCategory) =>
            new()
            {
                MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
                Name = defaultCategory.Name,
                IsExclusive = defaultCategory.IsExclusive,
                HasValidityPeriod = defaultCategory.HasValidityPeriod,
                Options = defaultCategory.Options
                    .Select(option => new Model.ClusterFeatureOption { ID = Guid.NewGuid(), Name = option })
                    .ToList()
            };

        private static void PrepareCategory(Model.ClusterFeatureCategory category)
        {
            category.Options ??= [];
            foreach (Model.ClusterFeatureOption option in category.Options)
            {
                if (option.ID == Guid.Empty)
                {
                    option.ID = Guid.NewGuid();
                }
            }
        }

        private sealed record DefaultClusterFeatureCategory(
            string Name,
            bool IsExclusive,
            bool HasValidityPeriod,
            string[] Options);
    }
}
