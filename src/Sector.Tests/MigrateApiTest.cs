using System;
using NUnit.Framework;
using Sector;
using System.IO;
using Mono.Data.Sqlite;

namespace Sector.Tests
{
    [TestFixture()]
    public class MigrateApiTest
    {
        [SetUp]
        public void Setup()
        {
            if (File.Exists(TestUtils.GetDbPath()))
            {
                File.Delete(TestUtils.GetDbPath());
            }
        }

        [Test()]
        public void IsVersionControlled()
        {
            var sectorDb = TestUtils.MakeSectorDb();
            var repository = TestUtils.MakeRepository();

            // Need to start with the schema created.
            TestUtils.CreateMigrationTable();

            var migrateApi = new MigrateApi(sectorDb);
            bool success = migrateApi.IsVersionControlled(repository);
            Assert.IsFalse(success);

            using (var dbConn = TestUtils.OpenDbconnection())
            using (var dbCommand = dbConn.CreateCommand())
            {
                string templ = "INSERT INTO migrate_version (repository_id, repository_path, version) VALUES('{0}', '{1}', '{2}')";
                dbCommand.CommandText = string.Format(templ, repository.RepositoryId, repository.RepositoryPath, 0);
                dbCommand.ExecuteNonQuery();
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

            migrateApi.VersionControl(repository);
            Assert.IsTrue(migrateApi.IsVersionControlled(repository));
            Assert.AreEqual(0, migrateApi.GetDbVersion(repository));

            using (var dbConn = TestUtils.OpenDbconnection())
            using (var dbCommand = dbConn.CreateCommand())
            {
                string templ = "SELECT * FROM {0} WHERE repository_id = '{1}'";
                dbCommand.CommandText = string.Format(templ, SectorDb.TableName, repository.RepositoryId);
                var reader = dbCommand.ExecuteReader();

                Assert.AreEqual(repository.RepositoryId, reader["repository_id"]);
                Assert.AreEqual(repository.RepositoryPath, reader["repository_path"]);
                Assert.AreEqual(0, reader["version"]);
            }

            // Trying again results in SectorException.
            Assert.Throws<SectorException>(delegate {
                migrateApi.VersionControl(repository);
            });
        }

        [Test()]
        public void GetDbVersion_EmptyDb()
        {
            var sectorDb = TestUtils.MakeSectorDb();
            var repository = TestUtils.MakeRepository();

            Assert.Throws<SqliteException>(delegate {
                MigrateApi migrateApi = new MigrateApi(sectorDb);
                migrateApi.GetDbVersion(repository);
            });
        }

        [Test()]
        public void GetDbVersion()
        {
            var sectorDb = TestUtils.MakeSectorDb();
            var repository = TestUtils.MakeRepository();

            MigrateApi migrateApi = new MigrateApi(sectorDb);

            // Need to start with the schema created.
            TestUtils.CreateMigrationTable();


            using (var dbConn = TestUtils.OpenDbconnection())
            {
                using (var dbCommand = dbConn.CreateCommand())
                {
                    string templ = "INSERT INTO migrate_version (repository_id, repository_path, version) VALUES('{0}', '{1}', '{2}')";
                    dbCommand.CommandText = string.Format(templ, repository.RepositoryId, repository.RepositoryPath, 0);
                    dbCommand.ExecuteNonQuery();

                    // Verify correct behaviour.
                    Assert.AreEqual(0, migrateApi.GetDbVersion(repository));
                }

                using (var dbCommand = dbConn.CreateCommand())
                {
                    string templ = "UPDATE migrate_version SET version = 10 WHERE repository_id = '{0}'";
                    dbCommand.CommandText = string.Format(templ, repository.RepositoryId);
                    dbCommand.ExecuteNonQuery();

                    // Verify correct behaviour.
                    Assert.AreEqual(10, migrateApi.GetDbVersion(repository));
                }
            }
        }

        [Test()]
        public void Upgrade_Incremental()
        {
            var sectorDb = TestUtils.MakeSectorDb();
            var repository = TestUtils.MakeRepository();

            MigrateApi migrateApi = new MigrateApi(sectorDb);
            migrateApi.VersionControl(repository);

            using (var dbConn = TestUtils.OpenDbconnection())
            {
                Assert.AreEqual(0, migrateApi.GetDbVersion(repository));

                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM testie";

                    Assert.Throws<SqliteException>(delegate {
                        dbCommand.ExecuteNonQuery();
                    });
                }

                // Upgrading to 1, will add testie.
                migrateApi.Upgrade(repository, 1);
                Assert.AreEqual(1, migrateApi.GetDbVersion(repository));

                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM testie";
                    dbCommand.ExecuteNonQuery();
                }

                // moon comes in in version 2, so shouldnt be there now.
                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM moon";

                    Assert.Throws<SqliteException>(delegate {
                        dbCommand.ExecuteNonQuery();
                    });
                }

                migrateApi.Upgrade(repository, 2);
                Assert.AreEqual(2, migrateApi.GetDbVersion(repository));

                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM testie";
                    dbCommand.ExecuteNonQuery();
                }

                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM moon";
                    dbCommand.ExecuteNonQuery();
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

            using (var dbConn = TestUtils.OpenDbconnection())
            {
                Assert.AreEqual(0, migrateApi.GetDbVersion(repository));
                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM testie";

                    Assert.Throws<SqliteException>(delegate {
                        dbCommand.ExecuteNonQuery();
                    });
                }

                migrateApi.Upgrade(repository, 2);
                Assert.AreEqual(2, migrateApi.GetDbVersion(repository));

                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM testie";
                    dbCommand.ExecuteNonQuery();
                }

                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM moon";
                    dbCommand.ExecuteNonQuery();
                }
            }
        }

        [Test()]
        public void Upgrade_Straight_NoVersionGiven()
        {
            var sectorDb = TestUtils.MakeSectorDb();
            var repository = TestUtils.MakeRepository();

            MigrateApi migrateApi = new MigrateApi(sectorDb);
            migrateApi.VersionControl(repository);

            using (var dbConn = TestUtils.OpenDbconnection())
            {
                Assert.AreEqual(0, migrateApi.GetDbVersion(repository));
                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM testie";

                    Assert.Throws<SqliteException>(delegate {
                        dbCommand.ExecuteNonQuery();
                    });
                }

                migrateApi.Upgrade(repository);
                Assert.AreEqual(2, migrateApi.GetDbVersion(repository));

                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM testie";
                    dbCommand.ExecuteNonQuery();
                }

                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM moon";
                    dbCommand.ExecuteNonQuery();
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

            using (var dbConn = TestUtils.OpenDbconnection())
            {
                migrateApi.Upgrade(repository, 2);
                Assert.AreEqual(2, migrateApi.GetDbVersion(repository));

                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM testie";
                    dbCommand.ExecuteNonQuery();
                }

                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM moon";
                    dbCommand.ExecuteNonQuery();
                }

                migrateApi.Downgrade(repository, 0);
                Assert.AreEqual(0, migrateApi.GetDbVersion(repository));

                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM testie";

                    Assert.Throws<SqliteException>(delegate {
                        dbCommand.ExecuteNonQuery();
                    });
                }

                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM moon";

                    Assert.Throws<SqliteException>(delegate {
                        dbCommand.ExecuteNonQuery();
                    });
                }
            }
        }

        [Test()]
        public void Downgrade_Incremental()
        {
            var sectorDb = TestUtils.MakeSectorDb();
            var repository = TestUtils.MakeRepository();

            MigrateApi migrateApi = new MigrateApi(sectorDb);
            migrateApi.VersionControl(repository);

            using (var dbConn = TestUtils.OpenDbconnection())
            {
                migrateApi.Upgrade(repository, 2);
                Assert.AreEqual(2, migrateApi.GetDbVersion(repository));

                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM testie";
                    dbCommand.ExecuteNonQuery();
                }

                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM moon";
                    dbCommand.ExecuteNonQuery();
                }

                migrateApi.Downgrade(repository, 1);
                Assert.AreEqual(1, migrateApi.GetDbVersion(repository));

                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM testie";
                    dbCommand.ExecuteNonQuery();
                }

                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM moon";

                    Assert.Throws<SqliteException>(delegate {
                        dbCommand.ExecuteNonQuery();
                    });
                }

                migrateApi.Downgrade(repository, 0);
                Assert.AreEqual(0, migrateApi.GetDbVersion(repository));

                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM testie";

                    Assert.Throws<SqliteException>(delegate {
                        dbCommand.ExecuteNonQuery();
                    });
                }

                using (var dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT * FROM moon";

                    Assert.Throws<SqliteException>(delegate {
                        dbCommand.ExecuteNonQuery();
                    });
                }
            }
        }
    }
}

