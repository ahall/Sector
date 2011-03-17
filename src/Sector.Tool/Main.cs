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
            Console.Error.WriteLine("Sector is a database migration tool");
            optionSet.WriteOptionDescriptions(Console.Error);
            Environment.Exit(-1);
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
                return;
            }

            if (string.IsNullOrEmpty(repoPath))
            {
                Console.WriteLine("Missing repository path");
                return;
            }

            if (extraArgs.Count < 1)
            {
                Console.WriteLine("Missing command");
                return;
            }

            // Now parse sector.cfg
            Repository repository = new Repository(repoPath);
            ISectorDb sectorDb = new SectorDb(SQLiteConfiguration.Standard.UsingFile("/tmp/a.db"));

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
