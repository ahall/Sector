using System;
using System.Collections.Generic;
using NDesk.Options;
using Nini.Config;
using Sector;
using System.IO;
using FluentNHibernate.Cfg.Db;

namespace Sector.Tool
{
    class MainClass
    {
        private List<string> extraArgs { get; set; }
        private OptionSet optionSet;
        private MigrateApi migrateApi;

        private string dbUser;
        private string dbPass;
        private string dbName;
        private string dbType;
        private string dbHostname;

        public MainClass()
        {
        }

        private void ShowHelp(OptionSet optionSet)
        {
            Console.Error.WriteLine("Sector.Tool.exe [OPTIONS] command");
            Console.Error.WriteLine("Supported commands:");
            Console.Error.WriteLine("\tmigrate_version_control:\tPut the database under version control");
            Console.Error.WriteLine("\tmigrate_version:\t\tPrints the latest version available in the repository");
            Console.Error.WriteLine("\tmigrate_db_version:\t\tPrints version the database is at");
            Console.Error.WriteLine("\tmigrate_upgrade <version>:\tUpgrade to the version given or latest if no version given");
            Console.Error.WriteLine("\tmigrate_downgrade [version]:\tDowngrade to the version given");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Supported database types: sqlite, postgresql, mysql");
            Console.Error.WriteLine("Sector is a database migration suite - https://github.com/ahall/Sector");
            optionSet.WriteOptionDescriptions(Console.Error);
            Environment.Exit(-1);
        }

        private string GetConnectionString()
        {
            if (string.IsNullOrEmpty(dbHostname) || string.IsNullOrEmpty(dbUser) ||
                string.IsNullOrEmpty(dbName) || string.IsNullOrEmpty(dbPass))
            {
                throw new SectorException("Missing dbhostname, username, dbname or dbpass");
            }

            return string.Format("Server={0};Database={1};User Id={2};Password={3}",
                        dbHostname, dbName, dbUser, dbPass);
        }

        private IPersistenceConfigurer GetConfigurator()
        {
            IPersistenceConfigurer ret = null;

            switch (dbType)
            {
                case "postgresql":
                {
                    string connString = GetConnectionString();
                    ret = PostgreSQLConfiguration.PostgreSQL82.ConnectionString(connString);
                    break;
                }
                case "mysql":
                {
                    string connString = GetConnectionString();
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

        public void Run(string[] args)
        {
            bool showHelp = false;
            string repoPath = string.Empty;
            int? version = null;

            optionSet = new OptionSet()
                .Add("?|help",
                     "Show this message and quit",
                     option => showHelp = option != null)
                .Add("repository-path=",
                     "Required: Full path to the repository path",
                     option => repoPath = option)
                .Add("dbuser=",
                     "Required: Username for the database",
                     option => dbUser = option)
                .Add("dbpass=",
                     "Required: Password for the database",
                     option => dbPass = option)
                .Add("dbname=",
                     "Required: Database name",
                     option => dbName = option)
                .Add("dbhost=",
                     "Required: Database hostname",
                     option => dbHostname = option)
                .Add("dbtype=",
                     "Required: Database type e.g. sqlite, postgresql, mysql",
                     option => dbType = option)
                .Add("version=",
                     "For upgrade/downgrade determines what version to go to",
                     option => version = int.Parse(option));

            try
            {
                extraArgs = optionSet.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("Sector: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `Sector --help' for more information.");
                return;
            }

            if (showHelp)
            {
                ShowHelp(optionSet);
                return;
            }

            if (extraArgs.Count < 1)
            {
                Console.WriteLine("Missing command");
                ShowHelp(optionSet);
                return;
            }

            if (string.IsNullOrEmpty(repoPath))
            {
                Console.WriteLine("Missing repository path");
                return;
            }

            // Now parse sector.cfg
            Repository repository = new Repository(repoPath);
            ISectorDb sectorDb = new SectorDb(GetConfigurator());

            migrateApi = new MigrateApi(sectorDb);

            string command = extraArgs[0];
            switch (command)
            {
                case "migrate_version_control":
                {
                    migrateApi.VersionControl(repository);
                    break;
                }
                case "migrate_db_version":
                {
                    int dbVersion = migrateApi.GetDbVersion(repository);
                    Console.WriteLine(dbVersion.ToString());
                    break;
                }
                case "migrate_version":
                {
                    int repoVersion = repository.GetVersion();
                    Console.WriteLine(repoVersion.ToString());
                    break;
                }
                case "migrate_upgrade":
                {
                    int upVersion = version.GetValueOrDefault(repository.GetVersion());
                    migrateApi.Upgrade(repository, upVersion);
                    break;
                }
                case "migrate_downgrade":
                {
                    if (!version.HasValue)
                    {
                        Console.WriteLine("Missing version for downgrade");
                        return;
                    }

                    migrateApi.Downgrade(repository, version.Value);
                    break;
                }
                default:
                {
                    Console.WriteLine("Invalid command");
                    return;
                }
            }
        }

        public static void Main(string[] args)
        {
            new MainClass().Run(args);
        }
    }
}
