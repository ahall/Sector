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
        /// Attempt to version control the database, one should call
        /// IsVersionControlled() first to make sure it's not already
        /// version controlled.
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
    }
}

