using Keues.Application.Features.UserGroups.CreateUserGroup;
using Keues.Application.Features.UserGroups.DeleteUserGroup;
using Keues.Application.Features.UserGroups.GetAllUserGroups;
using Keues.Application.Features.UserGroups.GetUserGroup;
using Keues.Application.Features.UserGroups.UpdateUserGroup;
using Keues.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Keues.Tests.UseCases;

public class UserGroupUseCasesTests : IDisposable
{
  private readonly TestDatabaseFactory _db = new();

  public void Dispose() => _db.Dispose();

  [Fact]
  public async Task Create_creates_and_returns_the_group()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var handler = new CreateUserGroupHandler(context);

    var response = await handler.Handle(new CreateUserGroupCommand("Grupo A", "red", location.Id));

    Assert.NotEqual(Guid.Empty, response.Id);
    Assert.Equal("Grupo A", response.Name);
    Assert.Equal("red", response.Color);
    Assert.Equal(location.Id, response.LocationId);
    Assert.NotEqual(default, response.CreatedAt);

    var stored = await context.UserGroups.FindAsync(response.Id);
    Assert.NotNull(stored);
    Assert.Equal(location.Id, stored!.LocationId);
  }

  [Fact]
  public async Task Create_rejects_duplicated_name_in_the_same_location_ignoring_case()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var handler = new CreateUserGroupHandler(context);
    await handler.Handle(new CreateUserGroupCommand("Grupo A", "red", location.Id));

    var ex = await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new CreateUserGroupCommand("grupo a", "blue", location.Id)));

    Assert.Equal("User group with the same name already exists in this location", ex.Message);
  }

  [Fact]
  public async Task Create_allows_the_same_name_in_another_location()
  {
    await using var context = _db.CreateContext();
    var locationA = await Seed.LocationAsync(context, "Location A");
    var locationB = await Seed.LocationAsync(context, "Location B");
    var handler = new CreateUserGroupHandler(context);
    await handler.Handle(new CreateUserGroupCommand("Grupo", "red", locationA.Id));

    var response = await handler.Handle(new CreateUserGroupCommand("Grupo", "red", locationB.Id));

    Assert.NotEqual(Guid.Empty, response.Id);
    Assert.Equal(locationB.Id, response.LocationId);
  }

  [Fact]
  public async Task Get_returns_the_group()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var created = await new CreateUserGroupHandler(context).Handle(
      new CreateUserGroupCommand("Grupo", "green", location.Id));
    var handler = new GetUserGroupHandler(context);

    var response = await handler.Handle(new GetUserGroupQuery(created.Id));

    Assert.Equal(created.Id, response.Id);
    Assert.Equal("Grupo", response.Name);
    Assert.Equal("green", response.Color);
    Assert.Equal(location.Id, response.LocationId);
  }

  [Fact]
  public async Task Get_throws_when_the_group_does_not_exist()
  {
    await using var context = _db.CreateContext();
    var handler = new GetUserGroupHandler(context);

    await Assert.ThrowsAsync<Exception>(() => handler.Handle(new GetUserGroupQuery(Guid.NewGuid())));
  }

  [Fact]
  public async Task GetAll_returns_every_group_and_can_filter_by_location()
  {
    await using var context = _db.CreateContext();
    var locationA = await Seed.LocationAsync(context, "Location A");
    var locationB = await Seed.LocationAsync(context, "Location B");
    var create = new CreateUserGroupHandler(context);
    await create.Handle(new CreateUserGroupCommand("A1", "red", locationA.Id));
    await create.Handle(new CreateUserGroupCommand("B1", "blue", locationB.Id));
    var handler = new GetAllUserGroupsHandler(context);

    var all = await handler.Handle(new GetAllUserGroupsQuery(null));
    Assert.Equal(2, all.UserGroups.Count());

    var onlyA = await handler.Handle(new GetAllUserGroupsQuery(locationA.Id));
    var single = Assert.Single(onlyA.UserGroups);
    Assert.Equal("A1", single.Name);
  }

  [Fact]
  public async Task Update_updates_the_group()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var created = await new CreateUserGroupHandler(context).Handle(
      new CreateUserGroupCommand("Grupo", "red", location.Id));
    var handler = new UpdateUserGroupHandler(context);

    var response = await handler.Handle(
      new UpdateUserGroupCommand(created.Id, "Renombrado", "orange", location.Id));

    Assert.Equal("Renombrado", response.Name);
    Assert.Equal("orange", response.Color);
    Assert.Equal(location.Id, response.LocationId);
  }

  [Fact]
  public async Task Update_throws_when_the_group_does_not_exist()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var handler = new UpdateUserGroupHandler(context);

    await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new UpdateUserGroupCommand(Guid.NewGuid(), "Nuevo", "red", location.Id)));
  }

  [Fact]
  public async Task Delete_soft_deletes_the_group()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var created = await new CreateUserGroupHandler(context).Handle(
      new CreateUserGroupCommand("Grupo", "red", location.Id));
    var handler = new DeleteUserGroupHandler(context);

    await handler.Handle(new DeleteUserGroupCommand(created.Id));

    var all = await new GetAllUserGroupsHandler(context).Handle(new GetAllUserGroupsQuery(null));
    Assert.Empty(all.UserGroups);

    var raw = await context.UserGroups.IgnoreQueryFilters().ToListAsync();
    var stored = Assert.Single(raw);
    Assert.NotNull(stored.RemovedAt);
  }

  [Fact]
  public async Task Delete_throws_when_the_group_does_not_exist()
  {
    await using var context = _db.CreateContext();
    var handler = new DeleteUserGroupHandler(context);

    await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new DeleteUserGroupCommand(Guid.NewGuid())));
  }
}
