using System;
using RepoWithOrleans.Entities;

namespace RepoWithOrleans.Repositories;

public interface IBookRepository : ICachedEntityRepository<Book, Guid>
{
}