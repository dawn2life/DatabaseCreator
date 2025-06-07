using DatabaseCreator.Data;
using DatabaseCreator.Domain.Services;
using DatabaseCreator.Service.CommonService;
using DatabaseCreator.Service.Profiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using System.Collections.Generic;

namespace DatabaseCreator.Service
{
    public static class Register
    {
        public static IServiceCollection ConfigureServiceLayer(this IServiceCollection services, IConfiguration configuration)
        {
            services.ConfigureDataLayer();
            services.AddScoped<IDatabaseOperationService, DatabaseOperationService>();
            services.AddScoped<IUserInterfaceService, UserInterfaceService>();
            return services;
        }
        public static List<Profile> GetAutoMapperProfiles()
        {
            return new List<Profile> { new DataProfiles() };
        }
    }
}
