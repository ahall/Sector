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
    public class MigrateApi
    {
        private string connectionStr;
        private ISessionFactory dbFactory;

        public MigrateApi(string connectionStr)
        {
            this.connectionStr = connectionStr;

            var dbCfg = Fluently.Configure().Database(BuildDatabase());
            dbCfg.Mappings(x => x.FluentMappings.AddFromAssemblyOf<MigrateVersionMap>());
            this.dbFactory = dbCfg.BuildSessionFactory();
        }

        private IPersistenceConfigurer BuildDatabase()
        {
            return PostgreSQLConfiguration.PostgreSQL82.ConnectionString(connectionStr);
        }

        public bool IsVersionControlled(Repository repository)
        {
            // First make sure we're not already under version control.
            bool alreadyVersioned = true;
            using (ISession session = dbFactory.OpenSession())
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

        private void BuildSchema(NHibernate.Cfg.Configuration cfg)
        {
            new NHibernate.Tool.hbm2ddl.SchemaExport(cfg).Create(script: false, export: true);
        }

        public void VersionControl(Repository repository)
        {
            FluentConfiguration tempDbCfg = Fluently.Configure().Database(BuildDatabase());
            tempDbCfg.Mappings(x => x.FluentMappings.Add<MigrateVersionMap>());
            tempDbCfg.ExposeConfiguration(BuildSchema);
            ISessionFactory tempDbFactory = tempDbCfg.BuildSessionFactory();

            Console.WriteLine("Versioning the db, setting version to 0");

            using (ISession session = tempDbFactory.OpenSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                MigrateVersion mgv = new MigrateVersion(repositoryId: repository.RepositoryId,
                                                        repositoryPath: repository.RepositoryPath,
                                                        version: 0);
                session.Save(mgv);
                transaction.Commit();
            }

            // Clean-up, the garbage collector will do it and will also happen when the app exists
            // but for the sake of clean-ness and if the app runs for a long time.
            tempDbFactory.Dispose();
        }

        public int GetDbVersion(Repository repository)
        {
            using (ISession session = dbFactory.OpenSession())
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
