using System.Threading.Tasks;
using Orleans;
using RepoWithOrleans.Entities;

namespace RepoWithOrleans.Grains;

public interface IBookGrain : IGrainWithGuidKey
{
    Task<Book> GetEntityOrNullAsync();

    Task StartUpdateAsync();

    Task FinishUpdateAsync();
}