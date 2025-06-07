using DatabaseCreator;
using DatabaseCreator.Domain.Configurations;
using DatabaseCreator.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Microsoft.EntityFrameworkCore;
using DatabaseCreator.Data.EfCore;
using System;

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

        // Configure services from the Service layer (and by extension, Data layer)
        services.ConfigureServiceLayer(hostContext.Configuration);

        services.AddAutoMapper(cfg => cfg.AddProfiles(Register.GetAutoMapperProfiles()));

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var connectionStringsOpt = serviceProvider.GetRequiredService<IOptions<ConnectionStrings>>().Value; // Renamed for clarity
            string? masterConnectionString = connectionStringsOpt?.SqlDb?.ConnectionString;
            if (string.IsNullOrWhiteSpace(masterConnectionString))
            {
                throw new InvalidOperationException("MasterConnection (SqlDb.ConnectionString) is not configured correctly in connectionstring.json or the ConnectionStrings:SqlDb section is missing.");
            }
            options.UseSqlServer(masterConnectionString, sqlServerOptionsAction: sqlOptions =>
            {
                // Example: sqlOptions.EnableRetryOnFailure();
            });
        });
    });

    // Register.ConfigureServiceLayer(hostBuilder); // This line is now removed.
    // Assuming Data layer DI might be part of ConfigureServiceLayer or needs similar registration if separate
    // Example: Register.ConfigureDataLayer(hostBuilder); // This would also need to be inside if it's IServiceCollection extension

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

