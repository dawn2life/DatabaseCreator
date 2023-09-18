namespace DatabaseCreator.Domain.Services
{
    public interface IDatabaseOperationService
    {
        /// <summary>
        /// This method creates multiple databases using single execution.
        /// </summary>
        /// <param name="databaseNames">A list of database names</param>
        /// <returns>A list of database names that were created successfully.</returns>
        public List<string>? SingleExecution(List<string>? databaseNames);

        /// <summary>
        /// This method creates multiple databases using batch like operation.
        /// </summary>
        /// <param name="databaseNames">A list of database names</param>
        /// <returns>A list of database names that were created successfully.</returns>
        List<string>? Batch(List<string>? databaseNames);
    }
}
