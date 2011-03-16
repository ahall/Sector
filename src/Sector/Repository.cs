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
        private ISet<int> versions;

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
            versions = new HashSet<int>();
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
                    // Skip files that are not e.g. 1_blah.sql
                    continue;
                }

                versions.Add(version);
            }
        }

        public bool HasVersion(int version)
        {
            return versions.Contains(version);
        }

        public int GetVersion()
        {
            return versions.Max();
        }

        public string GetUpgradeSql(int version)
        {
            if (!versions.Contains(version))
                throw new SectorException("Repository does not contain this version");

            string filename = string.Format("{0}_upgrade.sql", version);
            string fullPath = Path.Combine(versionDir, filename);
            return File.ReadAllText(fullPath);
        }

        public string GetDowngradeSql(int version)
        {
            if (!versions.Contains(version))
                throw new SectorException("Repository does not contain this version");

            string filename = string.Format("{0}_downgrade.sql", version);
            string fullPath = Path.Combine(versionDir, filename);
            return File.ReadAllText(fullPath);
        }
    }
}

