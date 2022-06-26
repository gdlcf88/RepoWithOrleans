using System;
using System.Threading;
using System.Threading.Tasks;
using RepoWithOrleans.Entities;
using Volo.Abp.Domain.Repositories;

namespace RepoWithOrleans.Repositories;

public interface IBookRepository : IRepository<Book, Guid>
{
    Task<Book> GetFromDatabaseAsync(Guid id, bool includeDetails = true, CancellationToken cancellationToken = default);

    Task<Book> FindFromDatabaseAsync(Guid id, bool includeDetails = true,
        CancellationToken cancellationToken = default);

    Task<Book> UpdateToDatabaseAsync(Book entity, bool autoSave = false, CancellationToken cancellationToken = default);
}