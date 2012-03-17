using System;
using System.Data;

namespace Sector
{
    public static class DbUtils
    {
        public static string GetConnectionString(string dbHostname, string dbUser,
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

