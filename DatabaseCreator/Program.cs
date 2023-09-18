using DatabaseCreator;
using DatabaseCreator.Domain.Configurations;
using DatabaseCreator.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

IHostBuilder hostBuilder = Host.CreateDefaultBuilder();
hostBuilder.ConfigureAppConfiguration(config =>
{
    config.AddJsonFile
    (
        "connectionstring.json",
        optional: false,
        reloadOnChange: true
    );
});

hostBuilder.ConfigureServices((hostContext, services) =>
{
    services.Configure<ConnectionStrings>(hostContext.Configuration.GetSection(nameof(ConnectionStrings)));
    services.AddSingleton<App>();
    services.AddAutoMapper(Register.GetAutoMapperProfiles());
});

Register.ConfigureServiceLayer(hostBuilder);

using IHost host = hostBuilder.Build();
var services = host.Services;

try
{
    services.GetRequiredService<App>().Run();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

