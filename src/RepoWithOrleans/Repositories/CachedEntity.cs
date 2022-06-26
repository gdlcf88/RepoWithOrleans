using Volo.Abp.Domain.Entities;

namespace RepoWithOrleans.Repositories;

public class CachedEntity<TEntity> where TEntity : class, IEntity
{
    public TEntity Entity { get; }

    public CachedEntity(TEntity entity)
    {
        Entity = entity;
    }
}