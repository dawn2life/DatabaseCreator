using DatabaseCreator.Domain.Models;

namespace DatabaseCreator.Domain.Repositories
{
    public interface IDatabaseOperationRepository
    {
        /// <summary>
        /// This method creates a database.
        /// </summary>
        /// <param name="dbName">The name of the database to be created</param>
        public void CreateDbWithSingleExecution(string dbName);

        /// <summary>
        /// This method creates a database using batch like operation.
        /// </summary>
        /// <param name="dbNames"></param>
        public void CreateDbWithBatch(List<string> dbNames);

        /// <summary>
        /// Insert the created databases into DbInfo table.
        /// </summary>
        /// <param name="dbInfos"></param>
        /// <returns></returns>
        void AddCreatedDb(List<DbInfo> dbInfos);

        /// <summary>
        /// Executes a given SQL script against the specified database.
        /// </summary>
        /// <param name="databaseName">The name of the database to connect to.</param>
        /// <param name="scriptContent">The SQL script content to execute.</param>
        void ExecuteSqlScript(string databaseName, string scriptContent);
    }
}