using DatabaseCreator.Data.Infrastructure.Connection;
using DatabaseCreator.Data.SqlConstants;
using DatabaseCreator.Domain.Configurations;
using DatabaseCreator.Domain.Models;
using DatabaseCreator.Domain.Repositories;
using DatabaseCreator.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;

namespace DatabaseCreator.Data.Repositories
{
    public class DatabaseOperationRepository : IDatabaseOperationRepository
    {
        private readonly IConnection _conn;
        private readonly ConnectionStrings _connStrings;
        private readonly ILogger<DatabaseOperationRepository> _logger;

        public DatabaseOperationRepository(IOptions<ConnectionStrings> connStrings,
                                         IConnection conn,
                                         ILogger<DatabaseOperationRepository> logger)
        {
            _conn = conn;
            _connStrings = connStrings.Value;
            _logger = logger;
        }

        public void CreateDbWithSingleExecution(string dbName)
        {
            using var connection = _conn.GetSqlConnection(_connStrings.SqlDb.ConnectionString);
            using (IDbCommand command = connection.CreateCommand())
            {
                try
                {
                    command.CommandText = DbSqlConstants.CreateDbQuery + dbName;
                    command.ExecuteNonQuery();
                    _logger.LogInformation("Database '{DbName}' created successfully.", dbName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error detail in repository for database {DbName} before throwing DatabaseOperationException.", dbName);
                    throw new DatabaseOperationException("CreateDbWithSingleExecution", dbName, $"Failed to create database '{dbName}'.", ex);
                }
            }
        }

        public void CreateDbWithBatch(List<string> dbNames)
        {
            int counter = 0;
            using var connection = _conn.GetSqlConnection(_connStrings.SqlDb.ConnectionString);
            using (IDbCommand command = connection.CreateCommand())
            {
                try
                {
                    foreach (string dbName in dbNames)
                    {
                        command.CommandText = DbSqlConstants.CreateDbQuery + dbName;
                        command.ExecuteNonQuery();
                        _logger.LogInformation("Database '{DbName}' created successfully in batch.", dbName);
                        counter++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during batch creation for {DbCount} databases. Attempting rollback for {Counter} databases if any were created.", dbNames.Count, counter);
                    try
                    {
                        for (int i = counter; i > 0; i--) // Rollback only successfully created ones in this batch
                        {
                            command.CommandText = DbSqlConstants.DropDbQuery + dbNames[i - 1];
                            command.ExecuteNonQuery();
                            _logger.LogInformation("Database '{DbName}' rolled back!", dbNames[i - 1]);
                        }
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogCritical(rollbackEx, "Critical error during rollback after partial batch failure. Original error message: {OriginalErrorMessage}", ex.Message);
                        throw new DatabaseOperationException("CreateDbWithBatchRollback", $"Critical error during rollback after partial batch failure. Original error: {ex.Message}", rollbackEx);
                    }
                    throw new DatabaseOperationException("CreateDbWithBatch", $"Error during batch creation after {counter} successful operations (if any). Original error: {ex.Message}", ex);
                }
            }
        }

        public void AddCreatedDb(List<DbInfo> dbInfos)
        {
            using var connection = _conn.GetSqlConnection(_connStrings.SqlDb.ConnectionString);
            using (IDbCommand command = connection.CreateCommand())
            {
                _logger.LogInformation("Starting to add/update history for {DbInfoCount} db operations.", dbInfos.Count);
                try
                {
                    command.CommandText = DbSqlConstants.InsertDbInfo;
                    foreach (var dbInfo in dbInfos)
                    {
                        IDataParameter dbNameParam = command.CreateParameter();
                        dbNameParam.ParameterName = "@DbName";
                        dbNameParam.Value = dbInfo.DbName;

                        IDataParameter isCreatedParam = command.CreateParameter();
                        isCreatedParam.ParameterName = "@IsCreated";
                        isCreatedParam.Value = dbInfo.IsCreated;

                        command.Parameters.Clear();
                        command.Parameters.Add(dbNameParam);
                        command.Parameters.Add(isCreatedParam);

                        var rowEffected = command.ExecuteNonQuery();
                        if (rowEffected > 0)
                        {
                            _logger.LogInformation("Inserted/Updated history record for {DbName}, IsCreated: {IsCreatedStatus}", dbInfo.DbName, dbInfo.IsCreated);
                        }
                        else
                        {
                            _logger.LogWarning("No rows affected when trying to insert/update history record for {DbName}", dbInfo.DbName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inserting history records for {DbInfoCount} operations.", dbInfos.Count);
                    // Decide if this should throw or just log. For now, it logs and returns.
                    // Consider if AddCreatedDb failure should propagate an exception.
                }
            }
        }

        public void ExecuteSqlScript(string databaseName, string scriptContent)
        {
            if (string.IsNullOrWhiteSpace(scriptContent))
            {
                _logger.LogWarning("ExecuteSqlScript was called with empty or whitespace script content for database {DatabaseName}. No operation will be performed.", databaseName);
                return;
            }

            var builder = new System.Data.SqlClient.SqlConnectionStringBuilder(_connStrings.SqlDb.ConnectionString);
            builder.InitialCatalog = databaseName;
            string targetDbConnectionString = builder.ConnectionString;

            _logger.LogInformation("Attempting to execute script against database {DatabaseName}.", databaseName);

            using var connection = _conn.GetSqlConnection(targetDbConnectionString);
            using (IDbCommand command = connection.CreateCommand())
            {
                try
                {
                    command.CommandText = scriptContent;
                    command.ExecuteNonQuery();
                    _logger.LogInformation("Successfully executed script against database {DatabaseName}.", databaseName);
                }
                catch (System.Data.SqlClient.SqlException ex) // More specific exception
                {
                    _logger.LogError(ex, "SQL error executing script against database {DatabaseName}.", databaseName);
                    throw new DatabaseOperationException("ExecuteSqlScript", databaseName, $"SQL error executing script against database '{databaseName}'. Error: {ex.Message}", ex);
                }
                catch (Exception ex) // Catch-all for other errors
                {
                    _logger.LogError(ex, "Generic error executing script against database {DatabaseName}.", databaseName);
                    throw new DatabaseOperationException("ExecuteSqlScript", databaseName, $"Generic error executing script against database '{databaseName}'. Error: {ex.Message}", ex);
                }
            }
        }
    }
}
