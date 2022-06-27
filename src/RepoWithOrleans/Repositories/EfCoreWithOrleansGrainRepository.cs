using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using RepoWithOrleans.Grains;
using Volo.Abp;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace RepoWithOrleans.Repositories;

public class EfCoreWithOrleansGrainRepository<TDbContext, TEntity> :
    EfCoreWithOrleansGrainRepository<TDbContext, TEntity, Guid>
    where TDbContext : IEfCoreDbContext where TEntity : AggregateRoot<Guid>
{
    protected EfCoreWithOrleansGrainRepository(IDbContextProvider<TDbContext> dbContextProvider) : base(
        dbContextProvider)
    {
    }

    protected override IEntityGrain<EfCoreWithOrleansGrainRepository<TDbContext, TEntity, Guid>, TEntity, Guid>
        GetGrain(Guid id)
    {
        return GrainFactory
            .GetGrain<IGuidKeyEntityGrain<EfCoreWithOrleansGrainRepository<TDbContext, TEntity, Guid>, TEntity>>(id);
    }
}

public abstract class EfCoreWithOrleansGrainRepository<TDbContext, TEntity, TKey> :
    EfCoreRepository<TDbContext, TEntity, TKey>, ICachedEntityRepository<TEntity, TKey>
    where TDbContext : IEfCoreDbContext where TEntity : AggregateRoot<TKey>
{
    protected IGrainFactory GrainFactory => LazyServiceProvider.LazyGetRequiredService<IGrainFactory>();

    protected ILogger<EfCoreWithOrleansGrainRepository<TDbContext, TEntity, TKey>> Logger => LazyServiceProvider
        .LazyGetRequiredService<ILogger<EfCoreWithOrleansGrainRepository<TDbContext, TEntity, TKey>>>();

    protected IAbpDistributedLock AbpDistributedLock =>
        LazyServiceProvider.LazyGetRequiredService<IAbpDistributedLock>();

    public EfCoreWithOrleansGrainRepository(IDbContextProvider<TDbContext> dbContextProvider) : base(dbContextProvider)
    {
    }

    public override async Task<TEntity> GetAsync(TKey id, bool includeDetails = true,
        CancellationToken cancellationToken = new())
    {
        var cachedEntity = await FindAsync(id, true, cancellationToken);

        return cachedEntity ?? throw new EntityNotFoundException(typeof(TEntity));
    }

    public override async Task<TEntity> FindAsync(TKey id, bool includeDetails = true,
        CancellationToken cancellationToken = new())
    {
        var grain = GetGrain(id);

        TEntity entity;
        try
        {
            entity = await grain.GetEntityOrNullAsync();
        }
        catch (EntityIsChangingException)
        {
            // try again
            Logger.LogInformation("Try again to get cached entity from the grain.");
            entity = await grain.GetEntityOrNullAsync();
            Logger.LogInformation("The second try succeeded.");
        }

        if (entity is not null && UnitOfWorkManager.Current is not null)
        {
            (await GetDbSetAsync()).Attach(entity);
        }

        return entity;
    }

    public async Task<TEntity> GetFromStorageAsync(TKey id, bool includeDetails = true,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindFromStorageAsync(id, includeDetails, GetCancellationToken(cancellationToken));

        if (entity == null)
        {
            throw new EntityNotFoundException(typeof(TEntity), id);
        }

        return entity;
    }

    public async Task<TEntity> FindFromStorageAsync(TKey id, bool includeDetails = true,
        CancellationToken cancellationToken = default)
    {
        return includeDetails
            ? await (await WithDetailsAsync()).OrderBy(e => e.Id)
                .FirstOrDefaultAsync(e => e.Id.Equals(id), GetCancellationToken(cancellationToken))
            : await (await GetDbSetAsync()).FindAsync(new object[] { id }, GetCancellationToken(cancellationToken));
    }

    public virtual async Task<TEntity> UpdateToStorageAsync(TEntity entity, bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();

        dbContext.Attach(entity);

        var updatedEntity = dbContext.Update(entity).Entity;

        if (autoSave)
        {
            await dbContext.SaveChangesAsync(GetCancellationToken(cancellationToken));
        }

        return updatedEntity;
    }

    public override async Task<TEntity> InsertAsync(TEntity entity, bool autoSave = false,
        CancellationToken cancellationToken = new())
    {
        var grain = GetGrain(entity.Id);

        await grain.StartUpdateAsync();

        CheckAndSetId(entity);

        var dbContext = await GetDbContextAsync();

        var savedEntity = (await dbContext.Set<TEntity>().AddAsync(entity, GetCancellationToken(cancellationToken)))
            .Entity;

        if (autoSave)
        {
            await dbContext.SaveChangesAsync(GetCancellationToken(cancellationToken));
        }

        UnitOfWorkManager.Current.OnCompleted(async () => { await grain.FinishUpdateAsync(); });

        return savedEntity;
    }

    protected abstract IEntityGrain<EfCoreWithOrleansGrainRepository<TDbContext, TEntity, TKey>, TEntity, TKey>
        GetGrain(TKey id);

    public override async Task<TEntity> UpdateAsync(TEntity entity, bool autoSave = false,
        CancellationToken cancellationToken = new())
    {
        var grain = GetGrain(entity.Id);

        await using var handle = await AbpDistributedLock.TryAcquireAsync(await GetLockNameAsync(entity),
            TimeSpan.FromSeconds(3), cancellationToken);

        if (handle is null)
        {
            // Todo
            throw new AbpException();
        }

        await grain.StartUpdateAsync();
        entity = await base.UpdateAsync(entity, autoSave, cancellationToken);

        UnitOfWorkManager.Current.OnCompleted(async () => { await grain.FinishUpdateAsync(); });

        return entity;
    }

    public override async Task DeleteAsync(TEntity entity, bool autoSave = false,
        CancellationToken cancellationToken = new())
    {
        var grain = GetGrain(entity.Id);

        await using var handle = await AbpDistributedLock.TryAcquireAsync(await GetLockNameAsync(entity),
            TimeSpan.FromSeconds(3), cancellationToken);

        if (handle is null)
        {
            // Todo
            throw new AbpException();
        }

        await grain.StartUpdateAsync();

        var dbContext = await GetDbContextAsync();

        dbContext.Set<TEntity>().Remove(entity);

        if (autoSave)
        {
            await dbContext.SaveChangesAsync(GetCancellationToken(cancellationToken));
        }

        UnitOfWorkManager.Current.OnCompleted(async () => { await grain.FinishUpdateAsync(); });
    }

    protected virtual Task<string> GetLockNameAsync(TEntity entity)
    {
        return Task.FromResult($"{nameof(TEntity)}:{entity.Id}");
    }
}