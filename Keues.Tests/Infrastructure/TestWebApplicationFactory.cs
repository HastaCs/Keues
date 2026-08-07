using Keues.Application.Common;
using Keues.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Keues.Tests.Infrastructure;

/// <summary>
/// Levanta la API completa (WebApplicationFactory) con una base de datos SQLite
/// en memoria y un IEmailService falso.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
  public const string JwtKey =
    "test-secret-key-that-is-at-least-32-characters-long-0123456789abcdef";
  public const string DashboardUrl = "http://localhost:8080";

  private readonly SqliteConnection _connection = new("DataSource=:memory:");
  private readonly string _contentRoot;

  public FakeEmailService Emails { get; } = new();

  public TestWebApplicationFactory()
  {
    _contentRoot = Path.Combine(Path.GetTempPath(), "keues-tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(_contentRoot);
    _connection.Open();

    // RuntimeConfig.ApplyEnvironmentOverrides() los lee en el arranque.
    Environment.SetEnvironmentVariable("KEUES_JWT_KEY", JwtKey);
    Environment.SetEnvironmentVariable("KEUES_DASHBOARD_URL", DashboardUrl);

    // El content root es temporal, así que appsettings.json del proyecto no se
    // carga. Program.cs lee la sección Jwt al inicio (antes de Build()), por eso
    // usamos variables de entorno (Jwt__Issuer -> Jwt:Issuer) que sí están
    // disponibles en ese momento.
    Environment.SetEnvironmentVariable("Jwt__Issuer", "Keues");
    Environment.SetEnvironmentVariable("Jwt__Audience", "Keues");
    Environment.SetEnvironmentVariable("Jwt__ExpirationInMinutes", "1440");
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseSetting(WebHostDefaults.ContentRootKey, _contentRoot);

    builder.ConfigureServices(services =>
    {
      services.RemoveAll<AppDbContext>();
      services.RemoveAll<IApplicationDbContext>();
      services.RemoveAll<DbContextOptions<AppDbContext>>();
      services.RemoveAll<DbContextOptions>();

      var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite(_connection)
        .Options;

      services.AddScoped(_ => new AppDbContext(options));
      services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

      services.RemoveAll<IEmailService>();
      services.AddSingleton<IEmailService>(Emails);
    });
  }

  /// <summary>
  /// Cliente HTTP con acceso directo al TestServer. No procesa cookies, de modo
  /// que el JWT se envía manualmente mediante el header Cookie (la cookie de la
  /// app es Secure y no vuelve sola por HTTP).
  /// </summary>
  public TestClient CreateTestClient()
  {
    var handler = Server.CreateHandler();
    var http = new HttpClient(handler, disposeHandler: true)
    {
      BaseAddress = Server.BaseAddress
    };
    return new TestClient(http);
  }

  public async Task WithContextAsync(Func<AppDbContext, Task> action)
  {
    using var scope = Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await action(context);
  }

  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);
    if (disposing)
    {
      _connection.Dispose();
      try
      {
        Directory.Delete(_contentRoot, recursive: true);
      }
      catch
      {
        // Best-effort: la carpeta temporal se ignora si no se pudo borrar.
      }
    }
  }
}
