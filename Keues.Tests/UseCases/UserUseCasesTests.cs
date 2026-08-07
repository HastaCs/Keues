using Keues.Application.Common;
using Keues.Application.Features.Users.CreateAdmin;
using Keues.Application.Features.Users.ForgotPassword;
using Keues.Application.Features.Users.HasAdmin;
using Keues.Application.Features.Users.Login;
using Keues.Application.Features.Users.Me;
using Keues.Application.Features.Users.ResetPassword;
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
}
