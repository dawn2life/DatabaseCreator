using DatabaseCreator.Domain.Services;
using Microsoft.Extensions.Logging;
using System;

namespace DatabaseCreator.Service.CommonService
{
    public class UserInterfaceService : IUserInterfaceService
    {
        private readonly ILogger<UserInterfaceService> _logger;

        public UserInterfaceService(ILogger<UserInterfaceService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void DisplayAppName()
        {
            Console.WriteLine("******************************************");
            Console.WriteLine("************ DATABASE CREATOR ************");
            Console.WriteLine("******************************************");
        }

        public void DisplayCommands()
        {
            Console.WriteLine("\nCOMMANDS:");
            Console.WriteLine("1. Create database(s) with single execution.");
            Console.WriteLine("2. Create database(s) with batch.");
            Console.WriteLine("0. Exit.");
        }

        public string GetConnectionStringInput()
        {
            _logger.LogInformation("GetConnectionStringInput called. Input currently not used to override configured connection string.");
            Console.WriteLine("\n(Optional) Enter SQL Server Connection String (press Enter to use the one from configuration):");
            return Console.ReadLine()?.Trim() ?? "";
        }

        public string GetConnectionMethodChoice()
        {
            Console.WriteLine("\nChoose a database connection method:");
            Console.WriteLine("  1. ADO.NET (Default)");
            Console.WriteLine("  2. EF Core");
            Console.WriteLine("  3. Dapper");
            Console.Write("Enter your choice (1-3, default is 1): ");
            string? choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "2":
                    _logger.LogInformation("User selected EF Core connection method.");
                    return "efcore";
                case "3":
                    _logger.LogInformation("User selected Dapper connection method.");
                    return "dapper";
                case "1":
                default:
                    _logger.LogInformation("User selected ADO.NET or entered invalid/empty choice ('{Choice}'), defaulting to ADO.NET.", choice);
                    if (!string.IsNullOrEmpty(choice) && choice != "1")
                        Console.WriteLine("Invalid choice. Using default ADO.NET.");
                    else if (string.IsNullOrEmpty(choice))
                        Console.WriteLine("No choice entered. Using default ADO.NET.");
                    return "ado.net";
            }
        }

        public void DisplayMessage(string message, bool isError = false)
        {
            if (isError)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nERROR: {message}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n{message}");
                Console.ResetColor();
            }
        }
    }
}
