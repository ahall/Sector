using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
        private readonly HashSet<string> allowedEndings = new HashSet<string>();
        private const string COMP_SEPARATOR = "_";

        /// <summary>
        /// Stores version number to a base filename e.g. 1 -> 001, 2 -> 1
        /// which can then be translated to 001_upgrade.sql, 1_upgrade.sql
        /// </summary>
        private Dictionary<int, string> versions;

        public Repository(string repoPath)
        {
            allowedEndings.Add("upgrade.sql");
            allowedEndings.Add("downgrade.sql");

            RepositoryPath = repoPath;
            string configPath = Path.Combine(RepositoryPath, "sector.cfg");
            RepositoryId = GetRepIdFromPath(configPath);
            versionDir = Path.Combine(RepositoryPath, "versions");
            versions = new Dictionary<int, string>();
            ScanFiles();
        }

        private static string GetRepIdFromPath(string path)
        {
            string repoId = null;

            using (var fs = File.OpenRead(path))
            using (var sr = new StreamReader(fs))
            {
                while (true)
                {
                    string line = sr.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    if (!line.StartsWith("repository_id"))
                    {
                        continue;
                    }

                    // Found the line
                    var components = Regex.Split(line, @"\s*=\s*");
                    if (components.Length == 2)
                    {
                        repoId = components[1];
                        break;
                    }
                }
            }

            if (repoId == null)
            {
                throw new ArgumentException("repository_id not found in sector config");
            }

            return "";
        }

        private void ScanFiles()
        {
            foreach (FileInfo file in new DirectoryInfo(versionDir).GetFiles("*.sql"))
            {
                string[] elements = file.Name.Split(new string[] { COMP_SEPARATOR },
                                                    StringSplitOptions.RemoveEmptyEntries);
                int version;
                if ((elements.Length < 2) || (!int.TryParse(elements[0], out version)) ||
                    (!allowedEndings.Contains(elements.Last())))
                {
                    // Skip files that are not e.g. 1_blah.sql or 001_blah_upgrade.sql or
                    // 001_blah_blah_upgrade.sql.
                    continue;
                }

                if (!HasVersion(version))
                {
                    versions.Add(version, string.Join(COMP_SEPARATOR, elements.Take(elements.Length - 1)));
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

