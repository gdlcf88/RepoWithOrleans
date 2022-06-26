using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;

namespace RepoWithOrleans.Grains;

[StorageProvider(ProviderName = StorageProviderName)]
public class EntityCurrentStampGrain : Grain<EntityCurrentStampState>, IEntityCurrentStampGrain
{
    public const string StorageProviderName = "EntityCurrentStampStorage";

    public Task<string> GetCurrentStampAsync()
    {
        return Task.FromResult(State.CurrentStamp);
    }

    public async Task SetCurrentStampAsync(string currentStamp)
    {
        State.CurrentStamp = currentStamp;
        
        await WriteStateAsync();
    }
}