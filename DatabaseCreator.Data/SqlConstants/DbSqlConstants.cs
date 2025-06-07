namespace DatabaseCreator.Data.SqlConstants
{
    public class DbSqlConstants // Changed to public
    {
        public const string CreateDatabaseQuery = "CREATE DATABASE {0}"; // Renamed and made public, added placeholder

        public const string DropDbQuery = "DROP DATABASE {0}"; // Made public, added placeholder

        public const string InsertDbInfo = @"INSERT INTO DbInfo (DbName ,IsCreated)
                                                           VALUES (@DbName, @IsCreated)"; // Made public
                                                             
    }
}
