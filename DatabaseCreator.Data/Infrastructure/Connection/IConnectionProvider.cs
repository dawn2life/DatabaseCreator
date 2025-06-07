using System.Data;

namespace DatabaseCreator.Data.Infrastructure.Connection
{
    public interface IConnectionProvider
    {
        IDbConnection GetDbConnection(string connectionString);
    }
}
