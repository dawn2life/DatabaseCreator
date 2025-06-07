using System.Data;
using Microsoft.Data.SqlClient; // Changed

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
