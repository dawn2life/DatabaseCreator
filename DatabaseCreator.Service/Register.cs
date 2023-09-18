using AutoMapper;
using DatabaseCreator.Domain.Services;
using DatabaseCreator.Service.CommonService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DatabaseCreator.Service
{
    public class Register
    {
        public static void ConfigureServiceLayer(IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((_, services) => 
            {
                services.AddTransient<IDatabaseOperationService, DatabaseOperationService>();
                services.AddTransient<IUserInterfaceService, UserInterfaceService>();
            });
            Data.Register.ConfigureDataLayer(hostBuilder);
        }

        public static Type[] GetAutoMapperProfiles()
        {
            return typeof(Register).Assembly.GetTypes().Where(z => z.IsSubclassOf(typeof(Profile))).ToArray();
        }
    }
}
