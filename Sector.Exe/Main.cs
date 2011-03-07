using System;
using System.Collections.Generic;
using NDesk.Options;
using Nini.Config;
using System.IO;

namespace Sector.Exe
{
    class MainClass
    {
        private List<string> extraArgs { get; set; }
        private const string DBURL = @"Server=localhost;Database=sector;User Id=ahall;Password=temp123";
        private OptionSet optionSet;
        private Sector.MigrateApi migrateApi;

        public MainClass()
        {
        }

        private void ShowHelp(OptionSet optionSet)
        {
            Console.Error.WriteLine("program [OPTIONS]");
            Console.Error.WriteLine("Sector is database migration tool");
            optionSet.WriteOptionDescriptions(Console.Error);
            Environment.Exit(-1);
        }

        public void Run(string[] args)
        {
            bool showHelp = false;
            string repoPath = string.Empty;

            optionSet = new OptionSet()
                .Add("?|help",
                     "Show this message and quit",
                     option => showHelp = option != null)
                .Add("repository-path=",
                     "Required: Full path to the repository path",
                     option => repoPath = option);

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
            migrateApi = new MigrateApi(DBURL);

            string command = extraArgs[0];
            if (command == "migrate_version_control")
            {
                // First make sure we're not already under version control.
                bool alreadyVersioned = migrateApi.IsVersionControlled(repository);
                if (alreadyVersioned)
                {
                    Console.WriteLine("Already Versioned, delete the tables and retry");
                    return;
                }

                migrateApi.VersionControl(repository);
            }
            else if (command == "migrate_db_version")
            {
                int dbVersion = migrateApi.GetDbVersion(repository);
                Console.WriteLine(dbVersion.ToString());
            }
        }

        public static void Main(string[] args)
        {
            new MainClass().Run(args);
        }
    }
}
