using System;
using System.IO;
using Mono.Data.Sqlite;


namespace Sector.Tests
{
    public static class TestUtils
    {
        private static readonly string TestFiles = GetTestFilesDir();
        private static readonly string TestArea = GetTestAreaDir();

        private static string GetTestFilesDir()
        {
            string solnDir = new DirectoryInfo(Environment.CurrentDirectory)
                                        .Parent.Parent.FullName;
            return Path.Combine(solnDir, "testfiles");
        }

        private static string GetTestAreaDir()
        {
            string solnDir = new DirectoryInfo(Environment.CurrentDirectory)
                                        .Parent.Parent.FullName;
            return Path.Combine(solnDir, "testarea");
        }

        public static string GetDbPath()
        {
            return Path.Combine(TestArea, "sector.db");
        }

        public static void CreateMigrationTable()
        {
            using (var conn = OpenDbconnection())
            using (var cmd = conn.CreateCommand())
            {
                const string sql = "CREATE TABLE {0}("
                                 + "repository_id VARCHAR(255) PRIMARY KEY NOT NULL,"
                                 + "repository_path VARCHAR(255),"
                                 + "version integer)";
                cmd.CommandText = string.Format(sql, "migrate_version");
                cmd.ExecuteNonQuery();
            }
        }

        public static ISectorDb MakeSectorDb()
        {
            return new SectorDb(OpenDbconnection());
        }

        public static SqliteConnection GetDbconnection()
        {
            return new SqliteConnection("Data Source=" + GetDbPath());
        }

        public static SqliteConnection OpenDbconnection()
        {
            var conn = GetDbconnection();
            conn.Open();
            return conn;
        }

        public static Repository MakeRepository()
        {
            return new Repository(Path.Combine(TestFiles, "repo"));
        }
    }
}

