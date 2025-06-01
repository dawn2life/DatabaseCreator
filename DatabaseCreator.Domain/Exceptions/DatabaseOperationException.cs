using System;

namespace DatabaseCreator.Domain.Exceptions
{
    public class DatabaseOperationException : Exception
    {
        public string? OperationName { get; }
        public string? DatabaseName { get; } // Optional, for single DB operations

        public DatabaseOperationException(string message) : base(message)
        {
        }

        public DatabaseOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public DatabaseOperationException(string operationName, string dbName, string message, Exception innerException)
            : base(message, innerException)
        {
            OperationName = operationName;
            DatabaseName = dbName;
        }

        public DatabaseOperationException(string operationName, string message, Exception innerException)
            : base(message, innerException)
        {
            OperationName = operationName;
        }
    }
}
