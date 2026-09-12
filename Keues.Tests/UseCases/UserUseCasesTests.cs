using Keues.Application.Common;
using Keues.Application.Features.Users.CreateAdmin;
using Keues.Application.Features.Users.CreateUser;
using Keues.Application.Features.Users.EnableDisableUser;
using Keues.Application.Features.Users.ForgotPassword;
using Keues.Application.Features.Users.GetAllUsers;
using Keues.Application.Features.Users.GetUser;
using Keues.Application.Features.Users.HasAdmin;
using Keues.Application.Features.Users.Login;
using Keues.Application.Features.Users.Me;
using Keues.Application.Features.Users.ResetPassword;
using Keues.Application.Features.Users.UpdateUser;
using Keues.Domain.Enums;
using Keues.Infrastructure.Authorization;
using Keues.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keues.Tests.UseCases;

public class UserUseCasesTests : IDisposable
{
  private readonly TestDatabaseFactory _db = new();

  public void Dispose() => _db.Dispose();

  private static JwtService CreateJwtService() => new(Options.Create(new JwtOptions
  {
    Key = TestWebApplicationFactory.JwtKey,
    Issuer = "keues",
    Audience = "keues",
    ExpirationInMinutes = 60
  }));

  private static IOptions<PasswordResetOptions> ResetOptions(string frontendUrl = "http://localhost:8080") =>
    Options.Create(new PasswordResetOptions { FrontendUrl = frontendUrl });

  [Fact]
  public async Task CreateAdmin_creates_the_first_admin_and_returns_a_jwt()
  {
    await using var context = _db.CreateContext();
    var handler = new CreateAdminHandle(context, CreateJwtService());

    var response = await handler.Handle(new CreateAdminCommand("Admin", "admin@keues.dev", "P@ssw0rd!"));

    Assert.NotEqual(Guid.Empty, response.Id);
    Assert.Equal(Rol.Admin, (await context.Users.FindAsync(response.Id))!.Role);
    Assert.False(string.IsNullOrWhiteSpace(response.Jwt));

    // La contraseña se guarda con hash de BCrypt.
    Assert.NotEqual("P@ssw0rd!", (await context.Users.FindAsync(response.Id))!.PasswordHash);
    Assert.True(BCrypt.Net.BCrypt.Verify("P@ssw0rd!", (await context.Users.FindAsync(response.Id))!.PasswordHash));
  }

  [Fact]
  public async Task CreateAdmin_rejects_a_second_admin()
  {
    await using var context = _db.CreateContext();
    var handler = new CreateAdminHandle(context, CreateJwtService());
    await handler.Handle(new CreateAdminCommand("Admin", "admin@keues.dev", "P@ssw0rd!"));

    var ex = await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new CreateAdminCommand("Admin 2", "admin2@keues.dev", "P@ssw0rd!")));

    Assert.Equal("Admin user already exists", ex.Message);
  }

  [Fact]
  public async Task Login_returns_a_jwt_with_valid_credentials()
  {
    await using var context = _db.CreateContext();
    var create = new CreateAdminHandle(context, CreateJwtService());
    await create.Handle(new CreateAdminCommand("Admin", "admin@keues.dev", "P@ssw0rd!"));
    var handler = new LoginHandler(context, CreateJwtService());

    var response = await handler.Handle(new LoginCommand("admin@keues.dev", "P@ssw0rd!"));

    Assert.False(string.IsNullOrWhiteSpace(response.Jwt));
  }

  [Fact]
  public async Task Login_throws_with_wrong_password()
  {
    await using var context = _db.CreateContext();
    var create = new CreateAdminHandle(context, CreateJwtService());
    await create.Handle(new CreateAdminCommand("Admin", "admin@keues.dev", "P@ssw0rd!"));
    var handler = new LoginHandler(context, CreateJwtService());

    var ex = await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new LoginCommand("admin@keues.dev", "incorrecta")));

    Assert.Equal("Invalid credentials", ex.Message);
  }

  [Fact]
  public async Task Login_throws_with_unknown_email()
  {
    await using var context = _db.CreateContext();
    var handler = new LoginHandler(context, CreateJwtService());

    var ex = await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new LoginCommand("nadie@keues.dev", "P@ssw0rd!")));

    Assert.Equal("Invalid credentials", ex.Message);
  }

  [Fact]
  public async Task HasAdmin_reflects_whether_an_admin_exists()
  {
    await using var context = _db.CreateContext();
    var handler = new HasAdminHandler(context);

    Assert.False(await handler.Handle(new HasAdminQuery()));

    var create = new CreateAdminHandle(context, CreateJwtService());
    await create.Handle(new CreateAdminCommand("Admin", "admin@keues.dev", "P@ssw0rd!"));

    Assert.True(await handler.Handle(new HasAdminQuery()));
  }

  [Fact]
  public async Task Me_returns_the_current_user()
  {
    await using var context = _db.CreateContext();
    var create = new CreateAdminHandle(context, CreateJwtService());
    var created = await create.Handle(new CreateAdminCommand("Admin", "admin@keues.dev", "P@ssw0rd!"));
    var handler = new GetCurrentUserHandler(context);

    var response = await handler.Handle(new MeQuery(created.Id));

    Assert.Equal(created.Id, response.Id);
    Assert.Equal("admin@keues.dev", response.Email);
    Assert.Equal(Rol.Admin, response.Role);
  }

  [Fact]
  public async Task Me_throws_when_user_not_found()
  {
    await using var context = _db.CreateContext();
    var handler = new GetCurrentUserHandler(context);

    await Assert.ThrowsAsync<Exception>(() => handler.Handle(new MeQuery(Guid.NewGuid())));
  }

  [Fact]
  public async Task ForgotPassword_sends_an_email_to_an_existing_user()
  {
    await using var context = _db.CreateContext();
    var create = new CreateAdminHandle(context, CreateJwtService());
    await create.Handle(new CreateAdminCommand("Admin", "admin@keues.dev", "P@ssw0rd!"));
    var emails = new FakeEmailService();
    var handler = new ForgotPasswordHandler(context, CreateJwtService(), emails, ResetOptions());

    await handler.Handle(new ForgotPasswordCommand("admin@keues.dev"));

    var email = await emails.WaitForEmailAsync();
    Assert.Equal("admin@keues.dev", email.To);
    Assert.Contains("reset-password", email.HtmlBody);
  }

  [Fact]
  public async Task ForgotPassword_is_a_no_op_for_an_unknown_email()
  {
    await using var context = _db.CreateContext();
    var emails = new FakeEmailService();
    var handler = new ForgotPasswordHandler(context, CreateJwtService(), emails, ResetOptions());

    await handler.Handle(new ForgotPasswordCommand("nadie@keues.dev"));

    Assert.Empty(emails.Sent);
  }

  [Fact]
  public async Task ForgotPassword_throws_when_dashboard_url_is_not_configured()
  {
    await using var context = _db.CreateContext();
    var create = new CreateAdminHandle(context, CreateJwtService());
    await create.Handle(new CreateAdminCommand("Admin", "admin@keues.dev", "P@ssw0rd!"));
    var emails = new FakeEmailService();
    var handler = new ForgotPasswordHandler(context, CreateJwtService(), emails, ResetOptions(""));

    await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new ForgotPasswordCommand("admin@keues.dev")));
  }

  [Fact]
  public async Task ResetPassword_resets_the_password_with_a_valid_token()
  {
    await using var context = _db.CreateContext();
    var jwt = CreateJwtService();
    var create = new CreateAdminHandle(context, jwt);
    await create.Handle(new CreateAdminCommand("Admin", "admin@keues.dev", "P@ssw0rd!"));
    var user = await context.Users.FirstOrDefaultAsync(x => x.Email == "admin@keues.dev");

    var token = jwt.GeneratePasswordResetToken(user!);
    var handler = new ResetPasswordHandler(context, jwt);
    await handler.Handle(new ResetPasswordCommand(token, "admin@keues.dev", "NuevaP@ss!"));

    // La nueva contraseña permite loguearse.
    var login = new LoginHandler(context, jwt);
    var response = await login.Handle(new LoginCommand("admin@keues.dev", "NuevaP@ss!"));
    Assert.False(string.IsNullOrWhiteSpace(response.Jwt));
  }

  [Fact]
  public async Task ResetPassword_rejects_an_invalid_token()
  {
    await using var context = _db.CreateContext();
    var create = new CreateAdminHandle(context, CreateJwtService());
    await create.Handle(new CreateAdminCommand("Admin", "admin@keues.dev", "P@ssw0rd!"));
    var handler = new ResetPasswordHandler(context, CreateJwtService());

    await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new ResetPasswordCommand("token-invalido", "admin@keues.dev", "NuevaP@ss!")));
  }

  // ---------- CreateUser ----------

  [Fact]
  public async Task CreateUser_creates_a_user_with_hashed_password_and_lowercased_email()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var handler = new CreateUserHandler(context);

    var response = await handler.Handle(
      new CreateUserCommand("Juan Pérez", "Juan@Keues.DEV", "P@ssw0rd!", location.Id));

    Assert.NotEqual(Guid.Empty, response.Id);
    Assert.Equal("Juan Pérez", response.Name);
    Assert.Equal("juan@keues.dev", response.Email);

    var stored = await context.Users.FindAsync(response.Id);
    Assert.NotNull(stored);
    Assert.Equal(Rol.User, stored!.Role);
    Assert.Equal(location.Id, stored.LocationId);
    Assert.NotEqual("P@ssw0rd!", stored.PasswordHash);
    Assert.True(BCrypt.Net.BCrypt.Verify("P@ssw0rd!", stored.PasswordHash));
  }

  [Fact]
  public async Task CreateUser_throws_when_location_does_not_exist()
  {
    await using var context = _db.CreateContext();
    var handler = new CreateUserHandler(context);

    var ex = await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new CreateUserCommand("Juan", "juan@keues.dev", "P@ssw0rd!", Guid.NewGuid())));

    Assert.Equal("Location not found", ex.Message);
    Assert.Empty(context.Users);
  }

  [Fact]
  public async Task CreateUser_throws_when_email_already_exists_ignoring_case()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    await Seed.UserAsync(context, location.Id, email: "juan@keues.dev");
    var handler = new CreateUserHandler(context);

    var ex = await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new CreateUserCommand("Otro Juan", "JUAN@keues.dev", "P@ssw0rd!", location.Id)));

    Assert.Equal("User with this email already exists.", ex.Message);
    Assert.Equal(1, context.Users.Count());
  }

  [Fact]
  public async Task CreateUser_throws_with_a_null_email()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var handler = new CreateUserHandler(context);

    await Assert.ThrowsAnyAsync<Exception>(() =>
      handler.Handle(new CreateUserCommand("Juan", null!, "P@ssw0rd!", location.Id)));
  }

  [Fact]
  public async Task CreateUser_throws_with_a_null_password()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var handler = new CreateUserHandler(context);

    await Assert.ThrowsAnyAsync<Exception>(() =>
      handler.Handle(new CreateUserCommand("Juan", "juan@keues.dev", null!, location.Id)));
  }

  // ---------- UpdateUser ----------

  [Fact]
  public async Task UpdateUser_updates_name_email_and_location()
  {
    await using var context = _db.CreateContext();
    var origin = await Seed.LocationAsync(context, "Origen");
    var destination = await Seed.LocationAsync(context, "Destino");
    var user = await Seed.UserAsync(context, origin.Id, name: "Juan", email: "juan@keues.dev");
    var handler = new UpdateUserHandler(context);

    var response = await handler.Handle(new UpdateUserCommand(
      user.Id, "Juan Actualizado", "Nuevo@Keues.dev", "", destination.Id));

    Assert.Equal(user.Id, response.Id);
    Assert.Equal("Juan Actualizado", response.Name);
    Assert.Equal("nuevo@keues.dev", response.Email);

    var stored = await context.Users.FindAsync(user.Id);
    Assert.Equal(destination.Id, stored!.LocationId);
    Assert.Equal("nuevo@keues.dev", stored.Email);
  }

  [Fact]
  public async Task UpdateUser_without_password_keeps_the_previous_hash()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var user = await Seed.UserAsync(context, location.Id, password: "Original1!");
    var originalHash = user.PasswordHash;
    var handler = new UpdateUserHandler(context);

    await handler.Handle(new UpdateUserCommand(
      user.Id, "Juan", "juan@keues.dev", "", location.Id));

    var stored = await context.Users.FindAsync(user.Id);
    Assert.Equal(originalHash, stored!.PasswordHash);
    Assert.True(BCrypt.Net.BCrypt.Verify("Original1!", stored.PasswordHash));
  }

  [Fact]
  public async Task UpdateUser_with_password_replaces_the_credentials()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var user = await Seed.UserAsync(context, location.Id, password: "Original1!");
    var handler = new UpdateUserHandler(context);

    await handler.Handle(new UpdateUserCommand(
      user.Id, "Juan", "juan@keues.dev", "NuevaP@ss!", location.Id));

    var stored = await context.Users.FindAsync(user.Id);
    Assert.True(BCrypt.Net.BCrypt.Verify("NuevaP@ss!", stored!.PasswordHash));
    Assert.False(BCrypt.Net.BCrypt.Verify("Original1!", stored.PasswordHash));
  }

  [Fact]
  public async Task UpdateUser_allows_keeping_its_own_email()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var user = await Seed.UserAsync(context, location.Id, email: "juan@keues.dev");
    var handler = new UpdateUserHandler(context);

    var response = await handler.Handle(new UpdateUserCommand(
      user.Id, "Juan", "juan@keues.dev", "", location.Id));

    Assert.Equal("juan@keues.dev", response.Email);
  }

  [Fact]
  public async Task UpdateUser_throws_when_user_does_not_exist()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var handler = new UpdateUserHandler(context);

    var ex = await Assert.ThrowsAsync<Exception>(() => handler.Handle(new UpdateUserCommand(
      Guid.NewGuid(), "Juan", "juan@keues.dev", "", location.Id)));

    Assert.Equal("User not found", ex.Message);
  }

  [Fact]
  public async Task UpdateUser_throws_when_location_does_not_exist()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var user = await Seed.UserAsync(context, location.Id);
    var handler = new UpdateUserHandler(context);

    var ex = await Assert.ThrowsAsync<Exception>(() => handler.Handle(new UpdateUserCommand(
      user.Id, "Juan", "juan@keues.dev", "", Guid.NewGuid())));

    Assert.Equal("Location not found", ex.Message);
  }

  [Fact]
  public async Task UpdateUser_throws_when_target_is_an_admin()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var admin = await Seed.UserAsync(context, location.Id, email: "admin@keues.dev", role: Rol.Admin);
    var handler = new UpdateUserHandler(context);

    var ex = await Assert.ThrowsAsync<Exception>(() => handler.Handle(new UpdateUserCommand(
      admin.Id, "Admin", "admin@keues.dev", "", location.Id)));

    Assert.Equal("You cannot update an admin user", ex.Message);
  }

  [Fact]
  public async Task UpdateUser_throws_when_email_is_already_used_by_another_user()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    await Seed.UserAsync(context, location.Id, email: "ocupado@keues.dev");
    var user = await Seed.UserAsync(context, location.Id, email: "juan@keues.dev");
    var handler = new UpdateUserHandler(context);

    var ex = await Assert.ThrowsAsync<Exception>(() => handler.Handle(new UpdateUserCommand(
      user.Id, "Juan", "Ocupado@keues.dev", "", location.Id)));

    Assert.Equal("Email already in use", ex.Message);
  }

  [Fact]
  public async Task UpdateUser_throws_with_a_null_email()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var user = await Seed.UserAsync(context, location.Id);
    var handler = new UpdateUserHandler(context);

    await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(new UpdateUserCommand(
      user.Id, "Juan", null!, "", location.Id)));
  }

  // ---------- GetUser ----------

  [Fact]
  public async Task GetUser_returns_the_user()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var user = await Seed.UserAsync(context, location.Id, name: "Juan", email: "juan@keues.dev");
    var handler = new GetUserHandler(context);

    var response = await handler.Handle(new GetUserQuery(user.Id));

    Assert.Equal(user.Id, response.Id);
    Assert.Equal("Juan", response.Name);
    Assert.Equal("juan@keues.dev", response.Email);
    Assert.Equal(location.Id, response.LocationId);
    Assert.True(response.Enabled);
  }

  [Fact]
  public async Task GetUser_throws_when_not_found()
  {
    await using var context = _db.CreateContext();
    var handler = new GetUserHandler(context);

    var ex = await Assert.ThrowsAsync<Exception>(() => handler.Handle(new GetUserQuery(Guid.NewGuid())));

    Assert.Equal("User not found", ex.Message);
  }

  // ---------- GetAllUsers ----------

  [Fact]
  public async Task GetAllUsers_excludes_admins()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    await Seed.UserAsync(context, location.Id, email: "admin@keues.dev", role: Rol.Admin);
    await Seed.UserAsync(context, location.Id, email: "juan@keues.dev");
    await Seed.UserAsync(context, location.Id, email: "ana@keues.dev");
    var handler = new GetAllUsersHandler(context);

    var response = await handler.Handle(new GetAllUsersQuery());

    Assert.Equal(2, response.Total);
    Assert.DoesNotContain(response.Users, u => u.Email == "admin@keues.dev");
  }

  [Fact]
  public async Task GetAllUsers_filters_by_location()
  {
    await using var context = _db.CreateContext();
    var locA = await Seed.LocationAsync(context, "A");
    var locB = await Seed.LocationAsync(context, "B");
    await Seed.UserAsync(context, locA.Id, email: "a1@keues.dev");
    await Seed.UserAsync(context, locA.Id, email: "a2@keues.dev");
    await Seed.UserAsync(context, locB.Id, email: "b1@keues.dev");
    var handler = new GetAllUsersHandler(context);

    var onlyA = await handler.Handle(new GetAllUsersQuery { LocationId = locA.Id });
    var all = await handler.Handle(new GetAllUsersQuery());

    Assert.Equal(2, onlyA.Total);
    Assert.All(onlyA.Users, u => Assert.Equal(locA.Id, u.LocationId));
    Assert.Equal(3, all.Total);
  }

  [Fact]
  public async Task GetAllUsers_filters_by_name_ignoring_case_and_partial_matches()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    await Seed.UserAsync(context, location.Id, name: "Juan Pérez", email: "juan@keues.dev");
    await Seed.UserAsync(context, location.Id, name: "Ana Gómez", email: "ana@keues.dev");
    var handler = new GetAllUsersHandler(context);

    var response = await handler.Handle(new GetAllUsersQuery { Name = "JUAN" });

    Assert.Equal(1, response.Total);
    Assert.Equal("Juan Pérez", response.Users.Single().Name);
  }

  [Fact]
  public async Task GetAllUsers_filters_by_active_state()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    await Seed.UserAsync(context, location.Id, email: "activo@keues.dev", enabled: true);
    await Seed.UserAsync(context, location.Id, email: "inactivo@keues.dev", enabled: false);
    var handler = new GetAllUsersHandler(context);

    var active = await handler.Handle(new GetAllUsersQuery { IsActive = true });
    var inactive = await handler.Handle(new GetAllUsersQuery { IsActive = false });

    Assert.Equal("activo@keues.dev", active.Users.Single().Email);
    Assert.Equal("inactivo@keues.dev", inactive.Users.Single().Email);
  }

  [Fact]
  public async Task GetAllUsers_paginates_and_reports_totals()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    for (var i = 1; i <= 5; i++)
      await Seed.UserAsync(context, location.Id, name: $"Usuario {i:D2}", email: $"u{i}@keues.dev");
    var handler = new GetAllUsersHandler(context);

    var page1 = await handler.Handle(new GetAllUsersQuery { Page = 1, Limit = 2 });
    var page2 = await handler.Handle(new GetAllUsersQuery { Page = 2, Limit = 2 });

    Assert.Equal(5, page1.Total);
    Assert.Equal(3, page1.TotalPages);
    Assert.Equal(2, page1.Users.Count());
    Assert.Equal(2, page2.Users.Count());
    Assert.Empty((await handler.Handle(new GetAllUsersQuery { Page = 3, Limit = 2 })).Users.Skip(1));
  }

  [Fact]
  public async Task GetAllUsers_sorts_by_name()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    await Seed.UserAsync(context, location.Id, name: "Beatriz", email: "bea@keues.dev");
    await Seed.UserAsync(context, location.Id, name: "Ana", email: "ana@keues.dev");
    var handler = new GetAllUsersHandler(context);

    var asc = await handler.Handle(new GetAllUsersQuery { SortOrder = SortOrder.Asc });
    var desc = await handler.Handle(new GetAllUsersQuery { SortOrder = SortOrder.Desc });

    Assert.Equal(["Ana", "Beatriz"], asc.Users.Select(u => u.Name).ToArray());
    Assert.Equal(["Beatriz", "Ana"], desc.Users.Select(u => u.Name).ToArray());
  }

  // ---------- EnableDisableUser ----------

  [Fact]
  public async Task EnableDisableUser_disables_and_enables_the_user()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var user = await Seed.UserAsync(context, location.Id, enabled: true);
    var handler = new EnableDisableUserHandler(context);

    await handler.Handle(new EnableDisableUserCommand(user.Id, false));
    Assert.False((await context.Users.FindAsync(user.Id))!.Enabled);

    await handler.Handle(new EnableDisableUserCommand(user.Id, true));
    Assert.True((await context.Users.FindAsync(user.Id))!.Enabled);
  }

  [Fact]
  public async Task EnableDisableUser_throws_when_not_found()
  {
    await using var context = _db.CreateContext();
    var handler = new EnableDisableUserHandler(context);

    var ex = await Assert.ThrowsAsync<Exception>(() =>
      handler.Handle(new EnableDisableUserCommand(Guid.NewGuid(), false)));

    Assert.Equal("User not found", ex.Message);
  }
}
