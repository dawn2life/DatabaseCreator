using DatabaseCreator.Domain.Services;

namespace DatabaseCreator
{
    internal class App
    {
        private readonly IUserInterfaceService _userInterfaceService;
        private readonly IDatabaseOperationService _databaseOperationService;
        public App(IUserInterfaceService userInterfaceService, 
                   IDatabaseOperationService databaseOperationService)
        {
            _userInterfaceService = userInterfaceService;
            _databaseOperationService = databaseOperationService;
        }

        public void Run()
        {
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
            var createdDatabases = _databaseOperationService.SingleExecution(databaseNames);
            return createdDatabases;
        }

        public List<string>? Batch()
        {
            var databaseNames = GetDatabaseNames();
            var createdDatabases = _databaseOperationService.Batch(databaseNames);
            return createdDatabases;
        }

        #region Private methods

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
