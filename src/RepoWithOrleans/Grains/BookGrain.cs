using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using RepoWithOrleans.Entities;
using RepoWithOrleans.Repositories;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Uow;

namespace RepoWithOrleans.Grains;

public class BookGrain : Grain, IBookGrain
{
    private readonly ILogger<BookGrain> _logger;
    private IEntityCurrentStampGrain CurrentStampGrain { get; set; }

    protected Book Entity { get; set; }

    public BookGrain(ILogger<BookGrain> logger)
    {
        _logger = logger;
    }

    public override async Task OnActivateAsync()
    {
        var grainFactory = ServiceProvider.GetRequiredService<IGrainFactory>();

        CurrentStampGrain = grainFactory.GetGrain<IEntityCurrentStampGrain>($"Book:{this.GetPrimaryKey()}");

        await ReadStateAsync();
    }

    protected async Task ReadStateAsync()
    {
        using var scope = ServiceProvider.CreateScope();

        var bookRepository = scope.ServiceProvider.GetRequiredService<IBookRepository>();

        Entity = await bookRepository.FindAsync(this.GetPrimaryKey());
    }

    public async Task<Book> GetEntityOrNullAsync()
    {
        var currentStamp = await CurrentStampGrain.GetCurrentStampAsync();

        if (currentStamp is not null && !await TryUpdateStampAsync())
        {
            throw new EntityIsChangingException();
        }

        return Entity;
    }

    protected async Task<bool> TryUpdateStampAsync()
    {
        using var scope = ServiceProvider.CreateScope();

        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        using var uow = uowManager.Begin(requiresNew: true, isTransactional: false);

        try
        {
            var bookRepository = scope.ServiceProvider.GetRequiredService<IBookRepository>();

            var entity = await bookRepository.GetAsync(Entity.Id);

            var currentStamp = await CurrentStampGrain.GetCurrentStampAsync();

            if (entity.ConcurrencyStamp != currentStamp)
            {
                await FinishUpdateAsync();
                return true;
            }

            await bookRepository.ForceUpdateAsync(entity, true);

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

    public async Task StartUpdateAsync()
    {
        if (await CurrentStampGrain.GetCurrentStampAsync() is not null)
        {
            // Todo
            throw new AbpException();
        }

        await CurrentStampGrain.SetCurrentStampAsync(Entity?.ConcurrencyStamp ?? string.Empty);
    }

    public async Task FinishUpdateAsync()
    {
        if (await CurrentStampGrain.GetCurrentStampAsync() is null)
        {
            return;
        }

        await ReadStateAsync();
        await CurrentStampGrain.SetCurrentStampAsync(null);
    }
}