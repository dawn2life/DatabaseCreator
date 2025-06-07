namespace DatabaseCreator.Domain.Services
{
    public interface IUserInterfaceService
    {
        void DisplayAppName();
        void DisplayCommands();
        string GetConnectionStringInput();
        string GetConnectionMethodChoice();
        void DisplayMessage(string message, bool isError = false);
    }
}
