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

        private static readonly string ConnectionString = string.Format(CONNSTRING_TEMPLATE, DbServer,
                                                                        Database, DbUsername, DbPassword);
        private static readonly string TestFiles = GetTestFilesDir();
        private ISessionFactory dbFactory;

        private static IPersistenceConfigurer BuildDatabase()
        {
            return PostgreSQLConfiguration.PostgreSQL82.ConnectionString(ConnectionString);
        }

        private static void BuildSchema(NHibernate.Cfg.Configuration cfg)
        {
            new SchemaExport(cfg).Create(script: false, export: true);
        }

        private static ISessionFactory MakeFactory(bool export = false)
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

            dbFactory = MakeFactory(export: false);
        }

        private static string GetTestFilesDir()
        {
            string solnDir = new DirectoryInfo(Environment.CurrentDirectory)
                                        .Parent.Parent.FullName;
            return Path.Combine(solnDir, "testfiles");
        }

        private static Repository MakeRepository()
        {
            return new Repository(Path.Combine(TestFiles, "repo"));
        }

        private ISectorDb MakeSectorDb()
        {
            return new SectorDb(server: DbServer, username: DbUsername,
                                password: DbPassword, database: Database);
        }

        [Test()]
        public void IsVersionControlled()
        {
            var sectorDb = MakeSectorDb();
            var repository = MakeRepository();

            MigrateApi migrateApi = new MigrateApi(sectorDb);
            bool success = migrateApi.IsVersionControlled(repository);
            Assert.IsFalse(success);

            // Now create migrate version with version 0, then it shall return true.
            using (ISession session = dbFactory.OpenSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                var migVer = new MigrateVersion(repository.RepositoryId, repository.RepositoryPath, 0);
                session.Save(migVer);
                transaction.Commit();
            }

            success = migrateApi.IsVersionControlled(repository);
            Assert.IsTrue(success);
        }

        [Test()]
        public void VersionControl()
        {
            var sectorDb = MakeSectorDb();
            var repository = MakeRepository();

            MigrateApi migrateApi = new MigrateApi(sectorDb);
            Assert.IsFalse(migrateApi.IsVersionControlled(repository));

            // No matter how often we do this, version control will always
            // set the database to version 0.
            for (int i = 0; i < 3; ++i)
            {
                migrateApi.VersionControl(repository);
                Assert.IsTrue(migrateApi.IsVersionControlled(repository));
                Assert.AreEqual(0, migrateApi.GetDbVersion(repository));

                using (ISession session = dbFactory.OpenSession())
                {
                    MigrateVersion migVer = session.QueryOver<MigrateVersion>()
                        .Where(m => m.RepositoryId == repository.RepositoryId)
                        .SingleOrDefault();

                    Assert.IsNotNull(migVer);
                    Assert.AreEqual(repository.RepositoryId, migVer.RepositoryId);
                    Assert.AreEqual(repository.RepositoryPath, migVer.RepositoryPath);
                    Assert.AreEqual(0, migVer.Version);
                }
            }

        }

        [Test()]
        [ExpectedException(typeof(Exception))]
        public void GetDbVersion_EmptyDb()
        {
            var sectorDb = MakeSectorDb();
            var repository = MakeRepository();

            MigrateApi migrateApi = new MigrateApi(sectorDb);
            migrateApi.GetDbVersion(repository);
        }

        [Test()]
        public void GetDbVersion()
        {
            var sectorDb = MakeSectorDb();
            var repository = MakeRepository();

            MigrateApi migrateApi = new MigrateApi(sectorDb);

            // Now create migrate version with version 0, then it shall return true.
            using (ISession session = dbFactory.OpenSession())
            {
                int migId = 0;
                using (ITransaction transaction = session.BeginTransaction())
                {
                    var migVer = new MigrateVersion(repository.RepositoryId, repository.RepositoryPath, 0);
                    session.Save(migVer);
                    transaction.Commit();
                    migId = migVer.Id;
                }

                Assert.AreEqual(0, migrateApi.GetDbVersion(repository));

                using (ITransaction transaction = session.BeginTransaction())
                {
                    var migVer = session.Get<MigrateVersion>(migId);
                    migVer.Version = 10;
                    session.Update(migVer);
                    transaction.Commit();
                }

                Assert.AreEqual(10, migrateApi.GetDbVersion(repository));
            }
        }


    }
}

