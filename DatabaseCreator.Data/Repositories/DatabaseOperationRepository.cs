using DatabaseCreator.Data.Infrastructure.Connection;
using DatabaseCreator.Domain.Configurations;
using DatabaseCreator.Domain.Dto;
using DatabaseCreator.Domain.Exceptions;
using DatabaseCreator.Domain.Models;
using DatabaseCreator.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient; // Changed
using System.Linq;
using System.Text;
using DatabaseCreator.Data.SqlConstants;
using System.Text.RegularExpressions; // Added for Regex.Split

namespace DatabaseCreator.Data.Repositories
{
    public class DatabaseOperationRepository : IDatabaseOperationRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly string _masterConnectionString;
        private string _currentConnectionMethod = "ado.net"; // Default connection method
        private readonly ILogger<DatabaseOperationRepository> _logger;

        public DatabaseOperationRepository(
            IOptions<ConnectionStrings> connectionStrings,
            IDatabaseConnectionFactory connectionFactory,
            ILogger<DatabaseOperationRepository> logger)
        {
               _masterConnectionString = connectionStrings.Value?.SqlDb?.ConnectionString ?? throw new InvalidOperationException("MasterConnection (SqlDb.ConnectionString) is not configured correctly in connectionstring.json or the ConnectionStrings section is missing.");
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void SetConnectionMethod(string methodName)
        {
            _logger.LogInformation("Attempting to set database connection method to: {ConnectionMethod}", methodName);
            try
            {
                // Validate that a provider can be obtained for this method name.
                _connectionFactory.GetConnectionProvider(methodName);
                _currentConnectionMethod = methodName;
                _logger.LogInformation("Database connection method set to: {ConnectionMethod}", _currentConnectionMethod);
            }
            catch (NotSupportedException ex)
            {
                _logger.LogError(ex, "Failed to set connection method to {ConnectionMethod} because it is not supported. The current method ({CurrentMethod}) will be retained.", methodName, _currentConnectionMethod);
                // Re-throwing to make it clear to the caller that the method was not set.
                throw;
            }
        }

        private IDbConnection GetMasterConnection()
        {
            var provider = _connectionFactory.GetConnectionProvider(_currentConnectionMethod);
            return provider.GetDbConnection(_masterConnectionString);
        }

        private IDbConnection GetTargetConnection(string dbName)
        {
            var provider = _connectionFactory.GetConnectionProvider(_currentConnectionMethod);
            if (string.IsNullOrWhiteSpace(dbName))
            {
                throw new ArgumentException("Database name cannot be null or whitespace.", nameof(dbName));
            }
               var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_masterConnectionString); // Fully qualified
            builder.InitialCatalog = dbName;
            string targetDbConnectionString = builder.ConnectionString;
            return provider.GetDbConnection(targetDbConnectionString);
        }

        public void CreateDbWithSingleExecution(string dbName)
        {
            _logger.LogInformation("Creating database {DbName} using {ConnectionMethod} via SingleExecution.", dbName, _currentConnectionMethod);
            try
            {
                using (var connection = GetMasterConnection())
                {
                    connection.Open(); // Explicitly open the connection
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(DbSqlConstants.CreateDatabaseQuery, dbName);
                        command.ExecuteNonQuery();
                    }
                }
                _logger.LogInformation("Database {DbName} created successfully.", dbName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating database {DbName}.", dbName);
                throw new DatabaseOperationException("CreateDbWithSingleExecution", $"Failed to create database {dbName}.", ex);
            }
        }

        public void CreateDbWithBatch(List<string> databaseNames)
        {
            if (databaseNames == null || !databaseNames.Any())
            {
                _logger.LogWarning("CreateDbWithBatch called with no database names.");
                return;
            }
            _logger.LogInformation("Creating databases ({DbNames}) using {ConnectionMethod} via Batch.", string.Join(", ", databaseNames), _currentConnectionMethod);
            try
            {
                using (var connection = GetMasterConnection())
                {
                    connection.Open(); // Explicitly open the connection
                    var commandText = new StringBuilder();
                    foreach (var dbName in databaseNames)
                    {
                        commandText.AppendLine(string.Format(DbSqlConstants.CreateDatabaseQuery, dbName));
                    }
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = commandText.ToString();
                        command.ExecuteNonQuery();
                    }
                }
                _logger.LogInformation("Successfully created databases in batch: {DbNames}", string.Join(", ", databaseNames));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating databases in batch. DBs: {DbNames}", string.Join(", ", databaseNames));
                throw new DatabaseOperationException("CreateDbWithBatch", "Failed to create databases in batch.", ex);
            }
        }

        public void ExecuteSqlScript(string dbName, string scriptContent)
        {
            _logger.LogInformation("Executing SQL script on database {DbName} using {ConnectionMethod}.", dbName, _currentConnectionMethod);
            try
            {
                using (var connection = GetTargetConnection(dbName))
                {
                    connection.Open(); // Explicitly open the connection
                     var commands = Regex.Split(scriptContent, @"^\s*GO\s*$",
                                          RegexOptions.Multiline | RegexOptions.IgnoreCase);

                     foreach (var cmdText in commands)
                     {
                         if (string.IsNullOrWhiteSpace(cmdText)) continue;
                         using (var command = connection.CreateCommand())
                         {
                             command.CommandText = cmdText;
                             command.ExecuteNonQuery();
                         }
                     }
                }
                _logger.LogInformation("Successfully executed SQL script on database {DbName}.", dbName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL script on database {DbName}.", dbName);
                throw new DatabaseOperationException("ExecuteSqlScript", $"Failed to execute script on database {dbName}.", ex);
            }
        }

        public void LogCreatedDbInfo(List<DbInfo> dbInfo)
        {
            // This method is for logging/auditing and does not require a direct DB connection itself.
            _logger.LogInformation("Recording creation status for {Count} databases.", dbInfo.Count);
            foreach (var db in dbInfo)
            {
                _logger.LogInformation("Database: {DbName}, Created: {IsCreated}", db.DbName, db.IsCreated);
            }
        }
    }
}
