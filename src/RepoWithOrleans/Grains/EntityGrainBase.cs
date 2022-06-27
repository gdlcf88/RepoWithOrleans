using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using RepoWithOrleans.Repositories;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Uow;

namespace RepoWithOrleans.Grains;

public abstract class EntityGrainBase<TRepository, TEntity, TKey> : Grain, IEntityGrain<TRepository, TEntity, TKey>
    where TRepository : ICachedEntityRepository<TEntity, TKey> where TEntity : AggregateRoot<TKey>
{
    private readonly ILogger<EntityGrainBase<TRepository, TEntity, TKey>> _logger;
    private IEntityCurrentStampGrain CurrentStampGrain { get; set; }

    protected TEntity Entity { get; set; }

    public EntityGrainBase(ILogger<EntityGrainBase<TRepository, TEntity, TKey>> logger)
    {
        _logger = logger;
    }

    public override async Task OnActivateAsync()
    {
        var grainFactory = ServiceProvider.GetRequiredService<IGrainFactory>();

        CurrentStampGrain =
            grainFactory.GetGrain<IEntityCurrentStampGrain>($"{typeof(TEntity).FullName}:{this.GetId()}");

        await ReadStateAsync();
    }

    protected virtual async Task ReadStateAsync()
    {
        using var scope = ServiceProvider.CreateScope();

        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        using var uow = uowManager.Begin(requiresNew: true, isTransactional: false);

        var repository = scope.ServiceProvider.GetRequiredService<ICachedEntityRepository<TEntity, TKey>>();

        Entity = await repository.FindFromStorageAsync(this.GetId());
    }

    protected abstract TKey GetId();

    public async Task<TEntity> GetEntityOrNullAsync()
    {
        var currentStamp = await CurrentStampGrain.GetCurrentStampAsync();

        if (currentStamp is not null && !await TryEmptyUpdateAsync())
        {
            throw new EntityIsChangingException();
        }

        return Entity;
    }

    protected virtual async Task<bool> TryEmptyUpdateAsync()
    {
        using var scope = ServiceProvider.CreateScope();

        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        try
        {
            using var uow = uowManager.Begin(requiresNew: true, isTransactional: false);

            var repository = scope.ServiceProvider.GetRequiredService<ICachedEntityRepository<TEntity, TKey>>();

            var entity = await repository.GetFromStorageAsync(Entity.Id);

            var currentStamp = await CurrentStampGrain.GetCurrentStampAsync();

            if (entity.ConcurrencyStamp != currentStamp)
            {
                await FinishUpdateAsync();
                return true;
            }

            await repository.UpdateToStorageAsync(entity, true);

            await uow.CompleteAsync();

            Entity = entity;

            await CurrentStampGrain.SetCurrentStampAsync(null);

            _logger.LogInformation("Try update stamp succeeded.");
        }
        catch (AbpDbConcurrencyException)
        {
            _logger.LogInformation("Try update stamp failed.");

            return false;
        }

        return true;
    }

    public virtual async Task StartUpdateAsync()
    {
        if (await CurrentStampGrain.GetCurrentStampAsync() is not null)
        {
            // Todo
            throw new AbpException();
        }

        await CurrentStampGrain.SetCurrentStampAsync(Entity?.ConcurrencyStamp ?? string.Empty);
    }

    public virtual async Task FinishUpdateAsync()
    {
        if (await CurrentStampGrain.GetCurrentStampAsync() is null)
        {
            return;
        }

        await ReadStateAsync();
        await CurrentStampGrain.SetCurrentStampAsync(null);
    }
}