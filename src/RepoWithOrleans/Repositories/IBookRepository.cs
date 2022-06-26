using System;
using System.Threading;
using System.Threading.Tasks;
using RepoWithOrleans.Entities;
using Volo.Abp.Domain.Repositories;

namespace RepoWithOrleans.Repositories;

public interface IBookRepository : IRepository<Book, Guid>
{
    Task<Book> ForceUpdateAsync(Book entity, bool autoSave = false, CancellationToken cancellationToken = default);
}