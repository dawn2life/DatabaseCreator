using System;

namespace DatabaseCreator.Data.Infrastructure.Connection
{
    public class DatabaseConnectionFactory : IDatabaseConnectionFactory
    {
        public IConnectionProvider GetConnectionProvider(string connectionMethodName)
        {
            switch (connectionMethodName?.ToLowerInvariant())
            {
                case "ado.net":
                    return new AdoNetConnectionProvider();
                case "efcore":
                    return new EfCoreConnectionProvider();
                case "dapper": // Added case
                    return new DapperConnectionProvider();
                default:
                    // Throw an exception for unsupported types. UI will need to handle this.
                    throw new NotSupportedException($"Connection method '{connectionMethodName}' is not supported.");
            }
        }
    }
}
