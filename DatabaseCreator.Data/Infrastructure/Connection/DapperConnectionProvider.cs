using System.Data;
using System.Data.SqlClient; // Or the generic System.Data.Common for broader compatibility if needed

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
