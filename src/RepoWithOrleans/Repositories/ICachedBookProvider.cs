using System;
using System.Threading.Tasks;
using RepoWithOrleans.Entities;

namespace RepoWithOrleans.Repositories;

public interface ICachedBookProvider
{
    Task<CachedEntity<Book>> GetAsync(Guid id);

    Task<CachedEntity<Book>> FindAsync(Guid id);
}