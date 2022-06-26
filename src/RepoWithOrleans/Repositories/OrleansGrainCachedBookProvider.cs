using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using RepoWithOrleans.Entities;
using RepoWithOrleans.Grains;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;

namespace RepoWithOrleans.Repositories;

public class OrleansGrainCachedBookProvider : ICachedBookProvider, ITransientDependency
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<OrleansGrainCachedBookProvider> _logger;

    public OrleansGrainCachedBookProvider(
        IGrainFactory grainFactory,
        ILogger<OrleansGrainCachedBookProvider> logger)
    {
        _grainFactory = grainFactory;
        _logger = logger;
    }
    
    public virtual async Task<CachedEntity<Book>> GetAsync(Guid id)
    {
        var cachedEntity = await FindAsync(id);

        return cachedEntity ?? throw new EntityNotFoundException(typeof(Book));
    }

    public virtual async Task<CachedEntity<Book>> FindAsync(Guid id)
    {
        var grain = _grainFactory.GetGrain<IBookGrain>(id);

        Book entity;
        try
        {
            entity = await grain.GetEntityOrNullAsync();
        }
        catch (EntityIsChangingException)
        {
            // try again
            _logger.LogInformation("Try again to get cached entity from the grain.");
            entity = await grain.GetEntityOrNullAsync();
            _logger.LogInformation("The second try succeeded.");
        }

        return entity is not null ? new CachedEntity<Book>(entity) : null;
    }
}