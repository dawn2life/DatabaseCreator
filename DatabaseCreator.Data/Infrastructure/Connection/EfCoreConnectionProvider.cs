using System.Data;
using System.Data.SqlClient;

namespace DatabaseCreator.Data.Infrastructure.Connection
{
    public class EfCoreConnectionProvider : IConnectionProvider
    {
        public IDbConnection GetDbConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }
    }
}
