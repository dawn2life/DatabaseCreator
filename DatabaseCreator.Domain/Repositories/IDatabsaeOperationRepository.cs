using DatabaseCreator.Domain.Models;

namespace DatabaseCreator.Domain.Repositories
{
    public interface IDatabsaeOperationRepository
    {
        /// <summary>
        /// This method creates a database.
        /// </summary>
        /// <param name="dbName">The name of the database to be created</param>
        /// <returns>true</returns>
        public bool CreateDbWithSingleExecution(string dbName);

        /// <summary>
        /// This method creates a database using batch like operation.
        /// </summary>
        /// <param name="dbNames"></param>
        /// <returns></returns>
        public bool CreateDbWithBatch(List<string> dbNames);

        /// <summary>
        /// Insert the created databases into DbInfo table.
        /// </summary>
        /// <param name="dbInfos"></param>
        /// <returns></returns>
        void AddCreatedDb(List<DbInfo> dbInfos);
    }
}