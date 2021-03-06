using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RepoWithOrleans.Entities;
using RepoWithOrleans.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace RepoWithOrleans;

public class MyService : ITransientDependency
{
    private readonly IBookRepository _bookRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    public ILogger<MyService> Logger { get; set; }

    public MyService(
        IBookRepository bookRepository,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _bookRepository = bookRepository;
        _unitOfWorkManager = unitOfWorkManager;
        Logger = NullLogger<MyService>.Instance;
    }

    public virtual async Task RunAsync()
    {
        await CachedEntityTestAsync();
        await ConcurrentTestAsync();
    }

    private async Task CachedEntityTestAsync()
    {
        Console.WriteLine("----- Cached Entity Test Begin -----");

        using (var uow = _unitOfWorkManager.Begin(isTransactional: true))
        {
            Console.WriteLine($"Start to create Book1");
            await _bookRepository.InsertAsync(new Book(Consts.Book1Id, "MyBook"), true);
            Console.WriteLine($"Book1 created");
            await uow.CompleteAsync();
        }

        var book1 = await _bookRepository.GetFromDatabaseAsync(Consts.Book1Id);
        Console.WriteLine($"Get Book1 from database, the sold is: {book1.Sold}");
        var book1Cache = await _bookRepository.GetAsync(Consts.Book1Id);
        Console.WriteLine($"Get Book1 from grain, the sold is: {book1Cache.Sold}");

        using (var uow = _unitOfWorkManager.Begin(isTransactional: true))
        {
            Console.WriteLine($"Start to increase Sold of Book1");
            book1 = await _bookRepository.GetAsync(Consts.Book1Id);
            Console.WriteLine($"Original Book1 sold is: {book1.Sold}");
            book1.IncreaseSold(2);
            await _bookRepository.UpdateAsync(book1, true);
            Console.WriteLine($"Increase 2, the new Book1 sold is: {book1.Sold}");
            await uow.CompleteAsync();
        }

        book1 = await _bookRepository.GetFromDatabaseAsync(Consts.Book1Id);
        Console.WriteLine($"Get Book1 from database, the sold is: {book1.Sold}");
        book1Cache = await _bookRepository.GetAsync(Consts.Book1Id);
        Console.WriteLine($"Get Book1 from grain, the sold is: {book1Cache.Sold}");

        using (var uow = _unitOfWorkManager.Begin(isTransactional: true))
        {
            Console.WriteLine($"Start to decrease Sold of Book1");
            book1 = await _bookRepository.GetAsync(Consts.Book1Id);
            Console.WriteLine($"Original Book1 sold is: {book1.Sold}");
            book1.IncreaseSold(-1);
            await _bookRepository.UpdateAsync(book1, true);
            Console.WriteLine($"Decrease 1, the new Book1 sold is: {book1.Sold}");
            await uow.CompleteAsync();
        }

        book1 = await _bookRepository.GetFromDatabaseAsync(Consts.Book1Id);
        Console.WriteLine($"Get Book1 from database, the sold is: {book1.Sold}");
        book1Cache = await _bookRepository.GetAsync(Consts.Book1Id);
        Console.WriteLine($"Get Book1 from grain, the sold is: {book1Cache.Sold}");

        using (var uow = _unitOfWorkManager.Begin(isTransactional: true))
        {
            book1 = await _bookRepository.GetAsync(Consts.Book1Id);
            Console.WriteLine($"Start to delete Book1");
            await _bookRepository.DeleteAsync(book1, true);
            await uow.CompleteAsync();
        }

        book1Cache = await _bookRepository.FindAsync(Consts.Book1Id);
        Console.WriteLine($"Find Book1 from grain, the result is: {(book1Cache?.ToString() ?? "entity not found")}");

        Console.WriteLine("----- Cached Entity Test End -----");
        Console.WriteLine();
    }

    private async Task ConcurrentTestAsync()
    {
        Console.WriteLine("----- Concurrent Test Begin -----");

        using (var uow = _unitOfWorkManager.Begin(isTransactional: true))
        {
            Console.WriteLine($"Start to create Book1");
            await _bookRepository.InsertAsync(new Book(Consts.Book1Id, "MyBook"), true);
            Console.WriteLine($"Book1 created");
            await uow.CompleteAsync();
        }

        using (var uow = _unitOfWorkManager.Begin(isTransactional: true))
        {
            Console.WriteLine($"[T1] Start to increase Sold of Book1");
            var book1 = await _bookRepository.GetAsync(Consts.Book1Id);
            Console.WriteLine($"[T1] Original Book1 sold is: {book1.Sold}");
            book1.IncreaseSold(2);
            await _bookRepository.UpdateAsync(book1, true);
        
            var task = Task.Run(async () =>
            {
                using var uow2 = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
                Console.WriteLine($"[T2] As it happens, someone else try to get Book1 cache before the commit");
                var book1Cache = await _bookRepository.GetAsync(Consts.Book1Id);
                Console.WriteLine($"[T2] Got Book1 cache, the sold is: {book1Cache.Sold}");
                await uow2.CompleteAsync();
            });
            
            Console.WriteLine($"[T1] Commit in 3s");
            await Task.Delay(TimeSpan.FromSeconds(1));
            Console.WriteLine($"[T1] Commit in 2s");
            await Task.Delay(TimeSpan.FromSeconds(1));
            Console.WriteLine($"[T1] Commit in 1s");
            await Task.Delay(TimeSpan.FromSeconds(1));
            await uow.CompleteAsync();
            Console.WriteLine($"[T1] Increased 2, the new Book1 sold is: {book1.Sold}");
        
            task.Wait();
        }

        var book11 = await _bookRepository.GetFromDatabaseAsync(Consts.Book1Id);
        Console.WriteLine($"!!!Got Book1 from database, the ConcurrencyStamp is: {book11.ConcurrencyStamp}");
        var book1Cache1 = await _bookRepository.GetAsync(Consts.Book1Id);
        Console.WriteLine($"!!!Got Book1 from grain, the ConcurrencyStamp is: {book1Cache1.ConcurrencyStamp}");

        using (var uow = _unitOfWorkManager.Begin(isTransactional: true))
        {
            Console.WriteLine($"[T1] Start to increase Sold of Book1");
            var book1 = await _bookRepository.GetAsync(Consts.Book1Id);
            Console.WriteLine($"[T1] Original Book1 sold is: {book1.Sold}");
            book1.IncreaseSold(2);
            await _bookRepository.UpdateAsync(book1, true);

            var task = Task.Run(async () =>
            {
                using var uow2 = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
                Console.WriteLine($"[T2] As it happens, someone else try to Get Book1 from grain before the commit");
                var book1Cache = await _bookRepository.GetAsync(Consts.Book1Id);
                Console.WriteLine($"[T2] Got Book1 from grain, the sold is: {book1Cache.Sold}");
                await uow2.CompleteAsync();
            });
            
            Console.WriteLine($"[T1] Commit in 3s");
            await Task.Delay(TimeSpan.FromSeconds(1));
            Console.WriteLine($"[T1] Commit in 2s");
            await Task.Delay(TimeSpan.FromSeconds(1));
            Console.WriteLine($"[T1] Commit in 1s");
            await Task.Delay(TimeSpan.FromSeconds(1));
            await uow.RollbackAsync();
            Console.WriteLine($"[T1] Something wrong! It rolled back!!!");

            task.Wait();
        }
        
        book11 = await _bookRepository.GetFromDatabaseAsync(Consts.Book1Id);
        Console.WriteLine($"!!!Got Book1 from database, the ConcurrencyStamp is: {book11.ConcurrencyStamp}");
        book1Cache1 = await _bookRepository.GetAsync(Consts.Book1Id);
        Console.WriteLine($"!!!Got Book1 from grain, the ConcurrencyStamp is: {book1Cache1.ConcurrencyStamp}");

        Console.WriteLine("----- Concurrent Test End -----");
        Console.WriteLine();
    }
}