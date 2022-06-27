using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace RepoWithOrleans.Repositories;

public interface ICachedEntityRepository<TEntity> : ICachedEntityRepository<TEntity, Guid>
    where TEntity : AggregateRoot<Guid>
{
}

public interface ICachedEntityRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : AggregateRoot<TKey>
{
    Task<TEntity> GetFromStorageAsync(TKey id, bool includeDetails = true,
        CancellationToken cancellationToken = default);

    Task<TEntity> FindFromStorageAsync(TKey id, bool includeDetails = true,
        CancellationToken cancellationToken = default);

    Task<TEntity> UpdateToStorageAsync(TEntity entity, bool autoSave = false,
        CancellationToken cancellationToken = default);
}