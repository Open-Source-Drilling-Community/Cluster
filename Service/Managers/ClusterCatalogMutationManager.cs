using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Cluster.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OSDC.Drilling.Cluster.Service.Managers;

internal static class ClusterCatalogMutationManager
{
    public static ClusterMutationResult UpdateClusterCategory(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, ClusterFeatureCategory? value) => UpdateCategory(manager, logger, id, expectedModifiedUtc, value,
            "ClusterFeatureCategoryTable", "ClusterFeatureCategory", value => value.MetaInfo, value => value.CreationDate,
            (value, date) => value.CreationDate = date, value => value.LastModificationDate, (value, date) => value.LastModificationDate = date,
            value => value.Options, option => option.ID, (option, optionId) => option.ID = optionId,
            (connection, transaction, categoryId, options) => ClusterReferenceIntegrityValidator.FindClusterCategoryReferences(connection, transaction, categoryId, options),
            (command, category) => { command.CommandText = "UPDATE ClusterFeatureCategoryTable SET MetaInfo=$meta, Name=$name, IsExclusive=$exclusive, HasValidityPeriod=$validity, CreationDate=$created, LastModificationDate=$modified, ClusterFeatureCategory=$document WHERE ID=$id"; AddCategoryParameters(command, category.Name, category.IsExclusive, category.HasValidityPeriod); });

    public static ClusterMutationResult UpdateSlotCategory(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, SlotFeatureCategory? value) => UpdateCategory(manager, logger, id, expectedModifiedUtc, value,
            "SlotFeatureCategoryTable", "SlotFeatureCategory", value => value.MetaInfo, value => value.CreationDate,
            (value, date) => value.CreationDate = date, value => value.LastModificationDate, (value, date) => value.LastModificationDate = date,
            value => value.Options, option => option.ID, (option, optionId) => option.ID = optionId,
            (connection, transaction, categoryId, options) => ClusterReferenceIntegrityValidator.FindSlotCategoryReferences(connection, transaction, categoryId, options),
            (command, category) => { command.CommandText = "UPDATE SlotFeatureCategoryTable SET MetaInfo=$meta, Name=$name, IsExclusive=$exclusive, HasValidityPeriod=$validity, CreationDate=$created, LastModificationDate=$modified, SlotFeatureCategory=$document WHERE ID=$id"; AddCategoryParameters(command, category.Name, category.IsExclusive, category.HasValidityPeriod); });

    public static ClusterMutationResult UpdateIdentity(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, ClusterIdentity? value) => value?.MetaInfo?.ID != id || id == Guid.Empty
        ? ClusterMutationResult.Invalid("MetaInfo.ID", "id_mismatch", "The route UUID must match MetaInfo.ID.")
        : ExecuteUpdate(manager, logger, id, expectedModifiedUtc, value, "ClusterIdentityTable", "ClusterIdentity",
            value => value.MetaInfo, value => value.CreationDate, (value, date) => value.CreationDate = date,
            value => value.LastModificationDate, (value, date) => value.LastModificationDate = date, null,
            (command, identity) => { command.CommandText = "UPDATE ClusterIdentityTable SET MetaInfo=$meta, Name=$name, CreationDate=$created, LastModificationDate=$modified, ClusterIdentity=$document WHERE ID=$id"; command.Parameters.AddWithValue("$name", identity.Name ?? (object)DBNull.Value); });

    public static ClusterMutationResult DeleteClusterCategory(SqlConnectionManager manager, ILogger logger, Guid id) =>
        Delete(manager, logger, id, "ClusterFeatureCategoryTable", (connection, transaction) => ClusterReferenceIntegrityValidator.FindClusterCategoryReferences(connection, transaction, id));
    public static ClusterMutationResult DeleteSlotCategory(SqlConnectionManager manager, ILogger logger, Guid id) =>
        Delete(manager, logger, id, "SlotFeatureCategoryTable", (connection, transaction) => ClusterReferenceIntegrityValidator.FindSlotCategoryReferences(connection, transaction, id));
    public static ClusterMutationResult DeleteIdentity(SqlConnectionManager manager, ILogger logger, Guid id) =>
        Delete(manager, logger, id, "ClusterIdentityTable", (connection, transaction) => ClusterReferenceIntegrityValidator.FindIdentityReferences(connection, transaction, id));

    private static ClusterMutationResult UpdateCategory<TCategory, TOption>(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, TCategory? value, string table, string documentColumn,
        Func<TCategory, OSDC.DotnetLibraries.General.DataManagement.MetaInfo?> meta, Func<TCategory, DateTimeOffset?> created,
        Action<TCategory, DateTimeOffset?> setCreated, Func<TCategory, DateTimeOffset?> modified, Action<TCategory, DateTimeOffset?> setModified,
        Func<TCategory, List<TOption>?> options, Func<TOption, Guid> optionId, Action<TOption, Guid> setOptionId,
        Func<SqliteConnection, SqliteTransaction, Guid, IReadOnlyCollection<Guid>, ClusterMutationError?> referenceCheck,
        Action<SqliteCommand, TCategory> configure) where TCategory : class
    {
        if (value == null || meta(value)?.ID != id || id == Guid.Empty)
            return ClusterMutationResult.Invalid("MetaInfo.ID", "id_mismatch", "The route UUID must match MetaInfo.ID.");
        List<TOption> categoryOptions = options(value) ?? [];
        foreach (TOption option in categoryOptions.Where(value => optionId(value) == Guid.Empty)) setOptionId(option, Guid.NewGuid());
        List<Guid> ids = categoryOptions.Select(optionId).ToList();
        if (ids.Count != ids.Distinct().Count()) return ClusterMutationResult.Invalid("Options", "duplicate_option_id", "Option UUIDs must be unique within a category.");
        return ExecuteUpdate(manager, logger, id, expectedModifiedUtc, value, table, documentColumn, meta, created, setCreated,
            modified, setModified, (connection, transaction) => referenceCheck(connection, transaction, id, ids), configure);
    }

    private static ClusterMutationResult ExecuteUpdate<T>(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, T value, string table, string documentColumn,
        Func<T, OSDC.DotnetLibraries.General.DataManagement.MetaInfo?> meta, Func<T, DateTimeOffset?> created,
        Action<T, DateTimeOffset?> setCreated, Func<T, DateTimeOffset?> modified, Action<T, DateTimeOffset?> setModified,
        Func<SqliteConnection, SqliteTransaction, ClusterMutationError?>? referenceCheck, Action<SqliteCommand, T> configure) where T : class
    {
        using SqliteConnection? connection = manager.GetConnection();
        if (connection == null) return ClusterMutationResult.StorageFailure();
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            T? stored = Read<T>(connection, transaction, table, documentColumn, id);
            if (stored == null) { transaction.Rollback(); return ClusterMutationResult.NotFound("The catalog definition does not exist."); }
            DateTimeOffset? storedModified = modified(stored);
            if (storedModified == null || storedModified.Value.UtcTicks != expectedModifiedUtc.UtcTicks)
            { transaction.Rollback(); return ClusterMutationResult.ConcurrencyConflict($"Expected {expectedModifiedUtc:O}, but the stored definition was modified at {storedModified:O}."); }
            ClusterMutationError? referenceError = referenceCheck?.Invoke(connection, transaction);
            if (referenceError != null) { transaction.Rollback(); return ClusterMutationResult.ReferenceConflict(referenceError); }
            setCreated(value, created(stored));
            setModified(value, DateTimeOffset.UtcNow);
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            configure(command, value);
            command.Parameters.AddWithValue("$id", id.ToString());
            command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(meta(value), JsonSettings.Options));
            command.Parameters.AddWithValue("$created", created(value)?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$modified", modified(value)?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$document", JsonSerializer.Serialize(value, JsonSettings.Options));
            if (command.ExecuteNonQuery() != 1) { transaction.Rollback(); return ClusterMutationResult.StorageFailure(); }
            transaction.Commit(); return ClusterMutationResult.Success();
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        { transaction.Rollback(); logger.LogError(ex, "Unable to update {Table} record {RecordId}", table, id); return ClusterMutationResult.StorageFailure(); }
    }

    private static ClusterMutationResult Delete(SqlConnectionManager manager, ILogger logger, Guid id, string table,
        Func<SqliteConnection, SqliteTransaction, ClusterMutationError?> referenceCheck)
    {
        if (id == Guid.Empty) return ClusterMutationResult.Invalid("id", "invalid_id", "A non-empty UUID is required.");
        using SqliteConnection? connection = manager.GetConnection();
        if (connection == null) return ClusterMutationResult.StorageFailure();
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            using (SqliteCommand exists = connection.CreateCommand()) { exists.Transaction = transaction; exists.CommandText = $"SELECT COUNT(*) FROM {table} WHERE ID=$id"; exists.Parameters.AddWithValue("$id", id.ToString()); if (Convert.ToInt64(exists.ExecuteScalar()) == 0) { transaction.Rollback(); return ClusterMutationResult.NotFound("The catalog definition does not exist."); } }
            ClusterMutationError? error = referenceCheck(connection, transaction);
            if (error != null) { transaction.Rollback(); return ClusterMutationResult.ReferenceConflict(error); }
            using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = $"DELETE FROM {table} WHERE ID=$id"; command.Parameters.AddWithValue("$id", id.ToString());
            if (command.ExecuteNonQuery() != 1) { transaction.Rollback(); return ClusterMutationResult.StorageFailure(); }
            transaction.Commit(); return ClusterMutationResult.Success();
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        { transaction.Rollback(); logger.LogError(ex, "Unable to delete {Table} record {RecordId}", table, id); return ClusterMutationResult.StorageFailure(); }
    }

    private static T? Read<T>(SqliteConnection connection, SqliteTransaction transaction, string table, string column, Guid id)
    { using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = $"SELECT {column} FROM {table} WHERE ID=$id"; command.Parameters.AddWithValue("$id", id.ToString()); return command.ExecuteScalar() is string json ? JsonSerializer.Deserialize<T>(json, JsonSettings.Options) : default; }
    private static void AddCategoryParameters(SqliteCommand command, string? name, bool exclusive, bool validity)
    { command.Parameters.AddWithValue("$name", name ?? (object)DBNull.Value); command.Parameters.AddWithValue("$exclusive", exclusive ? 1 : 0); command.Parameters.AddWithValue("$validity", validity ? 1 : 0); }
}
