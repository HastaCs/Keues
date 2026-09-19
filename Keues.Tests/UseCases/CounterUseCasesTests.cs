using Keues.Application.Features.Counters;
using Keues.Application.Features.Counters.AttendTicket;
using Keues.Application.Features.Counters.CallNextTicket;
using Keues.Application.Features.Counters.CancelTicket;
using Keues.Application.Features.Counters.CreateCounter;
using Keues.Application.Features.Counters.DeleteCounter;
using Keues.Application.Features.Counters.GetAllCounters;
using Keues.Application.Features.Counters.GetCounter;
using Keues.Application.Features.Counters.UpdateCounter;
using Keues.Application.Features.UserGroups.DeleteUserGroup;
using Keues.Domain.Enums;
using Keues.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Keues.Tests.UseCases;

public class CounterUseCasesTests : IDisposable
{
  private readonly TestDatabaseFactory _db = new();

  public void Dispose() => _db.Dispose();

  [Fact]
  public async Task Create_persists_and_links_the_queues()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var q1 = await Seed.QueueAsync(context, location.Id, code: "A");
    var q2 = await Seed.QueueAsync(context, location.Id, code: "B");
    var handler = new CreateCounterHandler(context);

    var response = await handler.Handle(new CreateCounterCommand
    {
      Name = "Caja 1",
      Code = "C1",
      Color = "green",
      Description = "Caja principal",
      LocationId = location.Id,
      Queues = [q1.Id, q2.Id]
    });

    Assert.NotEqual(Guid.Empty, response.Id);
    Assert.Equal("Caja 1", response.Name);
    Assert.Equal(2, response.Queues.Count());
    Assert.Contains(q1.Id, response.Queues);
    Assert.Contains(q2.Id, response.Queues);
  }

  [Fact]
  public async Task Get_throws_when_not_found()
  {
    await using var context = _db.CreateContext();
    var handler = new GetCounterHandler(context);

    await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new GetCounterCommand(Guid.NewGuid())));
  }

  [Fact]
  public async Task GetAll_filters_by_location()
  {
    await using var context = _db.CreateContext();
    var locA = await Seed.LocationAsync(context, "A");
    var locB = await Seed.LocationAsync(context, "B");
    await Seed.CounterAsync(context, locA.Id, code: "A1");
    await Seed.CounterAsync(context, locA.Id, code: "A2");
    await Seed.CounterAsync(context, locB.Id, code: "B1");
    var handler = new GetAllCountersHandler(context);

    var onlyA = await handler.Handle(new GetAllCountersCommand { LocationId = locA.Id });

    Assert.Equal(2, onlyA.Count());
  }

  [Fact]
  public async Task Update_replaces_the_linked_queues()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var q1 = await Seed.QueueAsync(context, location.Id, code: "A");
    var q2 = await Seed.QueueAsync(context, location.Id, code: "B");
    var counter = await Seed.CounterAsync(context, location.Id, queues: [q1]);
    var handler = new UpdateCounterHandler(context);

    var response = await handler.Handle(new UpdateCounterCommand
    {
      Id = counter.Id,
      Name = "Caja nueva",
      Code = "C2",
      Color = "yellow",
      Description = "Caja 2",
      LocationId = location.Id,
      Queues = [q2.Id]
    });

    Assert.Equal("Caja nueva", response.Name);
    Assert.Equal([q2.Id], response.Queues);
  }

  [Fact]
  public async Task Delete_soft_deletes_the_counter()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var counter = await Seed.CounterAsync(context, location.Id);
    var handler = new DeleteCounterHandler(context);

    await handler.Handle(new DeleteCounterCommand(counter.Id));

    var getAll = await new GetAllCountersHandler(context).Handle(new GetAllCountersCommand());
    Assert.Empty(getAll);
  }

  [Fact]
  public async Task CallNextTicket_returns_null_when_there_are_no_waiting_tickets()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var queue = await Seed.QueueAsync(context, location.Id);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
    var handler = new CallNextTicketHandler(context);

    var result = await handler.Handle(new CallNextTicketCommand(counter.Id));

    Assert.Null(result);
  }

  [Fact]
  public async Task CallNextTicket_calls_the_oldest_waiting_ticket()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id, code: "P");
    var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
    var t1 = await Seed.TicketAsync(context, queue.Id, flow.Id);
    var t2 = await Seed.TicketAsync(context, queue.Id, flow.Id);
    var handler = new CallNextTicketHandler(context);

    var result = await handler.Handle(new CallNextTicketCommand(counter.Id));

    Assert.NotNull(result);
    Assert.Equal(t1.Id, result.TicketId);
    Assert.Equal("P001", result.Code);

    var ticket = await context.Tickets.FindAsync(t1.Id);
    Assert.Equal(TicketStatus.InProgress, ticket!.Status);
    Assert.Equal(counter.Id, ticket.CounterId);
    Assert.NotNull(ticket.CalledAt);
  }

  [Fact]
  public async Task CallNextTicket_recalls_the_current_in_progress_ticket()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
    await Seed.TicketAsync(context, queue.Id, flow.Id);
    var handler = new CallNextTicketHandler(context);

    var first = await handler.Handle(new CallNextTicketCommand(counter.Id));
    var second = await handler.Handle(new CallNextTicketCommand(counter.Id));

    Assert.Equal(first!.TicketId, second!.TicketId);
  }

  [Fact]
  public async Task CallNextTicket_prefers_the_highest_priority_queue()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var high = await Seed.QueueAsync(context, location.Id, code: "HIGH", priority: 10);
    var low = await Seed.QueueAsync(context, location.Id, code: "LOW", priority: 0);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [high, low]);
    await Seed.TicketAsync(context, high.Id, flow.Id);
    await Seed.TicketAsync(context, low.Id, flow.Id);
    var handler = new CallNextTicketHandler(context);

    var result = await handler.Handle(new CallNextTicketCommand(counter.Id));

    Assert.NotNull(result);
    Assert.Equal(high.Id, result.QueueId);
    Assert.StartsWith("HIGH", result.Code);
  }

  [Fact]
  public async Task CallNextTicket_applies_aging_bonus_to_old_tickets()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    // Cola A: prioridad 0 pero con aging. Cada 10 minutos sube 1 de prioridad (máx 5).
    var aging = await Seed.QueueAsync(context, location.Id, code: "OLD", priority: 0,
      agingIntervalMinutes: 10, maxAgingBonus: 5);
    // Cola B: prioridad 3, sin aging.
    var fresh = await Seed.QueueAsync(context, location.Id, code: "FRESH", priority: 3);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [aging, fresh]);

    // El ticket de la cola A lleva 60 minutos esperando -> bonus = min(6, 5) = 5 -> prioridad efectiva 5 > 3.
    await Seed.TicketAsync(context, aging.Id, flow.Id,
      createdAt: DateTime.UtcNow.AddMinutes(-60));
    await Seed.TicketAsync(context, fresh.Id, flow.Id);
    var handler = new CallNextTicketHandler(context);

    var result = await handler.Handle(new CallNextTicketCommand(counter.Id));

    Assert.NotNull(result);
    Assert.Equal(aging.Id, result.QueueId);
    Assert.StartsWith("OLD", result.Code);
  }

  [Fact]
  public async Task CallNextTicket_distributes_equally_by_weight()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var qA = await Seed.QueueAsync(context, location.Id, code: "A", priority: 5, weight: 1);
    var qB = await Seed.QueueAsync(context, location.Id, code: "B", priority: 5, weight: 1);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [qA, qB]);
    for (var i = 0; i < 20; i++)
    {
      await Seed.TicketAsync(context, qA.Id, flow.Id);
      await Seed.TicketAsync(context, qB.Id, flow.Id);
    }

    var callHandler = new CallNextTicketHandler(context);
    var attendHandler = new AttendTicketHandler(context);

    var calledQueues = new HashSet<Guid>();
    for (var i = 0; i < 30; i++)
    {
      var result = await callHandler.Handle(new CallNextTicketCommand(counter.Id));
      Assert.NotNull(result);
      calledQueues.Add(result.QueueId);
      await attendHandler.Handle(new AttendTicketCommand(counter.Id, result.TicketId));
    }

    Assert.Contains(qA.Id, calledQueues);
    Assert.Contains(qB.Id, calledQueues);
  }

  [Fact]
  public async Task CallNextTicket_throws_when_counter_not_found()
  {
    await using var context = _db.CreateContext();
    var handler = new CallNextTicketHandler(context);

    var ex = await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new CallNextTicketCommand(Guid.NewGuid())));

    Assert.Equal("Counter not found", ex.Message);
  }

  [Fact]
  public async Task AttendTicket_marks_the_ticket_as_attended()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
    var ticket = await Seed.TicketAsync(context, queue.Id, flow.Id);
    var handler = new AttendTicketHandler(context);

    await handler.Handle(new AttendTicketCommand(counter.Id, ticket.Id));

    var reloaded = await context.Tickets.FindAsync(ticket.Id);
    Assert.Equal(TicketStatus.Attended, reloaded!.Status);
    Assert.NotNull(reloaded.AttendedAt);
  }

  [Theory]
  [InlineData(true, false)]
  [InlineData(false, true)]
  public async Task AttendTicket_throws_when_counter_or_ticket_not_found(bool missingCounter, bool missingTicket)
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id);
    var counter = await Seed.CounterAsync(context, location.Id, queues: [queue]);
    var ticket = await Seed.TicketAsync(context, queue.Id, flow.Id);
    var handler = new AttendTicketHandler(context);

    var counterId = missingCounter ? Guid.NewGuid() : counter.Id;
    var ticketId = missingTicket ? Guid.NewGuid() : ticket.Id;

    await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new AttendTicketCommand(counterId, ticketId)));
  }

  [Fact]
  public async Task Create_persists_authorized_users_and_groups()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var ana = await Seed.UserAsync(context, location.Id, name: "Ana", email: "ana@keues.dev");
    var luis = await Seed.UserAsync(context, location.Id, name: "Luis", email: "luis@keues.dev");
    var reception = await Seed.UserGroupAsync(context, location.Id, name: "Recepcion");
    var fruit = await Seed.UserGroupAsync(context, location.Id, name: "Fruteria");
    var handler = new CreateCounterHandler(context);

    var response = await handler.Handle(new CreateCounterCommand
    {
      Name = "Caja",
      Code = "C1",
      Color = "green",
      Description = "",
      LocationId = location.Id,
      AuthorizedUsers = [ana.Id, luis.Id],
      AuthorizedUserGroups = [reception.Id, fruit.Id]
    });

    Assert.Equal(2, response.AuthorizedUsers.Count());
    Assert.Contains(ana.Id, response.AuthorizedUsers);
    Assert.Contains(luis.Id, response.AuthorizedUsers);
    Assert.Equal(2, response.AuthorizedUserGroups.Count());
    Assert.Contains(reception.Id, response.AuthorizedUserGroups);
    Assert.Contains(fruit.Id, response.AuthorizedUserGroups);

    var stored = await context.Counters
      .Include(c => c.AuthorizedUsers)
      .Include(c => c.AuthorizedUserGroups)
      .FirstAsync(c => c.Id == response.Id);
    Assert.Equal(2, stored.AuthorizedUsers.Count);
    Assert.Equal(2, stored.AuthorizedUserGroups.Count);
  }

  [Fact]
  public async Task Create_ignores_authorized_users_and_groups_from_another_location()
  {
    await using var context = _db.CreateContext();
    var locationA = await Seed.LocationAsync(context, "A");
    var locationB = await Seed.LocationAsync(context, "B");
    var foreignUser = await Seed.UserAsync(context, locationB.Id, name: "Ajeno", email: "ajeno@keues.dev");
    var foreignGroup = await Seed.UserGroupAsync(context, locationB.Id, name: "Grupo ajeno");
    var handler = new CreateCounterHandler(context);

    var response = await handler.Handle(new CreateCounterCommand
    {
      Name = "Caja",
      Code = "C1",
      Color = "green",
      Description = "",
      LocationId = locationA.Id,
      AuthorizedUsers = [foreignUser.Id],
      AuthorizedUserGroups = [foreignGroup.Id]
    });

    Assert.Empty(response.AuthorizedUsers);
    Assert.Empty(response.AuthorizedUserGroups);
  }

  [Fact]
  public async Task GetAll_returns_authorized_users_and_groups()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var ana = await Seed.UserAsync(context, location.Id, name: "Ana", email: "ana@keues.dev");
    var group = await Seed.UserGroupAsync(context, location.Id, name: "Recepcion");
    await new CreateCounterHandler(context).Handle(new CreateCounterCommand
    {
      Name = "Caja",
      Code = "C1",
      Color = "green",
      Description = "",
      LocationId = location.Id,
      AuthorizedUsers = [ana.Id],
      AuthorizedUserGroups = [group.Id]
    });

    var response = await new GetAllCountersHandler(context)
      .Handle(new GetAllCountersCommand { LocationId = location.Id });

    var counter = Assert.Single(response);
    Assert.Contains(ana.Id, counter.AuthorizedUsers);
    Assert.Contains(group.Id, counter.AuthorizedUserGroups);
  }

  [Fact]
  public async Task Update_replaces_authorized_users_and_groups()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var ana = await Seed.UserAsync(context, location.Id, name: "Ana", email: "ana@keues.dev");
    var luis = await Seed.UserAsync(context, location.Id, name: "Luis", email: "luis@keues.dev");
    var reception = await Seed.UserGroupAsync(context, location.Id, name: "Recepcion");
    var fruit = await Seed.UserGroupAsync(context, location.Id, name: "Fruteria");
    var created = await new CreateCounterHandler(context).Handle(new CreateCounterCommand
    {
      Name = "Caja",
      Code = "C1",
      Color = "green",
      Description = "",
      LocationId = location.Id,
      AuthorizedUsers = [ana.Id],
      AuthorizedUserGroups = [reception.Id]
    });
    var handler = new UpdateCounterHandler(context);

    var response = await handler.Handle(new UpdateCounterCommand
    {
      Id = created.Id,
      Name = "Caja",
      Code = "C1",
      Color = "green",
      Description = "",
      LocationId = location.Id,
      AuthorizedUsers = [luis.Id],
      AuthorizedUserGroups = [fruit.Id]
    });

    Assert.Equal([luis.Id], response.AuthorizedUsers);
    Assert.Equal([fruit.Id], response.AuthorizedUserGroups);
  }

  [Fact]
  public async Task Update_in_a_fresh_context_keeps_existing_and_adds_new_authorized_items()
  {
    Guid counterId;
    Guid locationId;
    Guid anaId;
    Guid luisId;
    Guid receptionId;
    Guid fruitId;

    await using (var context = _db.CreateContext())
    {
      var location = await Seed.LocationAsync(context);
      var ana = await Seed.UserAsync(context, location.Id, name: "Ana", email: "ana@keues.dev");
      var luis = await Seed.UserAsync(context, location.Id, name: "Luis", email: "luis@keues.dev");
      var reception = await Seed.UserGroupAsync(context, location.Id, name: "Recepcion");
      var fruit = await Seed.UserGroupAsync(context, location.Id, name: "Fruteria");
      var created = await new CreateCounterHandler(context).Handle(new CreateCounterCommand
      {
        Name = "Caja",
        Code = "C1",
        Color = "green",
        Description = "",
        LocationId = location.Id,
        AuthorizedUsers = [ana.Id],
        AuthorizedUserGroups = [reception.Id]
      });

      counterId = created.Id;
      locationId = location.Id;
      anaId = ana.Id;
      luisId = luis.Id;
      receptionId = reception.Id;
      fruitId = fruit.Id;
    }

    await using (var freshContext = _db.CreateContext())
    {
      var handler = new UpdateCounterHandler(freshContext);

      var response = await handler.Handle(new UpdateCounterCommand
      {
        Id = counterId,
        Name = "Caja",
        Code = "C1",
        Color = "green",
        Description = "",
        LocationId = locationId,
        AuthorizedUsers = [anaId, luisId],
        AuthorizedUserGroups = [receptionId, fruitId]
      });

      Assert.Equal(2, response.AuthorizedUsers.Count());
      Assert.Contains(anaId, response.AuthorizedUsers);
      Assert.Contains(luisId, response.AuthorizedUsers);
      Assert.Equal(2, response.AuthorizedUserGroups.Count());
      Assert.Contains(receptionId, response.AuthorizedUserGroups);
      Assert.Contains(fruitId, response.AuthorizedUserGroups);
    }
  }

  [Fact]
  public async Task Update_with_empty_lists_clears_authorized_items()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var ana = await Seed.UserAsync(context, location.Id, name: "Ana", email: "ana@keues.dev");
    var reception = await Seed.UserGroupAsync(context, location.Id, name: "Recepcion");
    var created = await new CreateCounterHandler(context).Handle(new CreateCounterCommand
    {
      Name = "Caja",
      Code = "C1",
      Color = "green",
      Description = "",
      LocationId = location.Id,
      AuthorizedUsers = [ana.Id],
      AuthorizedUserGroups = [reception.Id]
    });
    var handler = new UpdateCounterHandler(context);

    var response = await handler.Handle(new UpdateCounterCommand
    {
      Id = created.Id,
      Name = "Caja",
      Code = "C1",
      Color = "green",
      Description = "",
      LocationId = location.Id,
      AuthorizedUsers = [],
      AuthorizedUserGroups = []
    });

    Assert.Empty(response.AuthorizedUsers);
    Assert.Empty(response.AuthorizedUserGroups);
  }

  [Fact]
  public async Task Update_with_null_leaves_authorized_items_unchanged()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var ana = await Seed.UserAsync(context, location.Id, name: "Ana", email: "ana@keues.dev");
    var reception = await Seed.UserGroupAsync(context, location.Id, name: "Recepcion");
    var created = await new CreateCounterHandler(context).Handle(new CreateCounterCommand
    {
      Name = "Caja",
      Code = "C1",
      Color = "green",
      Description = "",
      LocationId = location.Id,
      AuthorizedUsers = [ana.Id],
      AuthorizedUserGroups = [reception.Id]
    });
    var handler = new UpdateCounterHandler(context);

    var response = await handler.Handle(new UpdateCounterCommand
    {
      Id = created.Id,
      Name = "Caja renombrada",
      Code = "C1",
      Color = "green",
      Description = "",
      LocationId = location.Id,
      AuthorizedUsers = null,
      AuthorizedUserGroups = null
    });

    Assert.Equal("Caja renombrada", response.Name);
    Assert.Contains(ana.Id, response.AuthorizedUsers);
    Assert.Contains(reception.Id, response.AuthorizedUserGroups);
  }

  [Fact]
  public async Task Get_excludes_a_soft_deleted_authorized_group()
  {
    Guid counterId;
    Guid groupId;

    await using (var context = _db.CreateContext())
    {
      var location = await Seed.LocationAsync(context);
      var group = await Seed.UserGroupAsync(context, location.Id, name: "Temporal");
      var created = await new CreateCounterHandler(context).Handle(new CreateCounterCommand
      {
        Name = "Caja",
        Code = "C1",
        Color = "green",
        Description = "",
        LocationId = location.Id,
        AuthorizedUserGroups = [group.Id]
      });
      Assert.Single(created.AuthorizedUserGroups);

      counterId = created.Id;
      groupId = group.Id;
    }

    await using (var context = _db.CreateContext())
    {
      await new DeleteUserGroupHandler(context).Handle(new DeleteUserGroupCommand(groupId));
    }

    await using (var context = _db.CreateContext())
    {
      var response = await new GetCounterHandler(context).Handle(new GetCounterCommand(counterId));

      Assert.Empty(response.AuthorizedUserGroups);
    }
  }

  [Fact]
  public async Task Get_excludes_a_soft_deleted_authorized_user()
  {
    Guid counterId;
    Guid userId;

    await using (var context = _db.CreateContext())
    {
      var location = await Seed.LocationAsync(context);
      var user = await Seed.UserAsync(context, location.Id, name: "Ana", email: "ana@keues.dev");
      var created = await new CreateCounterHandler(context).Handle(new CreateCounterCommand
      {
        Name = "Caja",
        Code = "C1",
        Color = "green",
        Description = "",
        LocationId = location.Id,
        AuthorizedUsers = [user.Id]
      });
      Assert.Single(created.AuthorizedUsers);

      counterId = created.Id;
      userId = user.Id;
    }

    await using (var context = _db.CreateContext())
    {
      var user = await context.Users.FirstAsync(u => u.Id == userId);
      user.RemovedAt = DateTime.UtcNow;
      await context.SaveChangesAsync();
    }

    await using (var context = _db.CreateContext())
    {
      var response = await new GetCounterHandler(context).Handle(new GetCounterCommand(counterId));

      Assert.Empty(response.AuthorizedUsers);
    }
  }

  


}
