using System.Net;
using System.Text;
using System.Text.Json;
using Keues.Infrastructure.Email;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keues.Tests.UseCases;

public class MailjetEmailServiceTests
{
  private sealed class StubHandler : HttpMessageHandler
  {
    private readonly HttpStatusCode _status;
    private readonly string _body;

    public StubHandler(HttpStatusCode status, string body)
    {
      _status = status;
      _body = body;
    }

    public HttpRequestMessage? Request { get; private set; }
    public string? Body { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
    {
      Request = request;
      Body = request.Content is null
        ? null
        : await request.Content.ReadAsStringAsync(cancellationToken);

      return new HttpResponseMessage(_status)
      {
        Content = new StringContent(_body, Encoding.UTF8, "application/json")
      };
    }
  }

  private static IOptions<EmailOptions> CreateOptions(
    string apiKey = "api-key",
    string secretKey = "secret-key",
    string from = "no-reply@keues.dev",
    string fromName = "Keues")
  {
    return Options.Create(new EmailOptions
    {
      Provider = "mailjet",
      From = from,
      FromName = fromName,
      Mailjet = new MailjetOptions
      {
        ApiKey = apiKey,
        SecretKey = secretKey
      }
    });
  }

  [Fact]
  public async Task SendAsync_posts_the_message_to_mailjet_with_basic_auth()
  {
    var handler = new StubHandler(
      HttpStatusCode.OK,
      """{"Messages":[{"Status":"success"}]}""");
    var service = new MailjetEmailService(new HttpClient(handler), CreateOptions());

    await service.SendAsync("user@keues.dev", "Asunto", "<p>Hola</p>");

    Assert.NotNull(handler.Request);
    Assert.Equal(HttpMethod.Post, handler.Request!.Method);
    Assert.Equal("https://api.mailjet.com/v3.1/send", handler.Request.RequestUri!.ToString());
    Assert.Equal("Basic", handler.Request.Headers.Authorization!.Scheme);
    Assert.Equal(
      Convert.ToBase64String(Encoding.UTF8.GetBytes("api-key:secret-key")),
      handler.Request.Headers.Authorization.Parameter);

    using var json = JsonDocument.Parse(handler.Body!);
    var message = json.RootElement.GetProperty("Messages")[0];
    Assert.Equal("Asunto", message.GetProperty("Subject").GetString());
    Assert.Equal("<p>Hola</p>", message.GetProperty("HTMLPart").GetString());
    Assert.Equal("no-reply@keues.dev", message.GetProperty("From").GetProperty("Email").GetString());
    Assert.Equal("Keues", message.GetProperty("From").GetProperty("Name").GetString());
    Assert.Equal("user@keues.dev", message.GetProperty("To")[0].GetProperty("Email").GetString());
    Assert.False(json.RootElement.TryGetProperty("SandboxMode", out _));
  }

  [Fact]
  public async Task SendAsync_throws_when_mailjet_reports_an_error_status()
  {
    var handler = new StubHandler(
      HttpStatusCode.OK,
      """{"Messages":[{"Status":"error","Errors":[{"ErrorMessage":"invalid sender"}]}]}""");
    var service = new MailjetEmailService(new HttpClient(handler), CreateOptions());

    var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      service.SendAsync("user@keues.dev", "Asunto", "<p>Hola</p>"));

    Assert.Contains("rejected", ex.Message);
  }

  [Fact]
  public async Task SendAsync_throws_when_http_status_is_not_success()
  {
    var handler = new StubHandler(
      HttpStatusCode.Unauthorized,
      """{"ErrorMessage":"bad credentials"}""");
    var service = new MailjetEmailService(new HttpClient(handler), CreateOptions());

    var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      service.SendAsync("user@keues.dev", "Asunto", "<p>Hola</p>"));

    Assert.Contains("401", ex.Message);
    Assert.Contains("bad credentials", ex.Message);
  }

  [Fact]
  public async Task SendAsync_throws_when_api_credentials_are_missing()
  {
    var handler = new StubHandler(HttpStatusCode.OK, """{"Messages":[{"Status":"success"}]}""");
    var service = new MailjetEmailService(
      new HttpClient(handler),
      CreateOptions(apiKey: "", secretKey: ""));

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
      service.SendAsync("user@keues.dev", "Asunto", "<p>Hola</p>"));

    Assert.Null(handler.Request);
  }

  [Fact]
  public async Task SendAsync_throws_when_from_is_missing()
  {
    var handler = new StubHandler(HttpStatusCode.OK, """{"Messages":[{"Status":"success"}]}""");
    var service = new MailjetEmailService(
      new HttpClient(handler),
      CreateOptions(from: ""));

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
      service.SendAsync("user@keues.dev", "Asunto", "<p>Hola</p>"));

    Assert.Null(handler.Request);
  }
}
