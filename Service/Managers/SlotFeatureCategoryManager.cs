using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace NORCE.Drilling.Cluster.Service.Managers
{
    public class SlotFeatureCategoryManager
    {
        private static SlotFeatureCategoryManager? _instance;
        private readonly ILogger<SlotFeatureCategoryManager> _logger;
        private readonly SqlConnectionManager _connectionManager;
        private static readonly DefaultSlotFeatureCategory[] DefaultCategories =
        [
            new(
                "Slot status",
                true,
                true,
                [
                    "Available",
                    "Reserved",
                    "Occupied",
                    "Spare",
                    "Suspended",
                    "Abandoned",
                    "Lost",
                    "Recovered",
                    "Decommissioned"
                ]),
            new(
                "Slot usage",
                true,
                true,
                [
                    "Producer",
                    "Injector",
                    "Observer",
                    "Pilot",
                    "Relief well",
                    "Utility",
                    "Future well",
                    "Abandonment access"
                ]),
            new(
                "Slot integrity",
                true,
                true,
                [
                    "Intact",
                    "Damaged",
                    "Obstructed",
                    "Collapsed",
                    "Repaired",
                    "Unknown condition"
                ]),
            new(
                "Slot accessibility",
                true,
                true,
                [
                    "Accessible",
                    "Temporarily inaccessible",
                    "Permanently inaccessible",
                    "Requires intervention",
                    "Access restricted"
                ]),
            new(
                "Slot readiness",
                false,
                true,
                [
                    "Ready to drill",
                    "Requires preparation",
                    "Requires conductor",
                    "Requires cleanup",
                    "Requires survey",
                    "Requires verification"
                ]),
            new(
                "Operational constraint",
                false,
                true,
                [
                    "Collision concern",
                    "Anti-collision restricted",
                    "Shallow hazard concern",
                    "Platform clearance concern",
                    "Environmental restriction",
                    "Regulatory restriction"
                ]),
            new(
                "Slot geometry confidence",
                true,
                false,
                [
                    "Surveyed",
                    "Estimated",
                    "Uncertain location",
                    "Legacy position",
                    "Verified position"
                ])
        ];

        private SlotFeatureCategoryManager(ILogger<SlotFeatureCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static SlotFeatureCategoryManager GetInstance(ILogger<SlotFeatureCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new SlotFeatureCategoryManager(logger, connectionManager);
            return _instance;
        }

        public List<Guid>? GetAllSlotFeatureCategoryId()
        {
            EnsureDefaultCategories();
            List<Guid> ids = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT ID FROM SlotFeatureCategoryTable";
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
                _logger.LogError(ex, "Impossible to get IDs from SlotFeatureCategoryTable");
                return null;
            }
        }

        public List<MetaInfo?>? GetAllSlotFeatureCategoryMetaInfo()
        {
            EnsureDefaultCategories();
            List<MetaInfo?> metaInfos = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT MetaInfo FROM SlotFeatureCategoryTable";
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
                _logger.LogError(ex, "Impossible to get MetaInfo from SlotFeatureCategoryTable");
                return null;
            }
        }

        public Model.SlotFeatureCategory? GetSlotFeatureCategoryById(Guid guid)
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
            command.CommandText = $"SELECT SlotFeatureCategory FROM SlotFeatureCategoryTable WHERE ID = '{guid}'";
            try
            {
                using var reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    Model.SlotFeatureCategory? data = JsonSerializer.Deserialize<Model.SlotFeatureCategory>(reader.GetString(0), JsonSettings.Options);
                    if (data != null && data.MetaInfo != null && data.MetaInfo.ID != guid)
                    {
                        throw new SqliteException("SQLite database corrupted: returned SlotFeatureCategory has the wrong ID.", 1);
                    }
                    return data;
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get SlotFeatureCategory from SlotFeatureCategoryTable");
            }

            return null;
        }

        public List<Model.SlotFeatureCategory?>? GetAllSlotFeatureCategory()
        {
            EnsureDefaultCategories();
            List<Model.SlotFeatureCategory?> values = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT SlotFeatureCategory FROM SlotFeatureCategoryTable";
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    values.Add(JsonSerializer.Deserialize<Model.SlotFeatureCategory>(reader.GetString(0), JsonSettings.Options));
                }
                return values;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get SlotFeatureCategory from SlotFeatureCategoryTable");
                return null;
            }
        }

        public bool AddSlotFeatureCategory(Model.SlotFeatureCategory? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return false;
            }
            if (GetSlotFeatureCategoryById(data.MetaInfo.ID) != null)
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
                command.CommandText = "INSERT INTO SlotFeatureCategoryTable (" +
                    "ID, MetaInfo, Name, IsExclusive, HasValidityPeriod, CreationDate, LastModificationDate, SlotFeatureCategory" +
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
                _logger.LogError(ex, "Impossible to add SlotFeatureCategory");
                return false;
            }
        }

        public bool UpdateSlotFeatureCategoryById(Guid guid, Model.SlotFeatureCategory? data)
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
                command.CommandText = $"UPDATE SlotFeatureCategoryTable SET " +
                    $"MetaInfo = '{metaInfo}', " +
                    $"Name = '{data.Name}', " +
                    $"IsExclusive = {(data.IsExclusive ? 1 : 0)}, " +
                    $"HasValidityPeriod = {(data.HasValidityPeriod ? 1 : 0)}, " +
                    $"CreationDate = '{creationDate}', " +
                    $"LastModificationDate = '{lastModificationDate}', " +
                    $"SlotFeatureCategory = '{serialized}' " +
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
                _logger.LogError(ex, "Impossible to update SlotFeatureCategory");
                return false;
            }
        }

        public bool DeleteSlotFeatureCategoryById(Guid guid)
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
                command.CommandText = $"DELETE FROM SlotFeatureCategoryTable WHERE ID = '{guid}'";
                command.ExecuteNonQuery();
                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to delete SlotFeatureCategory");
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
            command.CommandText = "SELECT COUNT(*) FROM SlotFeatureCategoryTable";
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
                _logger.LogError(ex, "Impossible to count SlotFeatureCategoryTable");
                return;
            }

            foreach (DefaultSlotFeatureCategory defaultCategory in DefaultCategories)
            {
                AddSlotFeatureCategory(CreateDefaultCategory(defaultCategory));
            }
        }

        private static Model.SlotFeatureCategory CreateDefaultCategory(DefaultSlotFeatureCategory defaultCategory) =>
            new()
            {
                MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
                Name = defaultCategory.Name,
                IsExclusive = defaultCategory.IsExclusive,
                HasValidityPeriod = defaultCategory.HasValidityPeriod,
                Options = defaultCategory.Options
                    .Select(option => new Model.SlotFeatureOption { ID = Guid.NewGuid(), Name = option })
                    .ToList()
            };

        private static void PrepareCategory(Model.SlotFeatureCategory category)
        {
            category.Options ??= [];
            foreach (Model.SlotFeatureOption option in category.Options)
            {
                if (option.ID == Guid.Empty)
                {
                    option.ID = Guid.NewGuid();
                }
            }
        }

        private sealed record DefaultSlotFeatureCategory(
            string Name,
            bool IsExclusive,
            bool HasValidityPeriod,
            string[] Options);
    }
}

