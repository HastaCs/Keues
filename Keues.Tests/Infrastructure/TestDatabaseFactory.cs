using Keues.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Keues.Tests.Infrastructure;

/// <summary>
/// Crea una base de datos SQLite 100% en memoria (conexión única compartida,
/// que permanece abierta). Soporta transacciones y Migrate(), necesarios para
/// CallNextTicketHandler y para el arranque de la API.
/// </summary>
public sealed class TestDatabaseFactory : IDisposable
{
  private readonly SqliteConnection _connection;

  public TestDatabaseFactory()
  {
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();
    using var context = CreateContext();
    context.Database.EnsureCreated();
  }

  public AppDbContext CreateContext()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseSqlite(_connection)
      .Options;
    return new AppDbContext(options);
  }

  public void Dispose()
  {
    _connection.Dispose();
  }
}
