# Database Creator Utility

## Overview

The Database Creator Utility is a .NET console application designed to simplify the creation of SQL Server databases. It allows users to create one or more databases either individually or in a batch. Optionally, users can also specify a SQL script file to be executed against each newly created database. The application provides feedback via console messages and detailed logging for troubleshooting.

## Configuration

Configuration for the application, primarily the database connection string, is managed through the `connectionstring.json` file located in the `DatabaseCreator` project directory (and typically copied to the output directory during build).

### `connectionstring.json`

This file should contain the connection string for connecting to your SQL Server instance. The connection string must have permissions to create databases. It's recommended to point the `Database` (or `Initial Catalog`) property in the connection string to `master` or a similar system database from which new databases can be created.

**Example `connectionstring.json` structure:**

```json
{
  "ConnectionStrings": {
    "SqlDb": {
      "ConnectionString": "Server=your_server_address;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
    }
  }
}
```

Replace `your_server_address` with the actual address of your SQL Server instance (e.g., `localhost`, `(localdb)\\mssqllocaldb`, `your_server.database.windows.net`).

## How to Run

1.  Ensure you have the .NET SDK installed (version 8.0 or later recommended).
2.  Clone the repository or navigate to the project's root directory.
3.  The `connectionstring.json` file needs to be present in the `DatabaseCreator/bin/Debug/net8.0` (or similar, depending on build configuration) directory when running. It's typically copied from the `DatabaseCreator` project directory. Configure it as described above.
4.  Open a terminal or command prompt in the root directory.
5.  Run the application using the following command:
    ```bash
    dotnet run --project DatabaseCreator/DatabaseCreator.csproj
    ```

## Features

### Database Creation

The application offers two modes for creating databases:

*   **Single Execution Mode (Option 1):**
    *   Databases are created one by one.
    *   If an error occurs while creating one database, it does not prevent attempts to create subsequent databases in the list.
    *   The success or failure of each creation is logged.
*   **Batch Execution Mode (Option 2):**
    *   Databases are created within a single transaction (if the underlying repository supports it with `CreateDbWithBatch`).
    *   If any database creation fails within the batch, the repository attempts to roll back any databases created within that specific batch operation.
    *   The success or failure of the entire batch operation is logged.

In both modes, the user is prompted to enter the number of databases they wish to create, followed by the names for each database.

### SQL Script Execution

After providing the database names for either mode:
1.  The user is asked: "Do you want to execute a SQL script on the created database(s)? (y/n)"
2.  If the user enters 'y' (case-insensitive), they are prompted to: "Enter the full path to the SQL script file:"
3.  The application will then attempt to read this script file.
    *   If the file cannot be read (e.g., not found, access denied), an error is logged, and script execution is skipped. Database creation will still proceed.
4.  If the script is read successfully:
    *   **Single Execution Mode:** The script is executed on each database immediately after it is successfully created.
    *   **Batch Execution Mode:** If the entire batch of database creations is successful, the script is then executed on each of the newly created databases.
5.  Errors during script execution on a specific database are logged but do not typically halt the overall process (e.g., script execution on other databases in a batch will still be attempted).

## Logging

The application uses **Serilog** for structured logging to provide detailed information about its operations.
*   **Console Output:** Key information, prompts, and summaries are displayed directly in the console. Error messages from exceptions caught during operations are also logged to the console by Serilog.
*   **File Logging:** Detailed logs, including timestamps, log levels, messages, and full exception details (if any), are written to log files.
    *   Log files are stored in the `logs/` directory (relative to the application's execution path, e.g., `DatabaseCreator/bin/Debug/net8.0/logs/`).
    *   A new log file is created each day with a name like `database_creator_log_YYYYMMDD.txt` (e.g., `database_creator_log_20231027.txt`).
    *   These persistent logs are invaluable for troubleshooting any issues that occur, especially for script execution problems or repository-level errors.

## Error Handling

The application incorporates error handling for common issues:
*   Invalid user input for menu selections.
*   Database creation errors (e.g., permission issues, invalid database names, connection problems). These are caught, logged, and the application attempts to continue where appropriate (especially in single execution mode).
*   SQL script file reading errors (e.g., file not found).
*   SQL script execution errors (e.g., syntax errors in the script).
*   Details of these errors are logged by Serilog to help diagnose problems. Custom `DatabaseOperationException` is used to wrap database-specific errors originating from the repository.

## Project Structure (Overview)

The solution is organized into the following projects:

*   `DatabaseCreator/`: The main .NET console application project containing `Program.cs` and `App.cs`.
*   `DatabaseCreator.Domain/`: Contains domain models, DTOs, interfaces for services and repositories, custom exceptions, and configuration objects.
*   `DatabaseCreator.Service/`: Contains the business logic services (e.g., `DatabaseOperationService`) that orchestrate operations.
*   `DatabaseCreator.Data/`: Contains data access logic, including repository implementations (e.g., `DatabaseOperationRepository`) that interact with the database.
*   `DatabaseCreator.Tests/`: Contains xUnit unit tests for the service layer.

## License

This project is licensed under the terms of the MIT license. Please see the `LICENSE.txt` file for more details.
