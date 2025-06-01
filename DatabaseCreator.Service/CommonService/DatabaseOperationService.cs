using AutoMapper;
using DatabaseCreator.Domain.Dto;
using DatabaseCreator.Domain.Models;
using DatabaseCreator.Domain.Repositories;
using DatabaseCreator.Domain.Services;
using DatabaseCreator.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.WSIdentity;
using System.Xml.Linq;

namespace DatabaseCreator.Service.CommonService
{
    public  class DatabaseOperationService : IDatabaseOperationService
    {
        private readonly IDatabaseOperationRepository _databaseOperationRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<DatabaseOperationService> _logger;

        public DatabaseOperationService(IDatabaseOperationRepository databaseOperationRepository,
                                      IMapper mapper,
                                      ILogger<DatabaseOperationService> logger)
        {
            _databaseOperationRepository = databaseOperationRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public List<string>? SingleExecution(List<string>? databaseNames, string? sqlScriptFilePath = null)
        {
            if (databaseNames == null)
            {
                _logger.LogWarning("SingleExecution called with null databaseNames list.");
                return null;
            }
            if (databaseNames.Count == 0)
            {
                _logger.LogInformation("SingleExecution called with an empty databaseNames list.");
                return new List<string>();
            }
            // ValidateInputParameters(databaseNames); // Already handled by count check for throwing exception

            List<DbInfodto> dbOperationResults = new List<DbInfodto>();
            string? scriptContent = null;

            if (!string.IsNullOrWhiteSpace(sqlScriptFilePath))
            {
                try
                {
                    scriptContent = File.ReadAllText(sqlScriptFilePath);
                    _logger.LogInformation("Successfully read SQL script file {FilePath}", sqlScriptFilePath);
                }
                catch (Exception ex) // Catches FileNotFoundException, IOException, etc.
                {
                    _logger.LogError(ex, "Error reading SQL script file {FilePath}. Script execution will be skipped for all databases in this operation.", sqlScriptFilePath);
                    // scriptContent remains null, so script execution won't be attempted.
                }
            }

            foreach (string dbName in databaseNames)
            {
                bool createdSuccessfully = false;
                try
                {
                    _logger.LogInformation("Attempting to create database {DbName} via SingleExecution.", dbName);
                    _databaseOperationRepository.CreateDbWithSingleExecution(dbName);
                    createdSuccessfully = true;
                    _logger.LogInformation("Successfully created database {DbName}.", dbName);

                    if (createdSuccessfully && !string.IsNullOrWhiteSpace(scriptContent))
                    {
                        _logger.LogInformation("Attempting to execute SQL script on database {DbName}.", dbName);
                        _databaseOperationRepository.ExecuteSqlScript(dbName, scriptContent);
                        _logger.LogInformation("Successfully executed SQL script on database {DbName}.", dbName);
                    }
                }
                catch (DatabaseOperationException ex)
                {
                    // Logged if it's from CreateDbWithSingleExecution or ExecuteSqlScript
                    _logger.LogError(ex, "DatabaseOperationException for database {DbName}. Operation: {OperationName}. Script executed: {ScriptExecuted}", dbName, ex.OperationName, !string.IsNullOrWhiteSpace(scriptContent) && createdSuccessfully);
                    if (ex.OperationName == "CreateDbWithSingleExecution") createdSuccessfully = false; // Ensure status is false if DB creation failed
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error for database {DbName} during SingleExecution. Script executed: {ScriptExecuted}", dbName, !string.IsNullOrWhiteSpace(scriptContent) && createdSuccessfully);
                    createdSuccessfully = false; // Mark as not successful on unexpected error
                }
                dbOperationResults.Add(new DbInfodto { DbName = dbName, IsCreated = createdSuccessfully });
            }

            var mappedDbInfoForHistory = _mapper.Map<List<DbInfo>>(dbOperationResults);
            _databaseOperationRepository.AddCreatedDb(mappedDbInfoForHistory);

            DisplayResult(dbOperationResults);

            return dbOperationResults.Where(x => x.IsCreated).Select(x => x.DbName).ToList();
        }

        public List<string>? Batch(List<string>? databaseNames, string? sqlScriptFilePath = null)
        {
            if (databaseNames == null)
            {
                _logger.LogWarning("Batch operation called with null databaseNames list.");
                return null;
            }
            if (databaseNames.Count == 0)
            {
                _logger.LogInformation("Batch operation called with an empty databaseNames list.");
                return new List<string>();
            }
            // ValidateInputParameters(databaseNames); // Already handled by count check for throwing exception

            List<DbInfodto> dbOperationResults = new List<DbInfodto>();
            // bool batchCreationSuccess = false; // Removed as unused
            string? scriptContent = null;

            if (!string.IsNullOrWhiteSpace(sqlScriptFilePath))
            {
                try
                {
                    scriptContent = File.ReadAllText(sqlScriptFilePath);
                    _logger.LogInformation("Successfully read SQL script file {FilePath} for batch operation.", sqlScriptFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading SQL script file {FilePath} for batch operation. Script execution will be skipped for all databases.", sqlScriptFilePath);
                    // scriptContent remains null
                }
            }

            try
            {
                _logger.LogInformation("Attempting to create {DbCount} databases via Batch operation.", databaseNames.Count);
                _databaseOperationRepository.CreateDbWithBatch(databaseNames);
                // batchCreationSuccess = true; // This was unused and declaration removed
                _logger.LogInformation("Successfully created {DbCount} databases via Batch operation.", databaseNames.Count);

                foreach (var dbname in databaseNames)
                {
                    dbOperationResults.Add(new DbInfodto { DbName = dbname, IsCreated = true });
                }

                if (!string.IsNullOrWhiteSpace(scriptContent))
                {
                    _logger.LogInformation("Attempting to execute SQL script on {DbCount} successfully batch-created databases.", databaseNames.Count);
                    foreach (var dbname in databaseNames) // Iterate only over successfully created dbs
                    {
                        try
                        {
                            _logger.LogInformation("Executing script on batch-created database {DbName}", dbname);
                            _databaseOperationRepository.ExecuteSqlScript(dbname, scriptContent);
                            _logger.LogInformation("Successfully executed script on batch-created database {DbName}", dbname);
                        }
                        catch (DatabaseOperationException ex)
                        {
                            _logger.LogError(ex, "Error executing SQL script on batch-created database {DbName}. Operation: {OperationName}", dbname, ex.OperationName);
                            // Note: DB is already marked as IsCreated=true. Script failure doesn't change this.
                        }
                        catch (Exception ex)
                        {
                             _logger.LogError(ex, "Unexpected error executing SQL script on batch-created database {DbName}", dbname);
                        }
                    }
                }
            }
            catch (DatabaseOperationException ex)
            {
                _logger.LogError(ex, "Error during Batch database creation. Operation: {OperationName}", ex.OperationName);
                foreach (var dbname in databaseNames) // All failed if CreateDbWithBatch throws
                {
                    dbOperationResults.Add(new DbInfodto { DbName = dbname, IsCreated = false });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Batch database creation.");
                foreach (var dbname in databaseNames) // All failed
                {
                    dbOperationResults.Add(new DbInfodto { DbName = dbname, IsCreated = false });
                }
            }

            var mappedDbInfoForHistory = _mapper.Map<List<DbInfo>>(dbOperationResults);
            _databaseOperationRepository.AddCreatedDb(mappedDbInfoForHistory);

            DisplayResult(dbOperationResults);

            return dbOperationResults.Where(x => x.IsCreated == true).Select(x => x.DbName).ToList();
        }

        #region Private methods

        /// <summary>
        /// This method validates the input parameters for creating multiple databases.
        /// It throws an exception if any of these conditions are not met.
        /// </summary>
        /// <param name="dbNames">The list of database names to be created.</param>
        /// <exception cref="InvalidInputException">Thrown when the connection is not open or the list of database names is empty.</exception>
        private void ValidateInputParameters(List<string>? dbNames)
        {
            if (dbNames?.Count == 0)
            {
                throw new InvalidInputException($"The {nameof(dbNames)} is empty.");
            }
        }

        /// <summary>
        /// This method displays the result of creating multiple databases to the console.
        /// It displays a summary message showing how many databases were created and how many failed.
        /// </summary>
        /// <param name="createdDbNames">The list of database names that were created successfully.</param>
        /// <param name="failedDbNames">The list of database names that failed to be created.</param>
        private void DisplayResult(List<DbInfodto> dbs)
        {
            int createdDbCount = dbs.Count(x => x.IsCreated);
            int failedDbCount = dbs.Count - createdDbCount;
            // Using logger for this information as well, though it's also console output.
            // Depending on requirements, this might be Information or Debug level.
            _logger.LogInformation("Operation Summary: {CreatedCount} out of {TotalCount} databases processed successfully. {FailedCount} databases failed.",
                createdDbCount, dbs.Count, failedDbCount);
            Console.WriteLine($"\nSummary: {createdDbCount} out of {dbs.Count} databases were created successfully. {failedDbCount} databases failed to be created.");
        }

        #endregion
    }
}

