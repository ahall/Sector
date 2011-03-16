using System;

namespace Sector
{
    /// <summary>
    /// An API for managing database schemas.
    /// </summary>
    public interface IMigrateApi
    {
        /// <summary>
        /// Checks if a database is version controlled.
        /// </summary>
        /// <returns>
        /// True if the DB is version controlled, false otherwise.
        /// </returns>
        /// <param name='repository'>
        /// If set to <c>true</c> repository.
        /// </param>
        bool IsVersionControlled(IRepository repository);

        /// <summary>
        /// Version control the database, if it is already version
        /// controlled, a SectorException gets thrown.
        /// </summary>
        /// <param name='repository'>
        /// Repository.
        /// </param>
        void VersionControl(IRepository repository);

        /// <summary>
        /// Gets the current db version.
        /// </summary>
        /// <returns>
        /// The current db version.
        /// </returns>
        /// <param name='repository'>
        /// Repository.
        /// </param>
        int GetDbVersion(IRepository repository);

        /// <summary>
        /// Upgrades to the latest available version.
        /// </summary>
        void Upgrade(IRepository repository);

        /// <summary>
        /// Upgrades to the version specified..
        /// </summary>
        void Upgrade(IRepository repository, int version);

        /// <summary>
        /// Downgrades to the version specified..
        /// </summary>
        void Downgrade(IRepository repository, int version);
    }
}

