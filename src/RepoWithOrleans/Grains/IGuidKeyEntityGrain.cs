using System;
using Orleans;
using RepoWithOrleans.Repositories;
using Volo.Abp.Domain.Entities;

namespace RepoWithOrleans.Grains;

public interface IGuidKeyEntityGrain<TRepository, TEntity> : IGrainWithGuidKey, IEntityGrain<TRepository, TEntity, Guid>
    where TRepository : ICachedEntityRepository<TEntity, Guid> where TEntity : AggregateRoot<Guid>
{
}