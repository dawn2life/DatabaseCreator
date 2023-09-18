using DatabaseCreator.Data.Infrastructure.Connection;
using DatabaseCreator.Data.SqlConstants;
using DatabaseCreator.Domain.Configurations;
using DatabaseCreator.Domain.Models;
using DatabaseCreator.Domain.Repositories;
using Microsoft.Extensions.Options;
using System.Data;

namespace DatabaseCreator.Data.Repositories
{
    public class DatabaseOperationRepository : IDatabsaeOperationRepository
    {
        private readonly IConnection _conn;
        private readonly ConnectionStrings _connStrings;

        public DatabaseOperationRepository(IOptions<ConnectionStrings> connStrings, IConnection conn)
        {
            _conn = conn;
            _connStrings = connStrings.Value;
        }

        public bool CreateDbWithSingleExecution(string dbName)
        {
            using var connection = _conn.GetSqlConnection(_connStrings.SqlDb.ConnectionString);
            using (IDbCommand command = connection.CreateCommand())
            {
                try
                {
                    command.CommandText = DbSqlConstants.CreateDbQuery + dbName;
                    command.ExecuteNonQuery();
                    Console.WriteLine($"Database '{dbName}' created successfully.");
                }
                catch (Exception ex) 
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }

            return true;    
        }

        public bool CreateDbWithBatch(List<string> dbNames)
        {
            int counter = 0;
            using var connection = _conn.GetSqlConnection(_connStrings.SqlDb.ConnectionString);
            using (IDbCommand command = connection.CreateCommand())
            {
                try
                {
                    foreach (string dbName in dbNames)
                    {
                        command.CommandText = DbSqlConstants.CreateDbQuery + dbName;
                        command.ExecuteNonQuery();
                        Console.WriteLine($"Database '{dbName}' created successfully.");
                        counter++;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating databases: {ex.Message}");
                    
                    if (counter == 0) return false;

                    for (int i = counter; i > 0; i--)
                    {
                        command.CommandText = DbSqlConstants.DropDbQuery + dbNames[i - 1];
                        command.ExecuteNonQuery();
                        Console.WriteLine($"Database '{dbNames[i - 1]}' rolled back!");
                    }
                    return false;
                }
            }
        }

        public void AddCreatedDb(List<DbInfo> dbInfos) 
        {
            using var connection = _conn.GetSqlConnection(_connStrings.SqlDb.ConnectionString);
            using (IDbCommand command = connection.CreateCommand()) 
            {

                Console.WriteLine("\nHistory for db operations: ");
                try
                {
                    command.CommandText = DbSqlConstants.InsertDbInfo;
                    foreach (var dbInfo in dbInfos) 
                    {
                        IDataParameter dbNameParam = command.CreateParameter();
                        dbNameParam.ParameterName = "@DbName";
                        dbNameParam.Value = dbInfo.DbName;

                        IDataParameter isCreatedParam = command.CreateParameter();
                        isCreatedParam.ParameterName = "@IsCreated";
                        isCreatedParam.Value = dbInfo.IsCreated;

                        command.Parameters.Clear();
                        command.Parameters.Add(dbNameParam);
                        command.Parameters.Add(isCreatedParam);

                        var rowEffected = command.ExecuteNonQuery();
                        if (rowEffected > 0) 
                        {
                            Console.WriteLine($"Inserted record for {dbInfo.DbName}");
                        }
                    }
                    return;
                }
                catch (Exception ex) 
                {
                    Console.WriteLine($"Error inserting records: {ex.Message}");
                    return;
                }
            }
        }
    }
}
