using System;
using System.Data;
using System.Data.Common;

namespace Sector
{
    public enum DatabaseType
    {
        PostgreSql = 0,
        Sqlite
    }

    public class SectorDb : ISectorDb
    {
        public IDbConnection Connection { get; set; }

        public static string TableName = "migrate_version";

        public SectorDb(string dbType, string connectionString)
        {
            string providerName = "Mono.Data.Sqlite";

            if (dbType == "postgresql")
            {
                providerName = "Npgsql";
            }

            var factory = DbProviderFactories.GetFactory(providerName);
            Connection = factory.CreateConnection();
            Connection.ConnectionString = connectionString;
        }

        public SectorDb(IDbConnection connection)
        {
            Connection = connection;
        }

        public static SectorDb FromDbInfo(string dbType, string dbHostname,
                                          string dbUser, string dbName, string dbPass)
        {
            string connectionString = DbUtils.GetConnectionString(dbHostname, dbUser, dbName, dbPass);
            return new Sector.SectorDb(dbType, connectionString);
        }

        public void CreateMigrationTable()
        {
            using (var cmd = Connection.CreateCommand())
            {
                const string sql = "CREATE TABLE {0}("
                                 + "repository_id VARCHAR(255) PRIMARY KEY NOT NULL,"
                                 + "repository_path VARCHAR(255),"
                                 + "version integer)";
                cmd.CommandText = string.Format(sql, TableName);
                cmd.ExecuteNonQuery();
            }
        }

        public void Connect()
        {
            Connection.Open();
        }

        public void Dispose()
        {
            Connection.Close();
        }
    }
}

