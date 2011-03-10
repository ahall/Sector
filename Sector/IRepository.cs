using System;

namespace Sector
{
    public interface IRepository
    {
        string RepositoryId { get; }
        string RepositoryPath { get; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <returns>
        /// Returns the version of the repository, the latest version
        /// available.
        /// </returns>
        int GetVersion();

        /// <summary>
        /// Gets the upgrade sql.
        /// </summary>
        /// <returns>
        /// The sql for the version given.
        /// </returns>
        /// <param name='version'>
        /// Version.
        /// </param>
        string GetUpgradeSql(int version);

        /// <summary>
        /// Gets the downgrade sql.
        /// </summary>
        /// <returns>
        /// The sql for the version given.
        /// </returns>
        /// <param name='version'>
        /// Version.
        /// </param>
        string GetDowngradeSql(int version);
    }
}

