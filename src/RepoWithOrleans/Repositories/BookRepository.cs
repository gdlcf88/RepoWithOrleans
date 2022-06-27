using RepoWithOrleans.Data;
using RepoWithOrleans.Entities;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;

namespace RepoWithOrleans.Repositories;

public class BookRepository : EfCoreWithOrleansGrainRepository<MyDbContext, Book>, IBookRepository, ITransientDependency
{
    public BookRepository(IDbContextProvider<MyDbContext> dbContextProvider) : base(dbContextProvider)
    {
    }
}