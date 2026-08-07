using System.Text.RegularExpressions;
using Keues.Domain.Enums;
using Keues.Tests.Infrastructure;
using Xunit;

namespace Keues.Tests.Api;

public class AuthApiTests : ApiTestBase
{
  [Fact]
  public async Task HasAdmin_returns_false_when_no_admin_exists()
  {
    var client = Factory.CreateTestClient();

    var response = await client.GetAsync("/api/users/has-admin");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<HasAdminBody>(response);
    Assert.False(body!.HasAdmin);
  }

  [Fact]
  public async Task CreateAdmin_sets_cookie_and_returns_jwt()
  {
    var client = Factory.CreateTestClient();

    var response = await client.PostAsync("/api/users/create-admin", new
    {
      name = "Admin",
      email = "admin@keues.dev",
      password = "P@ssw0rd!"
    });

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    Assert.Contains(response.Headers, h =>
      h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase) &&
      h.Value.Any(v => v.Contains("access_token=")));

    var body = await client.ReadAsync<CreateAdminBody>(response);
    Assert.NotNull(body);
    Assert.False(string.IsNullOrWhiteSpace(body.Jwt));
    Assert.Equal("admin@keues.dev", body.Email);
  }

  [Fact]
  public async Task CreateAdmin_second_call_returns_400()
  {
    var client = Factory.CreateTestClient();
    await client.PostAsync("/api/users/create-admin", new
    {
      name = "Admin",
      email = "admin@keues.dev",
      password = "P@ssw0rd!"
    });

    var response = await client.PostAsync("/api/users/create-admin", new
    {
      name = "Admin 2",
      email = "admin2@keues.dev",
      password = "P@ssw0rd!"
    });

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task CreateAdmin_with_invalid_email_returns_400()
  {
    var client = Factory.CreateTestClient();

    var response = await client.PostAsync("/api/users/create-admin", new
    {
      name = "Admin",
      email = "no-es-un-email",
      password = "P@ssw0rd!"
    });

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Login_returns_a_jwt()
  {
    var client = Factory.CreateTestClient();
    await client.PostAsync("/api/users/create-admin", new
    {
      name = "Admin",
      email = "admin@keues.dev",
      password = "P@ssw0rd!"
    });

    var response = await client.PostAsync("/api/users/login", new
    {
      email = "admin@keues.dev",
      password = "P@ssw0rd!"
    });

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<LoginBody>(response);
    Assert.False(string.IsNullOrWhiteSpace(body!.Jwt));
  }

  [Fact]
  public async Task Login_with_wrong_password_returns_400()
  {
    var client = Factory.CreateTestClient();
    await client.PostAsync("/api/users/create-admin", new
    {
      name = "Admin",
      email = "admin@keues.dev",
      password = "P@ssw0rd!"
    });

    var response = await client.PostAsync("/api/users/login", new
    {
      email = "admin@keues.dev",
      password = "incorrecta"
    });

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Me_without_authentication_returns_401()
  {
    var client = Factory.CreateTestClient();

    var response = await client.GetAsync("/api/users/me");

    Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Me_with_authentication_returns_the_user()
  {
    var client = Factory.CreateTestClient();
    var create = await client.PostAsync("/api/users/create-admin", new
    {
      name = "Admin",
      email = "admin@keues.dev",
      password = "P@ssw0rd!"
    });
    var created = await client.ReadAsync<CreateAdminBody>(create);
    client.Jwt = created!.Jwt;

    var response = await client.GetAsync("/api/users/me");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<MeBody>(response);
    Assert.Equal("admin@keues.dev", body!.Email);
    Assert.Equal("Admin", body.Name);
    Assert.Equal((int)Rol.Admin, body.Role);
  }

  [Fact]
  public async Task Logout_returns_200()
  {
    var client = Factory.CreateTestClient();

    var response = await client.PostAsync("/api/users/logout");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    Assert.Contains(response.Headers, h =>
      h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase) &&
      h.Value.Any(v => v.Contains("access_token=")));
  }

  [Fact]
  public async Task ForgotPassword_and_ResetPassword_complete_the_flow()
  {
    var client = Factory.CreateTestClient();
    await client.PostAsync("/api/users/create-admin", new
    {
      name = "Admin",
      email = "admin@keues.dev",
      password = "P@ssw0rd!"
    });

    var forgot = await client.PostAsync("/api/users/forgot-password", new
    {
      email = "admin@keues.dev"
    });
    Assert.Equal(System.Net.HttpStatusCode.OK, forgot.StatusCode);

    var email = await Factory.Emails.WaitForEmailAsync();
    var tokenMatch = Regex.Match(email.HtmlBody, "token=([^&\"']+)");
    Assert.True(tokenMatch.Success, "El email debería contener el token de reseteo.");
    var token = Uri.UnescapeDataString(tokenMatch.Groups[1].Value);

    var reset = await client.PostAsync("/api/users/reset-password", new
    {
      token,
      email = "admin@keues.dev",
      password = "NuevaP@ss!"
    });
    Assert.Equal(System.Net.HttpStatusCode.OK, reset.StatusCode);

    var login = await client.PostAsync("/api/users/login", new
    {
      email = "admin@keues.dev",
      password = "NuevaP@ss!"
    });
    Assert.Equal(System.Net.HttpStatusCode.OK, login.StatusCode);
  }

  [Fact]
  public async Task ResetPassword_with_invalid_token_returns_400()
  {
    var client = Factory.CreateTestClient();
    await client.PostAsync("/api/users/create-admin", new
    {
      name = "Admin",
      email = "admin@keues.dev",
      password = "P@ssw0rd!"
    });

    var response = await client.PostAsync("/api/users/reset-password", new
    {
      token = "token-invalido",
      email = "admin@keues.dev",
      password = "NuevaP@ss!"
    });

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  private sealed record HasAdminBody(bool HasAdmin);
  private sealed record CreateAdminBody(Guid Id, string Name, string Email, string Jwt);
  private sealed record LoginBody(string Jwt);
  private sealed record MeBody(Guid Id, string Name, string Email, int Role);
}
