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
using NHibernate.Exceptions;

namespace Sector.Tests
{
    [TestFixture()]
    public class MigrateApiTest
    {
        private ISessionFactory dbFactory;

        [SetUp]
        public void Setup()
        {
            // Clean out schema to start fresh.
            TestUtils.MakeFactory(export: true).Dispose();

            dbFactory = TestUtils.MakeFactory(export: false);
        }

        [Test()]
        public void IsVersionControlled()
        {
            var sectorDb = TestUtils.MakeSectorDb();
            var repository = TestUtils.MakeRepository();

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
            var sectorDb = TestUtils.MakeSectorDb();
            var repository = TestUtils.MakeRepository();

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
        [ExpectedException(typeof(SectorException))]
        public void GetDbVersion_EmptyDb()
        {
            var sectorDb = TestUtils.MakeSectorDb();
            var repository = TestUtils.MakeRepository();

            MigrateApi migrateApi = new MigrateApi(sectorDb);
            migrateApi.GetDbVersion(repository);
        }

        [Test()]
        public void GetDbVersion()
        {
            var sectorDb = TestUtils.MakeSectorDb();
            var repository = TestUtils.MakeRepository();

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

        [Test()]
        public void Upgrade_Incremental()
        {
            var sectorDb = TestUtils.MakeSectorDb();
            var repository = TestUtils.MakeRepository();

            MigrateApi migrateApi = new MigrateApi(sectorDb);
            migrateApi.VersionControl(repository);

            using (ISession session = dbFactory.OpenSession())
            {
                Assert.AreEqual(0, migrateApi.GetDbVersion(repository));
                Assert.Throws<GenericADOException>(delegate {
                    session.CreateSQLQuery("SELECT * FROM testie;").ExecuteUpdate();
                });

                migrateApi.Upgrade(repository, 1);
                session.CreateSQLQuery("SELECT * FROM testie;").ExecuteUpdate();
                Assert.Throws<GenericADOException>(delegate {
                    session.CreateSQLQuery("SELECT * FROM moon;").ExecuteUpdate();
                });

                Assert.AreEqual(1, migrateApi.GetDbVersion(repository));
                session.CreateSQLQuery("SELECT * FROM testie;").ExecuteUpdate();

                migrateApi.Upgrade(repository, 2);
                Assert.AreEqual(2, migrateApi.GetDbVersion(repository));
                session.CreateSQLQuery("SELECT * FROM testie;").ExecuteUpdate();
                session.CreateSQLQuery("SELECT * FROM moon;").ExecuteUpdate();

                using (ITransaction transaction = session.BeginTransaction())
                {
                    session.CreateSQLQuery("DROP TABLE testie CASCADE;").ExecuteUpdate();
                    session.CreateSQLQuery("DROP TABLE moon CASCADE;").ExecuteUpdate();
                    transaction.Commit();
                }
            }
        }

        [Test()]
        public void Upgrade_Straight()
        {
            var sectorDb = TestUtils.MakeSectorDb();
            var repository = TestUtils.MakeRepository();

            MigrateApi migrateApi = new MigrateApi(sectorDb);
            migrateApi.VersionControl(repository);

            using (ISession session = dbFactory.OpenSession())
            {
                Assert.AreEqual(0, migrateApi.GetDbVersion(repository));
                Assert.Throws<GenericADOException>(delegate {
                    session.CreateSQLQuery("SELECT * FROM testie;").ExecuteUpdate();
                });

                migrateApi.Upgrade(repository, 2);
                session.CreateSQLQuery("SELECT * FROM testie;").ExecuteUpdate();
                session.CreateSQLQuery("SELECT * FROM moon;").ExecuteUpdate();
                Assert.AreEqual(2, migrateApi.GetDbVersion(repository));

                using (ITransaction transaction = session.BeginTransaction())
                {
                    session.CreateSQLQuery("DROP TABLE testie CASCADE;").ExecuteUpdate();
                    session.CreateSQLQuery("DROP TABLE moon CASCADE;").ExecuteUpdate();
                    transaction.Commit();
                }
            }
        }

        [Test()]
        public void Downgrade_Straight()
        {
            var sectorDb = TestUtils.MakeSectorDb();
            var repository = TestUtils.MakeRepository();

            MigrateApi migrateApi = new MigrateApi(sectorDb);
            migrateApi.VersionControl(repository);

            using (ISession session = dbFactory.OpenSession())
            {
                migrateApi.Upgrade(repository, 2);
                session.CreateSQLQuery("SELECT * FROM testie;").ExecuteUpdate();
                session.CreateSQLQuery("SELECT * FROM moon;").ExecuteUpdate();
                Assert.AreEqual(2, migrateApi.GetDbVersion(repository));

                migrateApi.Downgrade(repository, 0);
                Assert.AreEqual(0, migrateApi.GetDbVersion(repository));
                Assert.Throws<GenericADOException>(delegate {
                    session.CreateSQLQuery("SELECT * FROM testie;").ExecuteUpdate();
                    session.CreateSQLQuery("SELECT * FROM moon;").ExecuteUpdate();
                });
            }
        }

        [Test()]
        public void Downgrade_Incremental()
        {
            var sectorDb = TestUtils.MakeSectorDb();
            var repository = TestUtils.MakeRepository();

            MigrateApi migrateApi = new MigrateApi(sectorDb);
            migrateApi.VersionControl(repository);

            using (ISession session = dbFactory.OpenSession())
            {
                migrateApi.Upgrade(repository, 2);
                session.CreateSQLQuery("SELECT * FROM testie;").ExecuteUpdate();
                session.CreateSQLQuery("SELECT * FROM moon;").ExecuteUpdate();
                Assert.AreEqual(2, migrateApi.GetDbVersion(repository));

                migrateApi.Downgrade(repository, 1);
                Assert.AreEqual(1, migrateApi.GetDbVersion(repository));
                session.CreateSQLQuery("SELECT * FROM testie;").ExecuteUpdate();
                Assert.Throws<GenericADOException>(delegate {
                    session.CreateSQLQuery("SELECT * FROM moon;").ExecuteUpdate();
                });

                migrateApi.Downgrade(repository, 0);
                Assert.AreEqual(0, migrateApi.GetDbVersion(repository));
                Assert.Throws<GenericADOException>(delegate {
                    session.CreateSQLQuery("SELECT * FROM testie;").ExecuteUpdate();
                    session.CreateSQLQuery("SELECT * FROM moon;").ExecuteUpdate();
                });
            }
        }
    }
}

