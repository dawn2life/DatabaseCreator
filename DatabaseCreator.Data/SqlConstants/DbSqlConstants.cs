namespace DatabaseCreator.Data.SqlConstants
{
    internal class DbSqlConstants
    {
        internal const string CreateDbQuery = "CREATE DATABASE ";

        internal const string DropDbQuery = "DROP DATABASE ";

        internal const string InsertDbInfo = @"INSERT INTO DbInfo (DbName ,IsCreated)
                                                           VALUES (@DbName, @IsCreated)";
                                                             
    }
}
