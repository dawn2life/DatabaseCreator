using DatabaseCreator;
using DatabaseCreator.Domain.Configurations;
using DatabaseCreator.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

// Configure Serilog static logger for early bootstrap logging if needed
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args); // Pass args if available/needed

    hostBuilder.UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(hostingContext.Configuration) // Optional: if you want to configure sinks via appsettings.json
        .Enrich.FromLogContext()
        .MinimumLevel.Debug() // Example: Set minimum level
        .WriteTo.Console()
        .WriteTo.File("logs/database_creator_log_.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information) // Example: File log level
    );

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
    // Assuming Data layer DI might be part of ConfigureServiceLayer or needs similar registration if separate
    // Example: Register.ConfigureDataLayer(hostBuilder);

    using IHost host = hostBuilder.Build();
    var services = host.Services;
    var logger = services.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Application starting up.");
    services.GetRequiredService<App>().Run();
    logger.LogInformation("Application shutting down successfully.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup or execution.");
}
finally
{
    Log.CloseAndFlush();
}

