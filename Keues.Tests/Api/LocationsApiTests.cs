using Keues.Tests.Infrastructure;
using Xunit;

namespace Keues.Tests.Api;

public class LocationsApiTests : ApiTestBase
{
  [Fact]
  public async Task Create_returns_201_with_the_location()
  {
    var client = await CreateAuthenticatedClientAsync();

    var response = await client.PostAsync("/api/locations", new
    {
      name = "Tienda",
      description = "Tienda central",
      color = "red"
    });

    Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    var body = await client.ReadAsync<LocationBody>(response);
    Assert.NotEqual(Guid.Empty, body!.Id);
    Assert.Equal("Tienda", body.Name);
    Assert.Equal("red", body.Color);
  }

  [Fact]
  public async Task Create_returns_401_without_authentication()
  {
    var client = Factory.CreateTestClient();

    var response = await client.PostAsync("/api/locations", new
    {
      name = "Tienda",
      description = "",
      color = "blue"
    });

    Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Get_returns_the_location()
  {
    var client = await CreateAuthenticatedClientAsync();
    var created = await client.ReadAsync<LocationBody>(
      await client.PostAsync("/api/locations", new { name = "Tienda", description = "", color = "blue" }));

    var response = await client.GetAsync($"/api/locations/{created!.Id}");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<LocationBody>(response);
    Assert.Equal(created.Id, body!.Id);
  }

  [Fact]
  public async Task Get_with_non_guid_id_returns_404()
  {
    var client = Factory.CreateTestClient();

    var response = await client.GetAsync("/api/locations/not-a-guid");

    Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task Update_with_non_guid_id_returns_404()
  {
    var client = Factory.CreateTestClient();

    var response = await client.PutAsync("/api/locations/not-a-guid", new
    {
      name = "Tienda",
      description = "",
      color = "blue"
    });

    Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task Delete_with_non_guid_id_returns_404()
  {
    var client = Factory.CreateTestClient();

    var response = await client.DeleteAsync("/api/locations/not-a-guid");

    Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetAll_returns_all_locations()
  {
    var client = await CreateAuthenticatedClientAsync();
    await client.PostAsync("/api/locations", new { name = "A", description = "", color = "blue" });
    await client.PostAsync("/api/locations", new { name = "B", description = "", color = "blue" });

    var response = await client.GetAsync("/api/locations");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<DataBody<LocationBody>>(response);
    Assert.Equal(2, body!.Data.Count);
  }

  [Fact]
  public async Task Update_returns_the_updated_location()
  {
    var client = await CreateAuthenticatedClientAsync();
    var created = await client.ReadAsync<LocationBody>(
      await client.PostAsync("/api/locations", new { name = "Tienda", description = "", color = "blue" }));

    var response = await client.PutAsync($"/api/locations/{created!.Id}", new
    {
      name = "Renombrada",
      description = "Nueva descripción",
      color = "green"
    });

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var body = await client.ReadAsync<LocationBody>(response);
    Assert.Equal("Renombrada", body!.Name);
    Assert.Equal("green", body.Color);
  }

  [Fact]
  public async Task Update_returns_401_without_authentication()
  {
    var client = Factory.CreateTestClient();

    var response = await client.PutAsync($"/api/locations/{Guid.NewGuid()}", new
    {
      name = "Renombrada",
      description = "",
      color = "blue"
    });

    Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Delete_returns_204_and_hides_the_location()
  {
    var client = await CreateAuthenticatedClientAsync();
    var created = await client.ReadAsync<LocationBody>(
      await client.PostAsync("/api/locations", new { name = "Tienda", description = "", color = "blue" }));

    var response = await client.DeleteAsync($"/api/locations/{created!.Id}");

    Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);

    var get = await client.GetAsync($"/api/locations/{created.Id}");
    Assert.Equal(System.Net.HttpStatusCode.BadRequest, get.StatusCode);
  }

  [Fact]
  public async Task Delete_returns_401_without_authentication()
  {
    var client = Factory.CreateTestClient();

    var response = await client.DeleteAsync($"/api/locations/{Guid.NewGuid()}");

    Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
  }

  private sealed record LocationBody(Guid Id, string Name, string? Description, string Color);
  private sealed record DataBody<T>(List<T> Data);
}
