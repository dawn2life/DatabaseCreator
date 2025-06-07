namespace DatabaseCreator.Data.Infrastructure.Connection
{
    public interface IDatabaseConnectionFactory
    {
        IConnectionProvider GetConnectionProvider(string connectionMethodName);
    }
}
