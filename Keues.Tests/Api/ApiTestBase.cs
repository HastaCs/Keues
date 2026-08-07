using Keues.Tests.Infrastructure;
using Xunit;

namespace Keues.Tests.Api;

public abstract class ApiTestBase : IDisposable
{
  protected TestWebApplicationFactory Factory { get; } = new();

  /// <summary>
  /// Cliente con una sesión de administrador (JWT en el header Cookie) para
  /// acceder a los endpoints protegidos con [Authorize].
  /// </summary>
  protected async Task<TestClient> CreateAuthenticatedClientAsync()
  {
    var client = Factory.CreateTestClient();
    var response = await client.PostAsync("/api/users/create-admin", new
    {
      name = "Admin",
      email = $"admin_{Guid.NewGuid():N}@keues.dev",
      password = "P@ssw0rd!"
    });

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    var body = await client.ReadAsync<AuthBody>(response);
    client.Jwt = body!.Jwt;
    return client;
  }

  public void Dispose() => Factory.Dispose();

  private sealed record AuthBody(Guid Id, string Name, string Email, string Jwt);
}
