using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Sector
{
    public class MigrateApi : IMigrateApi
    {
        private ISectorDb sectorDb;

        public MigrateApi(ISectorDb sectorDb)
        {
            this.sectorDb = sectorDb;
        }

        public bool IsVersionControlled(IRepository repository)
        {
            // First make sure we're not already under version control.
            bool alreadyVersioned = true;

            try
            {
                 // Now update the version table.
                using (var sqlCommand = sectorDb.Connection.CreateCommand())
                {
                    // Upgrade the version info.
                    const string templ = "SELECT count(*) FROM {0} WHERE repository_id = @RepoId";
                    sqlCommand.CommandText = string.Format(templ, SectorDb.TableName, repository.RepositoryId);

                    var param = sqlCommand.CreateParameter();
                    param.DbType = DbType.String;
                    param.ParameterName = "@RepoId";
                    param.Value = repository.RepositoryId;
                    sqlCommand.Parameters.Add(param);

                    long ret = (long)sqlCommand.ExecuteScalar();
                    if (ret == 0)
                    {
                        alreadyVersioned = false;
                    }
                }
            }
            catch (Exception ex)
            {
                alreadyVersioned = false;
            }

            return alreadyVersioned;
        }

        public void VersionControl(IRepository repository)
        {
            if (IsVersionControlled(repository))
            {
                throw new SectorException("Already version controlled");
            }

            // Create the migration table first.
            sectorDb.CreateMigrationTable();

            using (var sqlCommand = sectorDb.Connection.CreateCommand())
            {
                // Upgrade the version info.
                const string templ = "INSERT INTO {0} (repository_id, repository_path, version) "
                                   + "VALUES (@RepoId, @RepoPath, @RepoVer)";
                sqlCommand.CommandText = string.Format(templ, SectorDb.TableName);

                {
                    var param = sqlCommand.CreateParameter();
                    param.DbType = DbType.String;
                    param.ParameterName = "@RepoId";
                    param.Value = repository.RepositoryId;
                    sqlCommand.Parameters.Add(param);
                }
                {
                    var param = sqlCommand.CreateParameter();
                    param.DbType = DbType.String;
                    param.ParameterName = "@RepoPath";
                    param.Value = repository.RepositoryPath;
                    sqlCommand.Parameters.Add(param);
                }
                {
                    var param = sqlCommand.CreateParameter();
                    param.DbType = DbType.Int32;
                    param.ParameterName = "@RepoVer";
                    param.Value = 0;
                    sqlCommand.Parameters.Add(param);
                }

                sqlCommand.ExecuteNonQuery();
            }
        }

        public int GetDbVersion(IRepository repository)
        {
            using (var sqlCommand = sectorDb.Connection.CreateCommand())
            {
                // Upgrade the version info.
                const string templ = "SELECT version FROM {0} WHERE repository_id = @RepoId";
                sqlCommand.CommandText = string.Format(templ, SectorDb.TableName);

                var param = sqlCommand.CreateParameter();
                param.DbType = DbType.String;
                param.ParameterName = "@RepoId";
                param.Value = repository.RepositoryId;
                sqlCommand.Parameters.Add(param);

                object ret = sqlCommand.ExecuteScalar();
                if (ret == null)
                {
                    throw new SectorException("Unable to fetch the db version");
                }

                // Needed, sqlite returns as long as casting object which is long to
                // int is not allowed.
                long retLong = (long)ret;
                return (int)retLong;
            }
        }

        public void Upgrade(IRepository repository)
        {
            int version = repository.GetVersion();
            Upgrade(repository, version);
        }

        public void Upgrade(IRepository repository, int version)
        {
            int dbVersion = GetDbVersion(repository);
            if (dbVersion >= version)
            {
                // Already up higher than this so do nothing.
                return;
            }

            int highestAvailable = repository.GetVersion();
            if (version > highestAvailable)
            {
                throw new SectorException("Version requested higher than latest available");
            }

            if (!repository.HasVersion(version))
            {
                throw new SectorException("Version requested not available in the repository");
            }

            int steps = version - dbVersion;
            foreach (var upVersion in Enumerable.Range(dbVersion + 1, steps))
            {
                using (var transaction = sectorDb.Connection.BeginTransaction())
                {
                    using (var sqlCommand = sectorDb.Connection.CreateCommand())
                    {
                        // Run the SQL for the next version.
                        sqlCommand.CommandText = repository.GetUpgradeSql(upVersion);
                        sqlCommand.ExecuteNonQuery();
                    }
    
                    // Now update the version table.
                    using (var sqlCommand = sectorDb.Connection.CreateCommand())
                    {
                        // Upgrade the version info.
                        const string templ = "UPDATE {0} SET version = @RepoVer WHERE repository_id = @RepoId";
                        sqlCommand.CommandText = string.Format(templ, SectorDb.TableName);

                        {
                            var param = sqlCommand.CreateParameter();
                            param.DbType = DbType.String;
                            param.ParameterName = "@RepoId";
                            param.Value = repository.RepositoryId;
                            sqlCommand.Parameters.Add(param);
                        }
                        {
                            var param = sqlCommand.CreateParameter();
                            param.DbType = DbType.Int32;
                            param.ParameterName = "@RepoVer";
                            param.Value = upVersion;
                            sqlCommand.Parameters.Add(param);
                        }

                        sqlCommand.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }
        }

        public void Downgrade(IRepository repository, int version)
        {
            int dbVersion = GetDbVersion(repository);
            if (dbVersion <= version)
            {
                // Already lower or same as current version.
                return;
            }

            if (version < 0)
            {
                throw new SectorException("Version cannot be less than 0");
            }

            // Example downgrade from 4 back to 0:
            // 4_downgrade set version to 3
            // 3_downgrade set version to 2,
            // 2_downgrade set version to 1
            // 1_downgrade set version = 0
            // Means we need to go over range 1, 4 in reverse order, here we iterate
            // over range 1, 4 reversed to { 4, 1 } == 4, 3, 2, 1 and end with dbVersion 0
            int steps = dbVersion - version;
            foreach (var downVersion in Enumerable.Range(version + 1, steps).Reverse())
            {
                using (var transaction = sectorDb.Connection.BeginTransaction())
                {
                    using (var sqlCommand = sectorDb.Connection.CreateCommand())
                    {
                        // Run the SQL for the next version.
                        sqlCommand.CommandText = repository.GetDowngradeSql(downVersion);
                        sqlCommand.ExecuteNonQuery();
                    }
    
                    // Now update the version table.
                    using (var sqlCommand = sectorDb.Connection.CreateCommand())
                    {
                        // Upgrade the version info.
                        const string templ = "UPDATE {0} SET version = @RepoVer WHERE repository_id = @RepoId";
                        sqlCommand.CommandText = string.Format(templ, SectorDb.TableName);

                        {
                            var param = sqlCommand.CreateParameter();
                            param.DbType = DbType.String;
                            param.ParameterName = "@RepoId";
                            param.Value = repository.RepositoryId;
                            sqlCommand.Parameters.Add(param);
                        }
                        {
                            var param = sqlCommand.CreateParameter();
                            param.DbType = DbType.Int32;
                            param.ParameterName = "@RepoVer";
                            param.Value = downVersion - 1;
                            sqlCommand.Parameters.Add(param);
                        }

                        sqlCommand.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }
        }
    }
}
