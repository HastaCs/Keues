using Keues.Application.Events;
using Keues.Application.Events.Handlers.Tickets;
using Keues.Domain.Events;
using Keues.Infrastructure.Persistence;
using Keues.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Keues.Tests.UseCases;

public class TicketEventPayloadBuilderTests : IDisposable
{
  private readonly TestDatabaseFactory _db = new();

  public void Dispose() => _db.Dispose();

  private static TicketEventPayloadBuilder Builder(AppDbContext context) => new(context);

  [Fact]
  public async Task Build_created_event_has_queue_location_and_null_counter_and_user()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context, "Oficina");
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id, code: "ENV", name: "Envíos");
    var ticket = await Seed.TicketAsync(context, queue.Id, flow.Id);

    var payload = await Builder(context).BuildAsync(
      EventTypes.Ticket.Created, DateTime.UtcNow, ticket.Id, null, null);

    Assert.Equal(EventTypes.Ticket.Created, payload.eventType);
    Assert.Equal(ticket.Id, payload.data.id);
    Assert.Equal(ticket.Code, payload.data.code);
    Assert.Equal("Envíos", payload.data.queue.name);
    Assert.Equal("Oficina", payload.data.location.name);
    Assert.Null(payload.data.counter);
    Assert.Null(payload.data.user);
  }

  [Fact]
  public async Task Build_uses_ticket_counter_when_present()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id);
    var counter = await Seed.CounterAsync(context, location.Id, name: "Caja 1");
    var ticket = await Seed.TicketAsync(context, queue.Id, flow.Id);
    ticket.CounterId = counter.Id;
    await context.SaveChangesAsync();

    var payload = await Builder(context).BuildAsync(
      EventTypes.Ticket.Called, DateTime.UtcNow, ticket.Id, null, null);

    Assert.NotNull(payload.data.counter);
    Assert.Equal(counter.Id, payload.data.counter!.id);
    Assert.Equal("Caja 1", payload.data.counter.name);
  }

  [Fact]
  public async Task Build_falls_back_to_event_counter_when_ticket_has_none()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id);
    var counter = await Seed.CounterAsync(context, location.Id, name: "Caja 9");
    var ticket = await Seed.TicketAsync(context, queue.Id, flow.Id);

    var payload = await Builder(context).BuildAsync(
      EventTypes.Ticket.Canceled, DateTime.UtcNow, ticket.Id, counter.Id, null);

    Assert.NotNull(payload.data.counter);
    Assert.Equal("Caja 9", payload.data.counter!.name);
  }

  [Fact]
  public async Task Build_resolves_user_name_when_id_is_provided()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id);
    var user = await Seed.UserAsync(context, location.Id, name: "Ana");
    var ticket = await Seed.TicketAsync(context, queue.Id, flow.Id);

    var payload = await Builder(context).BuildAsync(
      EventTypes.Ticket.Attended, DateTime.UtcNow, ticket.Id, null, user.Id);

    Assert.NotNull(payload.data.user);
    Assert.Equal(user.Id, payload.data.user!.id);
    Assert.Equal("Ana", payload.data.user.name);
  }

  [Fact]
  public async Task Build_without_counter_and_user_does_not_throw()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id);
    var ticket = await Seed.TicketAsync(context, queue.Id, flow.Id);

    var payload = await Builder(context).BuildAsync(
      EventTypes.Ticket.Transferred, DateTime.UtcNow, ticket.Id, null, null);

    Assert.Null(payload.data.counter);
    Assert.Null(payload.data.user);
  }

  [Fact]
  public async Task Build_unknown_ticket_returns_empty_payload_without_throwing()
  {
    await using var context = _db.CreateContext();

    var payload = await Builder(context).BuildAsync(
      EventTypes.Ticket.Created, DateTime.UtcNow, Guid.NewGuid(), null, null);

    Assert.Equal(string.Empty, payload.data.code);
    Assert.Null(payload.data.counter);
    Assert.Null(payload.data.user);
  }

  [Fact]
  public async Task TicketCalledHandler_sends_payload_with_counter_and_user()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id, name: "Envíos");
    var counter = await Seed.CounterAsync(context, location.Id, name: "Caja 1", queues: [queue]);
    var user = await Seed.UserAsync(context, location.Id, name: "Ana");
    var ticket = await Seed.TicketAsync(context, queue.Id, flow.Id);
    ticket.CounterId = counter.Id;
    await context.SaveChangesAsync();

    var webhook = new FakeWebhookSender();
    var handler = new TicketCalledHandler(
      NullLogger<TicketCalledHandler>.Instance,
      webhook,
      Builder(context));

    await handler.HandleAsync(new TicketCalled(ticket.Id, counter.Id, user.Id));

    var payload = Assert.IsType<TicketEventPayload>(Assert.Single(webhook.Sent));
    Assert.Equal(EventTypes.Ticket.Called, payload.eventType);
    Assert.Equal(ticket.Code, payload.data.code);
    Assert.Equal("Envíos", payload.data.queue.name);
    Assert.Equal("Caja 1", payload.data.counter!.name);
    Assert.Equal("Ana", payload.data.user!.name);
    Assert.Equal(location.Name, payload.data.location.name);
  }

  [Fact]
  public async Task TicketCanceledHandler_uses_event_counter_when_ticket_has_none()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id);
    var queue = await Seed.QueueAsync(context, location.Id);
    var counter = await Seed.CounterAsync(context, location.Id, name: "Caja 7");
    var ticket = await Seed.TicketAsync(context, queue.Id, flow.Id);

    var webhook = new FakeWebhookSender();
    var handler = new TicketCanceledHandler(
      NullLogger<TicketCanceledHandler>.Instance,
      webhook,
      Builder(context));

    await handler.HandleAsync(new TicketCanceled(ticket.Id, counter.Id));

    var payload = Assert.IsType<TicketEventPayload>(Assert.Single(webhook.Sent));
    Assert.Equal(EventTypes.Ticket.Canceled, payload.eventType);
    Assert.Equal("Caja 7", payload.data.counter!.name);
    Assert.Null(payload.data.user);
  }

  [Fact]
  public void TicketCanceled_user_is_null_by_default()
  {
    var keuesEvent = new TicketCanceled(Guid.NewGuid(), Guid.NewGuid());

    Assert.Null(keuesEvent.UserId);
    Assert.Equal(EventTypes.Ticket.Canceled, keuesEvent.EventType);
  }

  [Fact]
  public void TicketCalled_carries_the_user_when_provided()
  {
    var userid = Guid.NewGuid();
    var keuesEvent = new TicketCalled(Guid.NewGuid(), Guid.NewGuid(), userid);

    Assert.Equal(userid, keuesEvent.UserId);
  }
}
