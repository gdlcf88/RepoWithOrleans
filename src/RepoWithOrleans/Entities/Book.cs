using System;
using Volo.Abp.Domain.Entities;

namespace RepoWithOrleans.Entities;

public class Book : AggregateRoot<Guid>
{
    public virtual string Name { get; set; }
    
    public virtual int Sold { get; protected set; }

    protected Book()
    {
    }

    internal Book(Guid id, string name) : base(id)
    {
        Name = name;
    }

    internal void IncreaseSold(int number)
    {
        Sold += number;
    }
}