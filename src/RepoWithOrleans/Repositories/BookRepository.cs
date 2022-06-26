using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using RepoWithOrleans.Data;
using RepoWithOrleans.Entities;
using RepoWithOrleans.Grains;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace RepoWithOrleans.Repositories;

public class BookRepository : EfCoreRepository<MyDbContext, Book, Guid>, IBookRepository, ITransientDependency
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<BookRepository> _logger;
    private readonly IAbpDistributedLock _abpDistributedLock;

    public BookRepository(
        IGrainFactory grainFactory,
        ILogger<BookRepository> logger,
        IAbpDistributedLock abpDistributedLock,
        IDbContextProvider<MyDbContext> dbContextProvider) : base(dbContextProvider)
    {
        _grainFactory = grainFactory;
        _logger = logger;
        _abpDistributedLock = abpDistributedLock;
    }

    public override async Task<Book> GetAsync(Guid id, bool includeDetails = true,
        CancellationToken cancellationToken = new())
    {
        var cachedEntity = await FindAsync(id, true, cancellationToken);

        return cachedEntity ?? throw new EntityNotFoundException(typeof(Book));
    }

    public override async Task<Book> FindAsync(Guid id, bool includeDetails = true,
        CancellationToken cancellationToken = new())
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

        if (entity is not null && UnitOfWorkManager.Current is not null)
        {
            (await GetDbSetAsync()).Attach(entity);
        }

        return entity;
    }

    public async Task<Book> GetFromDatabaseAsync(Guid id, bool includeDetails = true,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindFromDatabaseAsync(id, includeDetails, GetCancellationToken(cancellationToken));

        if (entity == null)
        {
            throw new EntityNotFoundException(typeof(Book), id);
        }

        return entity;
    }

    public async Task<Book> FindFromDatabaseAsync(Guid id, bool includeDetails = true,
        CancellationToken cancellationToken = default)
    {
        return includeDetails
            ? await (await WithDetailsAsync()).OrderBy(e => e.Id)
                .FirstOrDefaultAsync(e => e.Id.Equals(id), GetCancellationToken(cancellationToken))
            : await (await GetDbSetAsync()).FindAsync(new object[] { id }, GetCancellationToken(cancellationToken));
    }

    public virtual async Task<Book> UpdateToDatabaseAsync(Book entity, bool autoSave = false,
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

    public override async Task<Book> InsertAsync(Book entity, bool autoSave = false,
        CancellationToken cancellationToken = new())
    {
        var grain = _grainFactory.GetGrain<IBookGrain>(entity.Id);

        await grain.StartUpdateAsync();

        CheckAndSetId(entity);

        var dbContext = await GetDbContextAsync();

        var savedEntity = (await dbContext.Set<Book>().AddAsync(entity, GetCancellationToken(cancellationToken)))
            .Entity;

        if (autoSave)
        {
            await dbContext.SaveChangesAsync(GetCancellationToken(cancellationToken));
        }

        UnitOfWorkManager.Current.OnCompleted(async () => { await grain.FinishUpdateAsync(); });

        return savedEntity;
    }

    public override async Task<Book> UpdateAsync(Book entity, bool autoSave = false,
        CancellationToken cancellationToken = new())
    {
        var grain = _grainFactory.GetGrain<IBookGrain>(entity.Id);

        await using var handle = await _abpDistributedLock.TryAcquireAsync(await GetLockNameAsync(entity),
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

    public override async Task DeleteAsync(Book entity, bool autoSave = false,
        CancellationToken cancellationToken = new())
    {
        var grain = _grainFactory.GetGrain<IBookGrain>(entity.Id);

        await using var handle = await _abpDistributedLock.TryAcquireAsync(await GetLockNameAsync(entity),
            TimeSpan.FromSeconds(3), cancellationToken);

        if (handle is null)
        {
            // Todo
            throw new AbpException();
        }

        await grain.StartUpdateAsync();

        var dbContext = await GetDbContextAsync();

        dbContext.Set<Book>().Remove(entity);

        if (autoSave)
        {
            await dbContext.SaveChangesAsync(GetCancellationToken(cancellationToken));
        }

        UnitOfWorkManager.Current.OnCompleted(async () => { await grain.FinishUpdateAsync(); });
    }

    protected virtual async Task<string> GetLockNameAsync(Book entity)
    {
        return $"{nameof(Book)}:{entity.Id}";
    }
}