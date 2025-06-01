using DatabaseCreator.Domain.Services;
using Microsoft.Extensions.Logging;

namespace DatabaseCreator
{
    internal class App
    {
        private readonly IUserInterfaceService _userInterfaceService;
        private readonly IDatabaseOperationService _databaseOperationService;
        private readonly ILogger<App> _logger;

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
            int selectedOption;
            do
            {
                _userInterfaceService.DisplayAppName();
                _userInterfaceService.DisplayCommands();
                selectedOption = SelectOption();
                if (selectedOption != 0) 
                {
                    ExecuteOperation(selectedOption);
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadLine();
                    Console.Clear();
                }
            } 
            while (selectedOption != 0);
        }

        public List<string>? SingleExecution() 
        {
            var databaseNames = GetDatabaseNames();
            if (databaseNames == null || databaseNames.Count == 0)
            {
                _logger.LogInformation("No database names provided for SingleExecution in App.");
                // Return empty list or null based on how GetDatabaseNames handles empty/cancelled input
                // Assuming GetDatabaseNames might return null or empty if user provides no names.
                return new List<string>();
            }
            string? sqlScriptPath = GetSqlScriptPathFromUser();
            var createdDatabases = _databaseOperationService.SingleExecution(databaseNames, sqlScriptPath);
            return createdDatabases;
        }

        public List<string>? Batch()
        {
            var databaseNames = GetDatabaseNames();
            if (databaseNames == null || databaseNames.Count == 0)
            {
                _logger.LogInformation("No database names provided for Batch execution in App.");
                return new List<string>();
            }
            string? sqlScriptPath = GetSqlScriptPathFromUser();
            var createdDatabases = _databaseOperationService.Batch(databaseNames, sqlScriptPath);
            return createdDatabases;
        }

        #region Private methods

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
                    Console.WriteLine("No script path entered. Script execution will be skipped.");
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
                Console.WriteLine("Please enter an option or '0' to exit:");
                string input = Console.ReadLine();
                valid = int.TryParse(input, out option);
                if (!valid)
                {
                    Console.WriteLine("Invalid input. Try again...\n");
                }
            }
            while (!valid);

            return option;
        }

        private void ExecuteOperation(int option)
        {
            switch (option)
            {
                case 1:
                    var createdDbs = SingleExecution();
                    if (createdDbs == null) 
                    {
                        Console.WriteLine("No database created!");
                        return;
                    }
                        
                    Console.WriteLine("\nFinal list of created databases: ");
                    foreach (var db in createdDbs.Select((value, i) => (value, i))) 
                    {
                        Console.WriteLine($"{db.i+1}. {db.value}");
                    }
                    break;

                case 2:
                    var createdDbsFromTransaction = Batch();
                    if (createdDbsFromTransaction == null) 
                    {
                        Console.WriteLine("No database created!");
                        return;
                    }

                    Console.WriteLine("\nFinal list of created databases: ");
                    foreach (var db in createdDbsFromTransaction.Select((value, i) => (value, i)))
                    {
                        Console.WriteLine($"{db.i + 1}. {db.value}");
                    }
                    break;

                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }

        private List<string>? GetDatabaseNames()
        {
            bool valid;
            int limit;

            do
            {
                Console.WriteLine("\nHow many database you want to create?");
                string input = Console.ReadLine();

                valid = int.TryParse(input, out limit);
                if (!valid)
                {
                    Console.WriteLine("Invalid input. Try again...\n");
                }
            }
            while (!valid);

            List<string> dbNames = new List<string>();

            Console.WriteLine($"Please enter {limit} database names: ");
            for (int i = 0; i < limit; i++) 
            {
                dbNames.Add(Console.ReadLine());
            }
            Console.WriteLine();

            return dbNames;
        }

        #endregion
    }
}
