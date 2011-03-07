using System;
using FluentNHibernate.Mapping;
using Sector.Entities;

namespace Sector.Mappings
{
    public class MigrateVersionMap : ClassMap<MigrateVersion>
    {
        public MigrateVersionMap()
        {
            Id(x => x.Id, "id");
            Map(x => x.RepositoryPath).Column("repository_path");
            Map(x => x.RepositoryId).Column("repository_id");
            Map(x => x.Version).Column("version");

            Table("migrate_version");
        }
    }
}



