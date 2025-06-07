using System.Collections.Generic;
using DatabaseCreator.Domain.Dto; // Added for OperationResultDto

namespace DatabaseCreator.Domain.Services
{
    public interface IDatabaseOperationService
    {
        void SetDatabaseConnectionMethod(string connectionMethodName);
        OperationResultDto? SingleExecution(List<string>? databaseNames, string? sqlScriptFilePath = null);
        OperationResultDto? Batch(List<string>? databaseNames, string? sqlScriptFilePath = null);
    }
}
