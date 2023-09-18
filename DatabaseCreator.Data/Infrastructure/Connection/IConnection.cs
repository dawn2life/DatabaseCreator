using System.Data;
using System.Data.SqlClient;

namespace DatabaseCreator.Data.Infrastructure.Connection
{
    public interface IConnection
    {
        /// <summary>
        /// Gets an open Sql connection based on connection string
        /// </summary>
        /// <param name="connectionString">The connection string</param>
        /// <param name="open">Whether to open the connection or not, by default the connection will be open</param>
        /// <returns>IDbConnection</returns>
        IDbConnection GetSqlConnection(string connectionString, bool open = true);
    }
}