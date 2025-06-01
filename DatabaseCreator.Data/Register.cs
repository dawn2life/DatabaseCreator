using DatabaseCreator.Data.Infrastructure.Connection;
using DatabaseCreator.Data.Repositories;
using DatabaseCreator.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DatabaseCreator.Data
{
    public static class Register
    {
        public static void ConfigureDataLayer(IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((_, services) =>
            {
                services.AddTransient<IConnection, Connection>();
                services.AddTransient<IDatabaseOperationRepository, DatabaseOperationRepository>();
            });
        }
    }
}
