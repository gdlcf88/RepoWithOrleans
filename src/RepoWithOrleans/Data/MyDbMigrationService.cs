using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;

namespace RepoWithOrleans.Data
{
    public class MyDbMigrationService : ITransientDependency
    {
        public ILogger<MyDbMigrationService> Logger { get; set; }

        private readonly IDataSeeder _dataSeeder;
        private readonly IServiceProvider _serviceProvider;

        public MyDbMigrationService(
            IDataSeeder dataSeeder,
            IServiceProvider serviceProvider)
        {
            _dataSeeder = dataSeeder;
            _serviceProvider = serviceProvider;

            Logger = NullLogger<MyDbMigrationService>.Instance;
        }

        public async Task MigrateAsync()
        {
            Logger.LogInformation("Started database migrations...");

            Logger.LogInformation("Migrating database schema...");
            await _serviceProvider
                .GetRequiredService<MyDbContext>()
                .Database
                .MigrateAsync();

            Logger.LogInformation("Executing database seed...");
            await _dataSeeder.SeedAsync();

            Logger.LogInformation("Successfully completed database migrations.");
        }
    }
}