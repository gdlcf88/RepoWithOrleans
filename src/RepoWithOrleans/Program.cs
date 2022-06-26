using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using RepoWithOrleans.Data;
using RepoWithOrleans.Grains;
using Serilog;
using Serilog.Events;

namespace RepoWithOrleans;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Orleans", LogEventLevel.Warning)
            // .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .WriteTo.Async(c => c.Console())
            .CreateLogger();

        try
        {
            Log.Information("Starting console host.");

            using var host = new HostBuilder()
                .ConfigureDefaults(args)
                .ConfigureServices((hostContext, services) => { services.AddApplication<RepoWithOrleansModule>(); })
                .UseOrleans(builder =>
                {
                    builder.UseLocalhostClustering()
                        .AddMemoryGrainStorage(EntityCurrentStampGrain.StorageProviderName);
                })
                .UseAutofac()
                .UseSerilog()
                .Build();

            await host.InitializeApplicationAsync();

            await host.StartAsync();

            await host.Services.GetRequiredService<MyDbMigrationService>().MigrateAsync();

            var helloWorldService = host.Services.GetRequiredService<MyService>();

            await helloWorldService.RunAsync();

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}