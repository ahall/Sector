using System;
using NUnit.Framework;

namespace Sector.Tests
{
    [TestFixture()]
    public class RepositoryTest
    {
        [Test()]
        public void GetVersion()
        {
            var repository = TestUtils.MakeRepository();
            Assert.AreEqual(2, repository.GetVersion());
        }

        [Test()]
        public void HasVersion()
        {
            var repository = TestUtils.MakeRepository();
            Assert.IsTrue(repository.HasVersion(1));
            Assert.IsTrue(repository.HasVersion(2));
            Assert.IsFalse(repository.HasVersion(3));
            Assert.IsFalse(repository.HasVersion(0));
        }

        [Test()]
        [ExpectedException(typeof(SectorException))]
        public void GetUpgradeSql_Invalid()
        {
            var repository = TestUtils.MakeRepository();
            repository.GetUpgradeSql(232323);
        }

        [Test()]
        public void GetUpgradeSql()
        {
            var repository = TestUtils.MakeRepository();

            string version1 = @"CREATE TABLE testie(
    id serial PRIMARY KEY NOT NULL,
    age integer NOT NULL UNIQUE,
    description varchar(255)
);
";
            Assert.AreEqual(version1, repository.GetUpgradeSql(1));

        string version2 = @"CREATE TABLE moon(
    id serial PRIMARY KEY NOT NULL,
    age2 integer NOT NULL UNIQUE,
    description2 varchar(255)
);
";
            Assert.AreEqual(version2, repository.GetUpgradeSql(2));
        }

        [Test()]
        [ExpectedException(typeof(SectorException))]
        public void GetDowngradeSql_Invalid()
        {
            var repository = TestUtils.MakeRepository();
            repository.GetDowngradeSql(232323);
        }

        [Test()]
        public void GetDowngradeSql()
        {
            var repository = TestUtils.MakeRepository();

            string version1 = @"DROP TABLE testie;" + Environment.NewLine;
            Assert.AreEqual(version1, repository.GetDowngradeSql(1));

            string version2 = @"DROP TABLE moon;" + Environment.NewLine;
            Assert.AreEqual(version2, repository.GetDowngradeSql(2));
        }
    }
}
