using System.Threading.Tasks;
using RepoWithOrleans.Repositories;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;

namespace RepoWithOrleans.Data;

public class MyDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IBookRepository _bookRepository;

    public MyDataSeedContributor(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        var book = await _bookRepository.FindAsync(Consts.Book1Id);

        if (book is null)
        {
            return;
        }

        await _bookRepository.DeleteAsync(book, true);
    }
}