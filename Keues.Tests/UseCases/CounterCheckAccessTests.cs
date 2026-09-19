using Keues.Application.Features.Counters.CheckAccess;
using Keues.Tests.Infrastructure;
using Xunit;

namespace Keues.Tests.UseCases;

public class CounterCheckAccessTests : IDisposable
{
  private readonly TestDatabaseFactory _db = new();

  public void Dispose() => _db.Dispose();

  [Fact]
  public async Task Allow_everyone_when_the_counter_has_no_restrictions()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var counter = await Seed.CounterAsync(context, location.Id);
    var handler = new CheckAccessHandler(context);

    var anonymous = await handler.Handle(new CheckAccessQuery(counter.Id, null));
    var someone = await handler.Handle(new CheckAccessQuery(counter.Id, Guid.NewGuid()));

    Assert.True(anonymous);
    Assert.True(someone);
  }

  [Fact]
  public async Task Deny_anonymous_when_the_counter_has_restrictions()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var ana = await Seed.UserAsync(context, location.Id, name: "Ana", email: "ana@keues.dev");
    var counter = await Seed.CounterAsync(context, location.Id);
    counter.AuthorizedUsers.Add(ana);
    await context.SaveChangesAsync();
    var handler = new CheckAccessHandler(context);

    var result = await handler.Handle(new CheckAccessQuery(counter.Id, null));

    Assert.False(result);
  }

  [Fact]
  public async Task Allow_a_directly_authorized_user()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var ana = await Seed.UserAsync(context, location.Id, name: "Ana", email: "ana@keues.dev");
    var counter = await Seed.CounterAsync(context, location.Id);
    counter.AuthorizedUsers.Add(ana);
    await context.SaveChangesAsync();
    var handler = new CheckAccessHandler(context);

    var result = await handler.Handle(new CheckAccessQuery(counter.Id, ana.Id));

    Assert.True(result);
  }

  [Fact]
  public async Task Allow_a_user_authorized_through_a_group()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var ana = await Seed.UserAsync(context, location.Id, name: "Ana", email: "ana@keues.dev");
    var group = await Seed.UserGroupAsync(context, location.Id, name: "Recepcion");
    group.Users.Add(ana);
    var counter = await Seed.CounterAsync(context, location.Id);
    counter.AuthorizedUserGroups.Add(group);
    await context.SaveChangesAsync();
    var handler = new CheckAccessHandler(context);

    var result = await handler.Handle(new CheckAccessQuery(counter.Id, ana.Id));

    Assert.True(result);
  }

  [Fact]
  public async Task Deny_a_user_who_is_neither_authorized_nor_in_an_authorized_group()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var ana = await Seed.UserAsync(context, location.Id, name: "Ana", email: "ana@keues.dev");
    var luis = await Seed.UserAsync(context, location.Id, name: "Luis", email: "luis@keues.dev");
    var group = await Seed.UserGroupAsync(context, location.Id, name: "Recepcion");
    group.Users.Add(ana);
    var counter = await Seed.CounterAsync(context, location.Id);
    counter.AuthorizedUsers.Add(ana);
    counter.AuthorizedUserGroups.Add(group);
    await context.SaveChangesAsync();
    var handler = new CheckAccessHandler(context);

    var result = await handler.Handle(new CheckAccessQuery(counter.Id, luis.Id));

    Assert.False(result);
  }

  [Fact]
  public async Task Throw_when_the_counter_does_not_exist()
  {
    await using var context = _db.CreateContext();
    var handler = new CheckAccessHandler(context);

    var ex = await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new CheckAccessQuery(Guid.NewGuid(), Guid.NewGuid())));

    Assert.Contains("not found", ex.Message);
  }

  [Fact]
  public async Task Deny_membership_through_a_soft_deleted_group()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var ana = await Seed.UserAsync(context, location.Id, name: "Ana", email: "ana@keues.dev");

    var removed = await Seed.UserGroupAsync(context, location.Id, name: "Antiguo");
    removed.Users.Add(ana);
    var active = await Seed.UserGroupAsync(context, location.Id, name: "Actual");

    var counter = await Seed.CounterAsync(context, location.Id);
    counter.AuthorizedUserGroups.Add(removed);
    counter.AuthorizedUserGroups.Add(active);
    await context.SaveChangesAsync();

    removed.RemovedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    var handler = new CheckAccessHandler(context);

    var result = await handler.Handle(new CheckAccessQuery(counter.Id, ana.Id));

    Assert.False(result);
  }

  [Fact]
  public async Task Deny_a_soft_deleted_user_even_if_they_are_in_an_authorized_group()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var ana = await Seed.UserAsync(context, location.Id, name: "Ana", email: "ana@keues.dev");
    var group = await Seed.UserGroupAsync(context, location.Id, name: "Recepcion");
    group.Users.Add(ana);
    var counter = await Seed.CounterAsync(context, location.Id);
    counter.AuthorizedUserGroups.Add(group);
    await context.SaveChangesAsync();

    ana.RemovedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    var handler = new CheckAccessHandler(context);

    var result = await handler.Handle(new CheckAccessQuery(counter.Id, ana.Id));

    Assert.False(result);
  }

  [Fact]
  public async Task Open_the_counter_to_everyone_when_its_only_restriction_is_removed()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var ana = await Seed.UserAsync(context, location.Id, name: "Ana", email: "ana@keues.dev");
    var group = await Seed.UserGroupAsync(context, location.Id, name: "Recepcion");
    group.Users.Add(ana);
    var counter = await Seed.CounterAsync(context, location.Id);
    counter.AuthorizedUserGroups.Add(group);
    await context.SaveChangesAsync();

    group.RemovedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    var handler = new CheckAccessHandler(context);

    var anonymous = await handler.Handle(new CheckAccessQuery(counter.Id, null));

    Assert.True(anonymous);
  }
}
