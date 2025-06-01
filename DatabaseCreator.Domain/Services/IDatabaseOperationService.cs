namespace DatabaseCreator.Domain.Services
{
    public interface IDatabaseOperationService
    {
        /// <summary>
        /// This method creates multiple databases using single execution.
        /// </summary>
        /// <param name="databaseNames">A list of database names</param>
        /// <param name="sqlScriptFilePath">Optional path to a SQL script file to execute after database creation.</param>
        /// <returns>A list of database names that were created successfully.</returns>
        public List<string>? SingleExecution(List<string>? databaseNames, string? sqlScriptFilePath = null);

        /// <summary>
        /// This method creates multiple databases using batch like operation.
        /// </summary>
        /// <param name="databaseNames">A list of database names</param>
        /// <param name="sqlScriptFilePath">Optional path to a SQL script file to execute after each successful database creation in the batch.</param>
        /// <returns>A list of database names that were created successfully.</returns>
        List<string>? Batch(List<string>? databaseNames, string? sqlScriptFilePath = null);
    }
}
