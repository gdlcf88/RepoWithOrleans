using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp;

namespace RepoWithOrleans;

public static class AbpHostExtensions
{
    public static async Task<IHost> InitializeApplicationAsync([NotNull] this IHost host)//InitializeHost?
    {
        Check.NotNull(host, nameof(host));

        var application = host.Services.GetRequiredService<IAbpApplicationWithExternalServiceProvider>();
        var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

        applicationLifetime.ApplicationStopping.Register(() =>
        {
            application.Shutdown();
        });

        applicationLifetime.ApplicationStopped.Register(() =>
        {
            application.Dispose();
        });

        await application.InitializeAsync(host.Services);

        return host;
    }
}