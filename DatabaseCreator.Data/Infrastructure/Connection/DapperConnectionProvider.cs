using System.Data;
using Microsoft.Data.SqlClient; // Changed from System.Data.SqlClient

namespace DatabaseCreator.Data.Infrastructure.Connection
{
    public class DapperConnectionProvider : IConnectionProvider
    {
        public IDbConnection GetDbConnection(string connectionString)
        {
            // Dapper works with any IDbConnection, so SqlConnection is fine here.
            // Like other providers, it returns a new connection instance.
            return new SqlConnection(connectionString);
        }
    }
}
