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
        private const string DbServer = "localhost";
        private const string DbUsername = "ahall";
        private const string DbPassword = "temp123";
        private const string Database = "sector_test";

        private static readonly string ConnectionString = string.Format(CONNSTRING_TEMPLATE, DbServer,
                                                                        Database, DbUsername, DbPassword);
        private static readonly string TestFiles = GetTestFilesDir();

        private const string CONNSTRING_TEMPLATE =
                           @"Server={0};Database={1};User Id={2};Password={3}";


        private static string GetTestFilesDir()
        {
            string solnDir = new DirectoryInfo(Environment.CurrentDirectory)
                                        .Parent.Parent.FullName;
            return Path.Combine(solnDir, "testfiles");
        }

        private static IPersistenceConfigurer BuildDatabase()
        {
            return PostgreSQLConfiguration.PostgreSQL82.ConnectionString(ConnectionString);
        }

        private static void BuildSchema(NHibernate.Cfg.Configuration cfg)
        {
            new SchemaExport(cfg).Create(script: false, export: true);
        }

        public static ISessionFactory MakeFactory(bool export = false)
        {
            FluentConfiguration dbCfg = Fluently.Configure().Database(BuildDatabase());
            dbCfg.Mappings(x => x.FluentMappings.Add<MigrateVersionMap>());

            if (export)
                dbCfg.ExposeConfiguration(BuildSchema);
            return dbCfg.BuildSessionFactory();
        }

        public static Repository MakeRepository()
        {
            return new Repository(Path.Combine(TestFiles, "repo"));
        }

        public static ISectorDb MakeSectorDb()
        {
            return new SectorDb(server: DbServer, username: DbUsername,
                                password: DbPassword, database: Database);
        }
    }
}

