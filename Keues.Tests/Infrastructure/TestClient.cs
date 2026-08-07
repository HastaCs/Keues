using System.Net.Http.Json;

namespace Keues.Tests.Infrastructure;

/// <summary>
/// Envuelve un HttpClient y añade el JWT (header Cookie) a cada petición cuando
/// la propiedad <see cref="Jwt"/> está establecida.
/// </summary>
public sealed class TestClient
{
  private readonly HttpClient _http;

  public TestClient(HttpClient http)
  {
    _http = http;
  }

  public string? Jwt { get; set; }

  public Task<HttpResponseMessage> GetAsync(string uri) =>
    SendAsync(HttpMethod.Get, uri, null);

  public Task<HttpResponseMessage> PostAsync(string uri, object? body = null) =>
    SendAsync(HttpMethod.Post, uri, body);

  public Task<HttpResponseMessage> PutAsync(string uri, object? body = null) =>
    SendAsync(HttpMethod.Put, uri, body);

  public Task<HttpResponseMessage> DeleteAsync(string uri) =>
    SendAsync(HttpMethod.Delete, uri, null);

  public async Task<T?> ReadAsync<T>(HttpResponseMessage response)
  {
    return await response.Content.ReadFromJsonAsync<T>(JsonHelper.Options);
  }

  private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, object? body)
  {
    using var request = new HttpRequestMessage(method, uri);
    if (Jwt != null)
    {
      request.Headers.Add("Cookie", $"access_token={Jwt}");
    }

    if (body != null)
    {
      request.Content = JsonContent.Create(body, options: JsonHelper.Options);
    }

    return await _http.SendAsync(request);
  }
}
