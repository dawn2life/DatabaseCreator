using System.Data;
using System.Data.SqlClient;

namespace DatabaseCreator.Data.Infrastructure.Connection
{
    public class AdoNetConnectionProvider : IConnectionProvider
    {
        public IDbConnection GetDbConnection(string connectionString)
        {
            // The consumer (repository) will manage the connection's lifecycle (open/close).
            return new SqlConnection(connectionString);
        }
    }
}
