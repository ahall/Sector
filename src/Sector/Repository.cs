using System;
using Nini.Config;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace Sector
{
    /// <summary>
    /// A Repository that contains the SQL scripts we need.
    /// </summary>
    public class Repository : IRepository
    {
        public string RepositoryId { get; private set; }
        public string RepositoryPath { get; private set; }
        private string versionDir;

        /// <summary>
        /// Stores version number to a base filename e.g. 1 -> 001, 2 -> 1
        /// which can then be translated to 001_upgrade.sql, 1_upgrade.sql
        /// </summary>
        private Dictionary<int, string> versions;

        IConfigSource configSource;
        IConfig mainConfig;

        public Repository(string repoPath)
        {
            RepositoryPath = repoPath;
            string configPath = Path.Combine(RepositoryPath, "sector.cfg");
            configSource = new IniConfigSource(configPath);
            mainConfig = configSource.Configs["main"];
            RepositoryId = mainConfig.GetString("repository_id");
            versionDir = Path.Combine(RepositoryPath, "versions");
            versions = new Dictionary<int, string>();
            ScanFiles();
        }

        private void ScanFiles()
        {
            foreach (FileInfo file in new DirectoryInfo(versionDir).GetFiles("*.sql"))
            {
                var elements = file.Name.Split(new char[] { '_' }, 2);
                int version;
                if ((elements.Length < 2) || (!int.TryParse(elements[0], out version)))
                {
                    // Skip files that are not e.g. 1_blah.sql or 001_blah.sql
                    continue;
                }

                if (!HasVersion(version))
                {
                    versions.Add(version, elements[0]);
                }
            }
        }

        public bool HasVersion(int version)
        {
            return versions.ContainsKey(version);
        }

        public int GetVersion()
        {
            return versions.Keys.Max();
        }

        public string GetUpgradeSql(int version)
        {
            if (!versions.ContainsKey(version))
                throw new SectorException("Repository does not contain this version");

            string filename = string.Format("{0}_upgrade.sql", versions[version]);
            string fullPath = Path.Combine(versionDir, filename);
            return File.ReadAllText(fullPath);
        }

        public string GetDowngradeSql(int version)
        {
            if (!versions.ContainsKey(version))
                throw new SectorException("Repository does not contain this version");

            string filename = string.Format("{0}_downgrade.sql", versions[version]);
            string fullPath = Path.Combine(versionDir, filename);
            return File.ReadAllText(fullPath);
        }
    }
}

