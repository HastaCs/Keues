using Keues.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Keues.Application.Common;

public interface IApplicationDbContext
{
  DbSet<Ticket> Tickets { get; }

  DbSet<Queue> Queues { get; }

  DbSet<Counter> Counters { get; }
  
  DbSet<Location> Locations { get; }
  
  DbSet<Flow> Flows { get; }
  
  DbSet<User> Users { get; }
  
  DbSet<Device> Devices { get; }
  
  DatabaseFacade Database { get; }
  
  Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}