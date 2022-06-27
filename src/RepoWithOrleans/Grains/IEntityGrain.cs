using System.Threading.Tasks;
using Orleans;
using RepoWithOrleans.Repositories;
using Volo.Abp.Domain.Entities;

namespace RepoWithOrleans.Grains;

public interface IEntityGrain<TRepository, TEntity, TKey> : IGrain
    where TRepository : ICachedEntityRepository<TEntity, TKey> where TEntity : AggregateRoot<TKey>
{
    Task<TEntity> GetEntityOrNullAsync();

    Task StartUpdateAsync();

    Task FinishUpdateAsync();
}