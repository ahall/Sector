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
        public ISessionFactory DbFactory { get; private set; }
        private string connectionString;
        private const string CONNSTRING_TEMPLATE =
                           @"Server={0};Database={1};User Id={2};Password={3}";

        public SectorDb(string server, string username,
                        string password, string database)
        {
            connectionString = string.Format(CONNSTRING_TEMPLATE, server,
                                             database, username, password);
            var dbCfg = Fluently.Configure().Database(BuildDatabase());
            dbCfg.Mappings(x => x.FluentMappings.AddFromAssemblyOf<MigrateVersionMap>());
            DbFactory = dbCfg.BuildSessionFactory();
        }

        private IPersistenceConfigurer BuildDatabase()
        {
            return PostgreSQLConfiguration.PostgreSQL82.ConnectionString(connectionString);
        }

        private void BuildSchema(NHibernate.Cfg.Configuration cfg)
        {
            new SchemaExport(cfg).Create(script: false, export: true);
        }

        public void CreateMigrationTable()
        {
            // The trick here is to use schema export which practically does a
            // DROP/CREATE. We create the factory with only one mapping and
            // dispose of it immediately after.
            FluentConfiguration dbCfg = Fluently.Configure().Database(BuildDatabase());
            dbCfg.Mappings(x => x.FluentMappings.Add<MigrateVersionMap>());
            dbCfg.ExposeConfiguration(BuildSchema);
            dbCfg.BuildSessionFactory().Dispose();
        }
    }
}

