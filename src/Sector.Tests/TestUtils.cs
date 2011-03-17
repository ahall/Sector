using System;
using NHibernate.Tool.hbm2ddl;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Cfg;
using Sector.Mappings;
using NHibernate;
using System.IO;

namespace Sector.Tests
{
    public static class TestUtils
    {
        private static readonly string TestFiles = GetTestFilesDir();
        private static readonly string TestArea = GetTestAreaDir();

        private static string GetTestFilesDir()
        {
            string solnDir = new DirectoryInfo(Environment.CurrentDirectory)
                                        .Parent.Parent.FullName;
            return Path.Combine(solnDir, "testfiles");
        }

        private static string GetTestAreaDir()
        {
            string solnDir = new DirectoryInfo(Environment.CurrentDirectory)
                                        .Parent.Parent.FullName;
            return Path.Combine(solnDir, "testarea");
        }

        private static IPersistenceConfigurer GetDbConfigurer()
        {
            return SQLiteConfiguration.Standard.UsingFile(GetDbPath());
        }

        public static string GetDbPath()
        {
            return Path.Combine(TestArea, "sector.db");
        }

        public static void CreateMigrationTable()
        {
            Fluently.Configure()
                    .Database(GetDbConfigurer())
                    .Mappings(x => x.FluentMappings.Add<MigrateVersionMap>())
                    .ExposeConfiguration(cfg => new SchemaExport(cfg).Create(script: false, export: true))
                    .BuildSessionFactory()
                    .Dispose();
        }

        public static ISessionFactory MakeFactory()
        {
            return Fluently.Configure()
                  .Database(GetDbConfigurer())
                  .Mappings(x => x.FluentMappings.Add<MigrateVersionMap>())
                  .BuildSessionFactory();
        }

        public static Repository MakeRepository()
        {
            return new Repository(Path.Combine(TestFiles, "repo"));
        }

        public static ISectorDb MakeSectorDb()
        {
            return new SectorDb(GetDbConfigurer());
        }
    }
}

