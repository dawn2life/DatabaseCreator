namespace DatabaseCreator.Domain.Configurations
{
    public class ConnectionStrings
    {
        public ConnectionInfo? SqlDb { get; set; }
    }

    public class ConnectionInfo
    {
        public string? Name { get; set; }
        public string? ConnectionString { get; set; }
        public string? ProviderName { get; set; }
    }
}
