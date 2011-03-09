using System;
using NUnit.Framework;
using NHibernate;
using Sector.Entities;
using Sector;
using FluentNHibernate.Cfg.Db;
using NHibernate.Tool.hbm2ddl;
using FluentNHibernate.Cfg;
using Sector.Mappings;
using System.IO;

namespace Sector.Tests
{
    [TestFixture()]
    public class MigrateApiTest
    {
        private const string DbServer = "localhost";
        private const string DbUsername = "ahall";
        private const string DbPassword = "temp123";
        private const string Database = "sector_test";

        private const string CONNSTRING_TEMPLATE =
                           @"Server={0};Database={1};User Id={2};Password={3}";

        private readonly string ConnectionString = string.Format(CONNSTRING_TEMPLATE, DbServer,
                                                                 Database, DbUsername, DbPassword);
        private readonly string TestFiles = GetTestFilesDir();

        private IPersistenceConfigurer BuildDatabase()
        {
            return PostgreSQLConfiguration.PostgreSQL82.ConnectionString(ConnectionString);
        }

        private void BuildSchema(NHibernate.Cfg.Configuration cfg)
        {
            new SchemaExport(cfg).Create(script: false, export: true);
        }

        private ISessionFactory MakeFactory(bool export = false)
        {
            FluentConfiguration dbCfg = Fluently.Configure().Database(BuildDatabase());
            dbCfg.Mappings(x => x.FluentMappings.Add<MigrateVersionMap>());

            if (export)
                dbCfg.ExposeConfiguration(BuildSchema);
            return dbCfg.BuildSessionFactory();
        }

        [SetUp]
        public void Setup()
        {
            // Clean out schema to start fresh.
            MakeFactory(export: true).Dispose();
        }

        private static string GetTestFilesDir()
        {
            string solnDir = new DirectoryInfo(Environment.CurrentDirectory)
                                        .Parent.Parent.FullName;
            return Path.Combine(solnDir, "testfiles");
        }

        private ISectorDb MakeSectorDb()
        {
            return new SectorDb(server: DbServer, username: DbUsername,
                                password: DbPassword, database: Database);
        }

        [Test()]
        public void IsVersionControlled_False()
        {
            var sectorDb = MakeSectorDb();
            var repository = new Repository(Path.Combine(TestFiles, "repo"));

            MigrateApi migrateApi = new MigrateApi(sectorDb);
            bool success = migrateApi.IsVersionControlled(repository);
            Assert.IsFalse(success);
        }

    }
}

