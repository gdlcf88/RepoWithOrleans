using System.Threading.Tasks;
using JetBrains.Annotations;
using Orleans;

namespace RepoWithOrleans.Grains;

public interface IEntityCurrentStampGrain : IGrainWithStringKey
{
    Task<string> GetCurrentStampAsync();
    
    Task SetCurrentStampAsync([CanBeNull] string currentStamp);
}