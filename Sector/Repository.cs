using System;
using Nini.Config;
using System.IO;

namespace Sector
{
    /// <summary>
    /// A Repository that contains the SQL scripts we need.
    /// </summary>
    public class Repository
    {
        public string RepositoryId { get; private set; }
        public string RepositoryPath { get; private set; }

        IConfigSource configSource;
        IConfig mainConfig;

        public Repository(string repoPath)
        {
            RepositoryPath = repoPath;
            string configPath = Path.Combine(RepositoryPath, "sector.cfg");
            configSource = new IniConfigSource(configPath);
            mainConfig = configSource.Configs["main"];
            RepositoryId = mainConfig.GetString("repository_id");
        }

    }
}

