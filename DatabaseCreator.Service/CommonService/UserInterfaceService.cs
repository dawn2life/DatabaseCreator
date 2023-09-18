using DatabaseCreator.Domain.Services;

namespace DatabaseCreator.Service.CommonService
{
    public class UserInterfaceService : IUserInterfaceService
    {
        public void DisplayAppName() 
        {
            Console.Clear();
            Console.WriteLine("--------------------");
            Console.WriteLine("| Database Creator |");
            Console.WriteLine("--------------------");
            Console.WriteLine();
        }

        public void DisplayCommands() 
        {
            Console.WriteLine("1. Single Execution");
            Console.WriteLine("2. Batch");
            Console.WriteLine();
        }
    }
}
