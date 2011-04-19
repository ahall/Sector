using System;

namespace Sector.Entities
{
    public class MigrateVersion
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        /// <value>
        /// The repository identifier.
        /// </value>
        public virtual string RepositoryId { get; private set; }

        public virtual string RepositoryPath { get; private set; }
        public virtual int Version { get; set; }

        protected MigrateVersion()
        {
        }

        public MigrateVersion(string repositoryId, string repositoryPath, int version = 0)
        {
            RepositoryId = repositoryId;
            RepositoryPath = repositoryPath;
        }
    }
}

