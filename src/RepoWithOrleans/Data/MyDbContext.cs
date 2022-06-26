using Microsoft.EntityFrameworkCore;
using RepoWithOrleans.Entities;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace RepoWithOrleans.Data;

public class MyDbContext : AbpDbContext<MyDbContext>, IAbpEfCoreDbContext
{
    public DbSet<Book> Books { get; set; }

    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Book>(b =>
        {
            b.ToTable("AppBooks", (string)null);
            b.ConfigureByConvention();
        });
    }
}