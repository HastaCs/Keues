using Keues.Application.Common;
using Keues.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Counters.CreateCounter;

public class CreateCounterHandler(IApplicationDbContext _context)
{
  public async Task<CounterBaseResult> Handle(CreateCounterCommand command)
  {
    var counter = Counter.Create(command.Name, command.Description, command.Code, command.Color, command.LocationId);
    if (command.Queues != null)
    {
      var queues = await _context.Queues
        .Where(q => command.Queues.Contains(q.Id))
        .ToListAsync();
      foreach (var queue in queues)
      {
        counter.Queues.Add(queue);
      }
    }

    if (command.AuthorizedUsers != null)
    {
      var users = await _context.Users
        .Where(u => command.AuthorizedUsers.Contains(u.Id) && u.LocationId == command.LocationId)
        .ToListAsync();
      foreach (var user in users)
      {
        counter.AuthorizedUsers.Add(user);
      }
    }

    if (command.AuthorizedUserGroups != null)
    {
      var userGroups = await _context.UserGroups
        .Where(ug => command.AuthorizedUserGroups.Contains(ug.Id) && ug.LocationId == command.LocationId)
        .ToListAsync();
      foreach (var userGroup in userGroups)
      {
        counter.AuthorizedUserGroups.Add(userGroup);
      }
    }

    _context.Counters.Add(counter);
    await _context.SaveChangesAsync();
    var authorizedUsers = counter.AuthorizedUsers.Select(u => u.Id);
    var authorizedUserGroups = counter.AuthorizedUserGroups.Select(ug => ug.Id);

    return new CounterBaseResult(counter.Id, counter.Name, counter.Code, counter.Description, counter.Color,
      counter.Queues.Select(q => q.Id), counter.LocationId, counter.CreatedAt!.Value,
      authorizedUsers, authorizedUserGroups);
  }
}