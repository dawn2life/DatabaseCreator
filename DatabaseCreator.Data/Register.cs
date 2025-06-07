using DatabaseCreator.Data.Infrastructure.Connection;
using DatabaseCreator.Data.Repositories;
using DatabaseCreator.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DatabaseCreator.Data
{
    public static class Register
    {
        public static IServiceCollection ConfigureDataLayer(this IServiceCollection services)
        {
            services.AddSingleton<IDatabaseConnectionFactory, DatabaseConnectionFactory>();
            services.AddScoped<IDatabaseOperationRepository, DatabaseOperationRepository>();
            return services;
        }
    }
}
