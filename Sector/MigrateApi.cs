using System;
using System.Data;
using System.Linq;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Cfg;
using NHibernate;
using System.Collections.Generic;
using Sector.Entities;
using System.IO;
using Sector.Mappings;
using NHibernate.Tool.hbm2ddl;

namespace Sector
{
    public class MigrateApi : IMigrateApi
    {
        private ISectorDb sectorDb;

        public MigrateApi(ISectorDb sectorDb)
        {
            this.sectorDb = sectorDb;
        }

        public bool IsVersionControlled(IRepository repository)
        {
            // First make sure we're not already under version control.
            bool alreadyVersioned = true;
            using (ISession session = sectorDb.DbFactory.OpenSession())
            {
                try
                {
                    MigrateVersion mgv = session.QueryOver<MigrateVersion>()
                        .Where(m => m.RepositoryId == repository.RepositoryId)
                        .SingleOrDefault();
                    if (mgv == null)
                        alreadyVersioned = false;
                }
                catch (Exception)
                {
                    alreadyVersioned = false;
                }
            }

            return alreadyVersioned;
        }

        public void VersionControl(IRepository repository)
        {
            // Create the migration table first.
            sectorDb.CreateMigrationTable();

            Console.WriteLine("Versioning the db, setting version to 0");

            using (ISession session = sectorDb.DbFactory.OpenSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                MigrateVersion mgv = new MigrateVersion(repositoryId: repository.RepositoryId,
                                                        repositoryPath: repository.RepositoryPath,
                                                        version: 0);
                session.Save(mgv);
                transaction.Commit();
            }
        }

        private int GetDbVersion(IRepository repository, ISession session)
        {
            MigrateVersion mgv = session.QueryOver<MigrateVersion>()
                .Where(m => m.RepositoryId == repository.RepositoryId)
                .SingleOrDefault();
            if (mgv == null)
                throw new SectorException("Unable to fetch the db version");

            return mgv.Version;
        }

        public int GetDbVersion(IRepository repository)
        {
            using (ISession session = sectorDb.DbFactory.OpenSession())
            {
                return GetDbVersion(repository, session);
            }
        }

        public void Upgrade(IRepository repository, int version)
        {
            using (ISession session = sectorDb.DbFactory.OpenSession())
            {
                int dbVersion = GetDbVersion(repository, session);
                if (dbVersion >= version)
                {
                    // Already up higher than this so do nothing.
                    return;
                }

                int highestAvailable = repository.GetVersion();
                if (version > highestAvailable)
                {
                    throw new SectorException("Version requested higher than latest available");
                }

                if (!repository.HasVersion(version))
                {
                    throw new SectorException("Version requested not available in the repository");
                }

                int steps = version - dbVersion;
                foreach (var upVersion in Enumerable.Range(dbVersion + 1, steps))
                {
                    using (ITransaction transaction = session.BeginTransaction())
                    using (var sqlCommand = session.Connection.CreateCommand())
                    {
                        // Run the SQL for the next version.
                        sqlCommand.CommandText = repository.GetUpgradeSql(upVersion);
                        sqlCommand.ExecuteNonQuery();

                        // Upgrade the version info and then commit the transaction.
                        MigrateVersion mgv = session.QueryOver<MigrateVersion>()
                            .Where(m => m.RepositoryId == repository.RepositoryId)
                            .SingleOrDefault();
                        mgv.Version = upVersion;

                        transaction.Commit();
                    }
                }
            }
        }

        public void Downgrade(IRepository repository, int version)
        {
            using (ISession session = sectorDb.DbFactory.OpenSession())
            {
                int dbVersion = GetDbVersion(repository, session);
                if (dbVersion <= version)
                {
                    // Already lower or same as current version.
                    return;
                }

                if (version < 0)
                {
                    throw new SectorException("Version cannot be less than 0");
                }

                // Example downgrade from 4 back to 0:
                // 4_downgrade set version to 3
                // 3_downgrade set version to 2,
                // 2_downgrade set version to 1
                // 1_downgrade set version = 0
                // Means we need to go over range 1, 4 in reverse order, here we iterate
                // over range 1, 4 reversed to { 4, 1 } == 4, 3, 2, 1 and end with dbVersion 0
                int steps = dbVersion - version;
                foreach (var downVersion in Enumerable.Range(version + 1, steps).Reverse())
                {
                    using (ITransaction transaction = session.BeginTransaction())
                    using (var sqlCommand = session.Connection.CreateCommand())
                    {
                        // Run the SQL for the next version.
                        sqlCommand.CommandText = repository.GetDowngradeSql(downVersion);
                        sqlCommand.ExecuteNonQuery();

                        // Upgrade the version info and then commit the transaction.
                        MigrateVersion mgv = session.QueryOver<MigrateVersion>()
                            .Where(m => m.RepositoryId == repository.RepositoryId)
                            .SingleOrDefault();
                        mgv.Version = downVersion - 1;

                        transaction.Commit();
                    }
                }
            }
        }
    }
}
