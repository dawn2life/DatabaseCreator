using DatabaseCreator.Domain.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DatabaseCreator
{
    internal class App
    {
        private readonly IUserInterfaceService _userInterfaceService;
        private readonly IDatabaseOperationService _databaseOperationService;
        private readonly ILogger<App> _logger;
        private string _currentConnectionMethod = "ado.net";

        public App(IUserInterfaceService userInterfaceService,
                   IDatabaseOperationService databaseOperationService,
                   ILogger<App> logger)
        {
            _userInterfaceService = userInterfaceService;
            _databaseOperationService = databaseOperationService;
            _logger = logger;
        }

        public void Run()
        {
            _logger.LogInformation("App.Run started.");
            _userInterfaceService.DisplayAppName();

            // Connection method choice remains part of App setup
            _currentConnectionMethod = _userInterfaceService.GetConnectionMethodChoice();
            try
            {
                _databaseOperationService.SetDatabaseConnectionMethod(_currentConnectionMethod);
                _userInterfaceService.DisplayMessage($"Operations will use: {_currentConnectionMethod.ToUpperInvariant()}", false);
            }
            catch (NotSupportedException ex)
            {
                _logger.LogError(ex, "Failed to set initial connection method to {ConnectionMethod}.", _currentConnectionMethod);
                _userInterfaceService.DisplayMessage($"The chosen connection method '{_currentConnectionMethod}' is not supported. Exiting.", true);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while setting the initial connection method.");
                _userInterfaceService.DisplayMessage($"An unexpected error occurred: {ex.Message}. Exiting.", true);
                return;
            }

            int selectedOption;
            do
            {
                _userInterfaceService.DisplayCommands();
                selectedOption = SelectOption();
                if (selectedOption != 0)
                {
                    ExecuteOperation(selectedOption); // ExecuteOperation will now handle displaying the summary
                    // _userInterfaceService.DisplayMessage("Operation processing complete. Press any key to continue...", false); // Moved to ExecuteOperation
                    Console.WriteLine("\nPress any key to return to the main menu...");
                    Console.ReadLine();
                    Console.Clear();
                    _userInterfaceService.DisplayAppName();
                }
            }
            while (selectedOption != 0);
            _userInterfaceService.DisplayMessage("Exiting application.", false);
        }

        // Removed App.SingleExecution and App.Batch as service calls are now in ExecuteOperation

        private void ExecuteOperation(int option)
        {
            var databaseNames = GetDatabaseNames();
            if (databaseNames == null || !databaseNames.Any()) // Combined null and empty check
            {
                _logger.LogInformation("No database names provided for operation in App.");
                _userInterfaceService.DisplayMessage("No database names were entered. Returning to main menu.", true);
                return;
            }
            string? sqlScriptPath = GetSqlScriptPathFromUser();

            Domain.Dto.OperationResultDto? result = null; // Ensure Domain.Dto is used or add using statement

            switch (option)
            {
                case 1:
                    _logger.LogInformation("App calling SingleExecution service.");
                    result = _databaseOperationService.SingleExecution(databaseNames, sqlScriptPath);
                    break;
                case 2:
                    _logger.LogInformation("App calling Batch service.");
                    result = _databaseOperationService.Batch(databaseNames, sqlScriptPath);
                    break;
                default:
                    _userInterfaceService.DisplayMessage("Invalid option selected.", true);
                    return;
            }

            if (result != null)
            {
                _userInterfaceService.DisplayMessage(result.SummaryMessage, !result.Success);
            }
            else
            {
                _userInterfaceService.DisplayMessage("Operation did not return a result.", true);
            }
        }

         private string? GetSqlScriptPathFromUser()
         {
             Console.WriteLine("\nDo you want to execute a SQL script on the created database(s)? (y/n)");
             string? choice = Console.ReadLine()?.Trim().ToLower();

             if (choice == "y")
             {
                 Console.WriteLine("Enter the full path to the SQL script file:");
                 string? scriptPath = Console.ReadLine()?.Trim();
                 if (!string.IsNullOrWhiteSpace(scriptPath))
                 {
                     return scriptPath;
                 }
                 else
                 {
                     _logger.LogWarning("No SQL script path entered, or path was whitespace. Skipping script execution.");
                     _userInterfaceService.DisplayMessage("No script path entered. Script execution will be skipped.", true);
                     return null;
                 }
             }
             _logger.LogInformation("User chose not to execute a SQL script.");
             return null;
         }

         private int SelectOption()
         {
             int option;
             bool valid;
             do
             {
                 Console.Write("\nPlease enter an option or '0' to exit: ");
                 string? input = Console.ReadLine();
                 valid = int.TryParse(input, out option) && (option >= 0 && option <= 2);
                 if (!valid)
                 {
                     _userInterfaceService.DisplayMessage("Invalid input. Please enter a valid option (0-2).", true);
                 }
             }
             while (!valid);
             return option;
         }

         private List<string>? GetDatabaseNames()
         {
             int limit;
             bool valid;
             string? input;
             do
             {
                 Console.Write("\nHow many databases do you want to create? ");
                 input = Console.ReadLine();
                 valid = int.TryParse(input, out limit) && limit > 0;
                 if (!valid)
                 {
                      _userInterfaceService.DisplayMessage("Invalid input. Please enter a positive number greater than 0.", true);
                 }
             }
             while (!valid);

             List<string> dbNames = new List<string>();
             Console.WriteLine($"Please enter {limit} database name(s): ");
             for (int i = 0; i < limit; i++)
             {
                 Console.Write($"Database {i + 1}: ");
                 string? dbName = Console.ReadLine()?.Trim();
                 if (string.IsNullOrWhiteSpace(dbName))
                 {
                     _userInterfaceService.DisplayMessage("Database name cannot be empty. Please try again.", true);
                     i--;
                     continue;
                 }
                 dbNames.Add(dbName);
             }
             Console.WriteLine();
             return dbNames;
         }
    }
}
