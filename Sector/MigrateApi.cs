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

        /// <summary>
        /// Tries the version control.
        /// </summary>
        /// <returns>
        /// True if we should version control the db, false otherwise.
        /// </returns>
        public bool TryVersionControl(string repositoryPath, string repositoryId)
        {
            // First make sure we're not already under version control.
            bool alreadyVersioned = true;
            using (ISession session = dbFactory.OpenSession())
            {
                try
                {
                    MigrateVersion mgv = session.QueryOver<MigrateVersion>()
                        .Where(m => m.RepositoryId == repositoryId)
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

        public void VersionControl(string repositoryPath, string repositoryId)
        {
            FluentConfiguration tempDbCfg = Fluently.Configure().Database(BuildDatabase());
            tempDbCfg.Mappings(x => x.FluentMappings.Add<MigrateVersionMap>());
            tempDbCfg.ExposeConfiguration(BuildSchema);
            ISessionFactory tempDbFactory = tempDbCfg.BuildSessionFactory();

            Console.WriteLine("Versioning the db, setting version to 0");

            using (ISession session = tempDbFactory.OpenSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                MigrateVersion mgv = new MigrateVersion(repositoryId: repositoryId,
                                                        repositoryPath: repositoryPath,
                                                        version: 0);
                session.Save(mgv);
                transaction.Commit();
            }
        }
    }
}
