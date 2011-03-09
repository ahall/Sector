using System;

namespace Sector
{
    public interface IRepository
    {
        string RepositoryId { get; }
        string RepositoryPath { get; }
    }
}

