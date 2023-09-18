namespace DatabaseCreator.Domain.Services
{
    public interface IUserInterfaceService
    {
        /// <summary>
        /// User interface for app name 
        /// </summary>
        public void DisplayAppName();

        /// <summary>
        /// User interface for commands
        /// </summary>
        public void DisplayCommands();
    }
}
