using System;
using Microsoft.Extensions.Logging;
using Orleans;
using RepoWithOrleans.Repositories;
using Volo.Abp.Domain.Entities;

namespace RepoWithOrleans.Grains;

public class GuidKeyEntityGrain<TRepository, TEntity> : EntityGrainBase<TRepository, TEntity, Guid>,
    IGuidKeyEntityGrain<TRepository, TEntity>
    where TRepository : ICachedEntityRepository<TEntity, Guid>
    where TEntity : AggregateRoot<Guid>
{
    public GuidKeyEntityGrain(ILogger<EntityGrainBase<TRepository, TEntity, Guid>> logger) : base(logger)
    {
    }

    protected override Guid GetId() => this.GetPrimaryKey();
}