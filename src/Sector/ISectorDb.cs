using System;
using NHibernate;

namespace Sector
{
    public interface ISectorDb
    {
        /// <summary>
        /// Gets the db factory.
        /// </summary>
        /// <value>
        /// The main db factory.
        /// </value>
        ISessionFactory DbFactory { get; }

        /// <summary>
        /// Creates the migration table.
        /// </summary>
        void CreateMigrationTable();
    }
}

