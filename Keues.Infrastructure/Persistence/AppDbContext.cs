using System.Text.Json;
using Keues.Application.Common;
using Keues.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Keues.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
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
      .HasQueryFilter(x => x.RemovedAt == null);

    modelBuilder.Entity<Ticket>()
      .HasQueryFilter(t => t.Queue.RemovedAt == null && t.Flow.RemovedAt == null);

    modelBuilder.Entity<User>()
      .HasQueryFilter(u => u.RemovedAt == null);

    modelBuilder.Entity<UserGroup>()
      .HasQueryFilter(ug => ug.RemovedAt == null);
    
    
    var webhooksConverter = new ValueConverter<List<string>, string>(
      v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new()
    );
    
    modelBuilder.Entity<WebhooksConfig>(entity =>
    {
      entity.Property(x => x.Url)
        .IsRequired();

      entity.Property(x => x.Events)
        .HasConversion(webhooksConverter)
        .IsRequired();
    });
  }

  public DbSet<Ticket> Tickets => Set<Ticket>();

  public DbSet<Queue> Queues => Set<Queue>();

  public DbSet<Counter> Counters => Set<Counter>();

  public DbSet<Location> Locations => Set<Location>();

  public DbSet<User> Users => Set<User>();
  public DbSet<Flow> Flows => Set<Flow>();

  public DbSet<Device> Devices => Set<Device>();

  public DbSet<UserGroup> UserGroups => Set<UserGroup>();

  public DbSet<TicketHistory> TicketHistories => Set<TicketHistory>();
  
  public DbSet<WebhooksConfig> WebhooksConfigs => Set<WebhooksConfig>();
}