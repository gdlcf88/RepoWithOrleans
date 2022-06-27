using Microsoft.Extensions.DependencyInjection;
using RepoWithOrleans.Repositories;
using Volo.Abp.Domain.Entities;

namespace Volo.Abp.DependencyInjection;

public static class AbpCommonDbContextRegistrationOptionsBuilderExtensions
{
    public static IAbpCommonDbContextRegistrationOptionsBuilder
        AddEfCoreCachedEntityRepository<TEntity, TKey, TRepository>(
            this IAbpCommonDbContextRegistrationOptionsBuilder builder)
        where TEntity : AggregateRoot<TKey>
        where TRepository : class, ICachedEntityRepository<TEntity, TKey>
    {
        builder.AddRepository<TEntity, TRepository>();
        builder.Services.AddTransient<ICachedEntityRepository<TEntity, TKey>, TRepository>();

        return builder;
    }
}