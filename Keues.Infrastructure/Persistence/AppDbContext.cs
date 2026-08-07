using Keues.Application.Common;
using Keues.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keues.Infrastructure.Persistence;

public class AppDbContext:DbContext,IApplicationDbContext
{
  public AppDbContext(DbContextOptions<AppDbContext> options)
    : base(options)
  {
  }
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Queue>()
      .HasQueryFilter(x => x.RemovedAt == null);
    modelBuilder.Entity<Location>()
      .HasQueryFilter(x => x.RemovedAt == null);
   
    modelBuilder.Entity<Counter>()
      .HasQueryFilter(x => x.RemovedAt == null);
    modelBuilder.Entity<Flow>()
      .HasQueryFilter(x=>x.RemovedAt==null);
   
  }
  
  public DbSet<Ticket> Tickets => Set<Ticket>();

  public DbSet<Queue> Queues => Set<Queue>();

  public DbSet<Counter> Counters => Set<Counter>();
  
  public DbSet<Location> Locations => Set<Location>();

  public DbSet<User> Users => Set<User>();
  public DbSet<Flow> Flows => Set<Flow>();
  
  public DbSet<Device> Devices => Set<Device>();
  
 
}