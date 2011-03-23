using System;
using FluentNHibernate.Cfg.Db;

namespace Sector
{
    public static class DbUtils
    {
        public static IPersistenceConfigurer GetConfigurator(string dbType,
                                                             string dbHostname,
                                                             string dbUser,
                                                             string dbName,
                                                             string dbPass)
        {
            IPersistenceConfigurer ret = null;

            switch (dbType)
            {
                case "postgresql":
                {
                    string connString = GetConnectionString(dbHostname, dbUser, dbName, dbPass);
                    ret = PostgreSQLConfiguration.PostgreSQL82.ConnectionString(connString);
                    break;
                }
                case "mysql":
                {
                    string connString = GetConnectionString(dbHostname, dbUser, dbName, dbPass);
                    ret = MySQLConfiguration.Standard.ConnectionString(connString);
                    break;
                }
                case "sqlite":
                {
                    if (string.IsNullOrEmpty(dbName))
                    {
                        throw new SectorException("Dbname is required for sqlite");
                    }
                    ret = SQLiteConfiguration.Standard.UsingFile(dbName);
                    break;
                }
                default:
                {
                    throw new SectorException("Invalid database type given");
                }
            }

            return ret;
        }

        private static string GetConnectionString(string dbHostname, string dbUser,
                                                  string dbName, string dbPass)
        {
            if (string.IsNullOrEmpty(dbHostname) || string.IsNullOrEmpty(dbUser) ||
                string.IsNullOrEmpty(dbName) || string.IsNullOrEmpty(dbPass))
            {
                throw new SectorException("Missing dbhostname, username, dbname or dbpass");
            }

            return string.Format("Server={0};Database={1};User Id={2};Password={3}",
                        dbHostname, dbName, dbUser, dbPass);
        }
    }
}

