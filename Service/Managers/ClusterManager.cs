using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.Collections;
using System.Threading.Tasks;

namespace NORCE.Drilling.Cluster.Service.Managers
{
    /// <summary>
    /// A manager for Cluster. The manager implements the singleton pattern as defined by 
    /// Gamma, Erich, et al. "Design patterns: Abstraction and reuse of object-oriented design." 
    /// European Conference on Object-Oriented Programming. Springer, Berlin, Heidelberg, 1993.
    /// </summary>
    public class ClusterManager
    {
        private static ClusterManager? _instance = null;
        private readonly ILogger<ClusterManager> _logger;
        private readonly SqlConnectionManager _connectionManager;

        private ClusterManager(ILogger<ClusterManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static ClusterManager GetInstance(ILogger<ClusterManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new ClusterManager(logger, connectionManager);
            return _instance;
        }

        public int Count
        {
            get
            {
                int count = 0;
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM ClusterTable";
                    try
                    {
                        using SqliteDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            count = (int)reader.GetInt64(0);
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to count records in the ClusterTable");
                    }
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
                return count;
            }
        }

        public bool Clear()
        {
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                bool success = false;
                using var transaction = connection.BeginTransaction();
                try
                {
                    //empty ClusterTable
                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM ClusterTable";
                    command.ExecuteNonQuery();

                    transaction.Commit();
                    success = true;
                }
                catch (SqliteException ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "Impossible to clear the ClusterTable");
                }
                return success;
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }
        }

        public bool Contains(Guid guid)
        {
            int count = 0;
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT COUNT(*) FROM ClusterTable WHERE ID = '{guid}'";
                try
                {
                    using SqliteDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        count = (int)reader.GetInt64(0);
                    }
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to count rows from ClusterTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return count >= 1;
        }

        /// <summary>
        /// Returns the list of Guid of all Cluster present in the microservice database 
        /// </summary>
        /// <returns>the list of Guid of all Cluster present in the microservice database</returns>
        public List<Guid>? GetAllClusterId()
        {
            List<Guid> ids = [];
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT ID FROM ClusterTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        Guid id = reader.GetGuid(0);
                        ids.Add(id);
                    }
                    _logger.LogInformation("Returning the list of ID of existing records from ClusterTable");
                    return ids;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get IDs from ClusterTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of MetaInfo of all Cluster present in the microservice database 
        /// </summary>
        /// <returns>the list of MetaInfo of all Cluster present in the microservice database</returns>
        public List<MetaInfo?>? GetAllClusterMetaInfo()
        {
            List<MetaInfo?> metaInfos = new();
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT MetaInfo FROM ClusterTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        string mInfo = reader.GetString(0);
                        MetaInfo? metaInfo = JsonSerializer.Deserialize<MetaInfo>(mInfo, JsonSettings.Options);
                        metaInfos.Add(metaInfo);
                    }
                    _logger.LogInformation("Returning the list of MetaInfo of existing records from ClusterTable");
                    return metaInfos;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get IDs from ClusterTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the Cluster identified by its Guid from the microservice database 
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>the Cluster identified by its Guid from the microservice database</returns>
        public Model.Cluster? GetClusterById(Guid guid)
        {
            if (!guid.Equals(Guid.Empty))
            {
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    Model.Cluster? cluster;
                    var command = connection.CreateCommand();
                    command.CommandText = $"SELECT Cluster FROM ClusterTable WHERE ID = '{guid}'";
                    try
                    {
                        using var reader = command.ExecuteReader();
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            string data = reader.GetString(0);
                            cluster = JsonSerializer.Deserialize<Model.Cluster>(data, JsonSettings.Options);
                            if (cluster != null && cluster.MetaInfo != null && !cluster.MetaInfo.ID.Equals(guid))
                                throw new SqliteException("SQLite database corrupted: returned Cluster is null or has been jsonified with the wrong ID.", 1);
                        }
                        else
                        {
                            _logger.LogInformation("No Cluster of given ID in the database");
                            return null;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to get the Cluster with the given ID from ClusterTable");
                        return null;
                    }
                    _logger.LogInformation("Returning the Cluster of given ID from ClusterTable");
                    return cluster;
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
            }
            else
            {
                _logger.LogWarning("The given Cluster ID is null or empty");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of all Cluster present in the microservice database 
        /// </summary>
        /// <returns>the list of all Cluster present in the microservice database</returns>
        public List<Model.Cluster?>? GetAllCluster()
        {
            List<Model.Cluster?> vals = [];
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Cluster FROM ClusterTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        string data = reader.GetString(0);
                        Model.Cluster? cluster = JsonSerializer.Deserialize<Model.Cluster>(data, JsonSettings.Options);
                        vals.Add(cluster);
                    }
                    _logger.LogInformation("Returning the list of existing Cluster from ClusterTable");
                    return vals;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get Cluster from ClusterTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Performs calculation on the given Cluster and adds it to the microservice database
        /// </summary>
        /// <param name="cluster"></param>
        /// <returns>true if the given Cluster has been added successfully to the microservice database</returns>
        public bool AddCluster(Model.Cluster? cluster)
        {
            if (cluster != null && cluster.MetaInfo != null && cluster.MetaInfo.ID != Guid.Empty)
            {
                //update ClusterTable
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    using SqliteTransaction transaction = connection.BeginTransaction();
                    bool success = true;
                    try
                    {
                        //add the Cluster to the ClusterTable
                        string metaInfo = JsonSerializer.Serialize(cluster.MetaInfo, JsonSettings.Options);
                        string data = JsonSerializer.Serialize(cluster, JsonSettings.Options);
                        var command = connection.CreateCommand();
                        command.CommandText = "INSERT INTO ClusterTable (" +
                            "ID, " +
                            "MetaInfo, " +
                            "FieldID, " +
                            "IsSingleWell, " +
                            "RigID, " +
                            "IsFixedPlatform, " +
                            "Cluster" +
                            ") VALUES (" +
                            $"'{cluster.MetaInfo.ID}', " +
                            $"'{metaInfo}', " +
                            $"'{(cluster.FieldID != null ? cluster.FieldID : "")}', " +
                            $"'{(cluster.IsSingleWell ? 1 : 0)}', " +
                            $"'{(cluster.RigID != null ? cluster.RigID : "")}', " +
                            $"'{(cluster.IsFixedPlatform ? 1 : 0)}', " +
                            $"'{data}'" +
                            ")";
                        int count = command.ExecuteNonQuery();
                        if (count != 1)
                        {
                            _logger.LogWarning("Impossible to insert the given Cluster into the ClusterTable");
                            success = false;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to add the given Cluster into ClusterTable");
                        success = false;
                    }
                    //finalizing SQL transaction
                    if (success)
                    {
                        transaction.Commit();
                        _logger.LogInformation("Added the given Cluster of given ID into the ClusterTable successfully");
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                    return success;
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
            }
            else
            {
                _logger.LogWarning("The Cluster ID or the ID of its input are null or empty");
            }
            return false;
        }

        /// <summary>
        /// Performs calculation on the given Cluster and updates it in the microservice database
        /// </summary>
        /// <param name="cluster"></param>
        /// <returns>true if the given Cluster has been updated successfully</returns>
        public bool UpdateClusterById(Guid guid, Model.Cluster? cluster)
        {
            bool success = true;
            if (guid != Guid.Empty && cluster != null && cluster.MetaInfo != null && cluster.MetaInfo.ID == guid)
            {
                //update ClusterTable
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    using SqliteTransaction transaction = connection.BeginTransaction();
                    //update fields in ClusterTable
                    try
                    {
                        string metaInfo = JsonSerializer.Serialize(cluster.MetaInfo, JsonSettings.Options);
                        string data = JsonSerializer.Serialize(cluster, JsonSettings.Options);
                        var command = connection.CreateCommand();
                        command.CommandText = $"UPDATE ClusterTable SET " +
                            $"MetaInfo = '{metaInfo}', " +
                            $"FieldID = '{(cluster.FieldID != null ? cluster.FieldID : "")}', " +
                            $"IsSingleWell = '{(cluster.IsSingleWell ? 1 : 0)}', " +
                            $"RigID = '{(cluster.FieldID != null ? cluster.RigID : "")}', " +
                            $"IsFixedPlatform = '{(cluster.IsFixedPlatform ? 1 : 0)}', " +
                            $"Cluster = '{data}' " +
                            $"WHERE ID = '{guid}'";
                        int count = command.ExecuteNonQuery();
                        if (count != 1)
                        {
                            _logger.LogWarning("Impossible to update the Cluster");
                            success = false;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to update the Cluster");
                        success = false;
                    }

                    // Finalizing
                    if (success)
                    {
                        transaction.Commit();
                        _logger.LogInformation("Updated the given Cluster successfully");
                        return true;
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
            }
            else
            {
                _logger.LogWarning("The Cluster ID or the ID of some of its attributes are null or empty");
            }
            return false;
        }

        /// <summary>
        /// Deletes the Cluster of given ID from the microservice database
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>true if the Cluster was deleted from the microservice database</returns>
        public bool DeleteClusterById(Guid guid)
        {
            if (!guid.Equals(Guid.Empty))
            {
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    using var transaction = connection.BeginTransaction();
                    bool success = true;
                    //delete Cluster from ClusterTable
                    try
                    {
                        var command = connection.CreateCommand();
                        command.CommandText = $"DELETE FROM ClusterTable WHERE ID = '{guid}'";
                        int count = command.ExecuteNonQuery();
                        if (count < 0)
                        {
                            _logger.LogWarning("Impossible to delete the Cluster of given ID from the ClusterTable");
                            success = false;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to delete the Cluster of given ID from ClusterTable");
                        success = false;
                    }
                    if (success)
                    {
                        transaction.Commit();
                        _logger.LogInformation("Removed the Cluster of given ID from the ClusterTable successfully");
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                    return success;
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
            }
            else
            {
                _logger.LogWarning("The Cluster ID is null or empty");
            }
            return false;
        }
    }
}