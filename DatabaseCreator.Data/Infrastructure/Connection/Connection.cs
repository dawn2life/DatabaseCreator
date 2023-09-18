using System.Data;
using System.Data.SqlClient;

namespace DatabaseCreator.Data.Infrastructure.Connection
{
    public class Connection : IConnection
    {
        public IDbConnection GetSqlConnection(string connectionString, bool open = false)
        {
            IDbConnection conn = new SqlConnection(connectionString);

            if (conn == null)
                throw new DataException("The Sql provider cannot create a new connection.");

            conn.Open();
            return conn;
        }
    }
}
