using System;
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

        public int GetDbVersion(IRepository repository)
        {
            using (ISession session = sectorDb.DbFactory.OpenSession())
            {
                MigrateVersion mgv = session.QueryOver<MigrateVersion>()
                    .Where(m => m.RepositoryId == repository.RepositoryId)
                    .SingleOrDefault();
                if (mgv == null)
                    throw new Exception("Unable to fetch the db version");

                return mgv.Version;
            }
        }
    }
}
