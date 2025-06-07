using AutoMapper;
using DatabaseCreator.Domain.Dto;
using DatabaseCreator.Domain.Models;
using DatabaseCreator.Domain.Repositories;
using DatabaseCreator.Domain.Services;
using DatabaseCreator.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.WSIdentity;
using System.Xml.Linq;
using System.IO; // Added for File.ReadAllText

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

        public void SetDatabaseConnectionMethod(string connectionMethodName)
        {
            _logger.LogInformation("Service attempting to set database connection method to: {ConnectionMethod}", connectionMethodName);
            try
            {
                _databaseOperationRepository.SetConnectionMethod(connectionMethodName);
                _logger.LogInformation("Service successfully set database connection method to: {ConnectionMethod}", connectionMethodName);
            }
            catch (NotSupportedException ex)
            {
                _logger.LogError(ex, "Service failed to set database connection method to {ConnectionMethod} as it's not supported.", connectionMethodName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred in Service while setting database connection method to {ConnectionMethod}.", connectionMethodName);
                throw;
            }
        }

        public OperationResultDto? SingleExecution(List<string>? databaseNames, string? sqlScriptFilePath = null)
        {
            if (databaseNames == null)
            {
                _logger.LogWarning("SingleExecution called with null databaseNames list.");
                return new OperationResultDto { Success = false, SummaryMessage = "Input database names list was null." };
            }
            if (databaseNames.Count == 0)
            {
                _logger.LogInformation("SingleExecution called with an empty databaseNames list.");
                return new OperationResultDto { Success = true, SummaryMessage = "No databases requested for creation." }; // Success true as no work needed
            }

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
            _databaseOperationRepository.LogCreatedDbInfo(mappedDbInfoForHistory);

            string summaryMessage = FormatOperationSummary(dbOperationResults);
            var createdDbs = dbOperationResults.Where(x => x.IsCreated).Select(x => x.DbName).ToList();

            return new OperationResultDto
            {
                CreatedDatabaseNames = createdDbs,
                SummaryMessage = summaryMessage,
                Success = createdDbs.Any() || !databaseNames.Any() // Success if any created OR if no dbs were requested
            };
        }

        public OperationResultDto? Batch(List<string>? databaseNames, string? sqlScriptFilePath = null)
        {
            if (databaseNames == null)
            {
                _logger.LogWarning("Batch operation called with null databaseNames list.");
                return new OperationResultDto { Success = false, SummaryMessage = "Input database names list was null." };
            }
            if (databaseNames.Count == 0)
            {
                _logger.LogInformation("Batch operation called with an empty databaseNames list.");
                return new OperationResultDto { Success = true, SummaryMessage = "No databases requested for creation in batch." };
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
            _databaseOperationRepository.LogCreatedDbInfo(mappedDbInfoForHistory);

            string summaryMessage = FormatOperationSummary(dbOperationResults);
            var createdDbs = dbOperationResults.Where(x => x.IsCreated).Select(x => x.DbName).ToList();

            return new OperationResultDto
            {
                CreatedDatabaseNames = createdDbs,
                SummaryMessage = summaryMessage,
                Success = createdDbs.Count == databaseNames.Count // For batch, success means all requested DBs were made
            };
        }

        #region Private methods
        private string FormatOperationSummary(List<DbInfodto> dbs)
        {
            int createdDbCount = dbs.Count(x => x.IsCreated);
            int failedDbCount = dbs.Count - createdDbCount;
            string summary = $"Summary: {createdDbCount} out of {dbs.Count} databases processed. {createdDbCount} created, {failedDbCount} failed.";

            _logger.LogInformation("Operation Summary: {CreatedCount} out of {TotalCount} databases processed successfully. {FailedCount} databases failed.",
                createdDbCount, dbs.Count, failedDbCount);
            // Console.WriteLine($"\nSummary: {createdDbCount} out of {dbs.Count} databases were created successfully. {failedDbCount} databases failed to be created.");
            return summary;
        }

        #endregion
    }
}

