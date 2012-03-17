using System;
using System.Data;

namespace Sector
{
    public interface ISectorDb : IDisposable
    {
        IDbConnection Connection { get; }

        /// <summary>
        /// Creates the migration table.
        /// </summary>
        void CreateMigrationTable();

        void Connect();
    }
}