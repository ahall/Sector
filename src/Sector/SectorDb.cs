using System;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Cfg;
using Sector.Mappings;

namespace Sector
{
    public class SectorDb : ISectorDb
    {
        private IPersistenceConfigurer dbConfigurer;
        public ISessionFactory DbFactory { get; private set; }

        public SectorDb(IPersistenceConfigurer dbConfigurer)
        {
            this.dbConfigurer = dbConfigurer;
            DbFactory = Fluently.Configure()
                    .Database(dbConfigurer)
                    .Mappings(x => x.FluentMappings.Add<MigrateVersionMap>())
                    .BuildSessionFactory();
        }

        public void CreateMigrationTable()
        {
            // The trick here is to use schema export which practically does a
            // DROP/CREATE. We create the factory with only one mapping and
            // dispose of it immediately after.
            Fluently.Configure()
                    .Database(dbConfigurer)
                    .Mappings(x => x.FluentMappings.Add<MigrateVersionMap>())
                    .ExposeConfiguration(cfg => new SchemaExport(cfg).Create(script: false, export: true))
                    .BuildSessionFactory()
                    .Dispose();
        }
    }
}

