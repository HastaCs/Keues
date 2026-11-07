using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Keues.Application.Common;
using Microsoft.Extensions.Options;

namespace Keues.Infrastructure.Email;

public class MailjetEmailService : IEmailService
{
  private const string SendUrl = "https://api.mailjet.com/v3.1/send";

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  private readonly HttpClient _http;
  private readonly IOptions<EmailOptions> _options;

  public MailjetEmailService(HttpClient http, IOptions<EmailOptions> options)
  {
    _http = http;
    _options = options;
  }

  public async Task SendAsync(string to, string subject, string htmlBody)
  {
    var options = _options.Value;
    var mailjet = options.Mailjet;

    if (string.IsNullOrWhiteSpace(mailjet.ApiKey) ||
        string.IsNullOrWhiteSpace(mailjet.SecretKey))
      throw new InvalidOperationException(
        "Mailjet ApiKey/SecretKey are not configured (KEUES_MAILJET_API_KEY / KEUES_MAILJET_SECRET_KEY).");

    if (string.IsNullOrWhiteSpace(options.From))
      throw new InvalidOperationException(
        "Email From is not configured (KEUES_EMAIL_FROM).");

    if (string.IsNullOrWhiteSpace(to))
      throw new ArgumentException("Recipient is required.", nameof(to));

    var payload = new
    {
      Messages = new[]
      {
        new
        {
          From = new
          {
            Email = options.From,
            Name = string.IsNullOrWhiteSpace(options.FromName) ? "Keues" : options.FromName
          },
          To = new[] { new { Email = to } },
          Subject = subject,
          HTMLPart = htmlBody
        }
      }
    };

    using var request = new HttpRequestMessage(HttpMethod.Post, SendUrl);

    var credentials = Convert.ToBase64String(
      Encoding.UTF8.GetBytes($"{mailjet.ApiKey}:{mailjet.SecretKey}"));
    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    request.Content = JsonContent.Create(payload, options: JsonOptions);

    using var response = await _http.SendAsync(request);
    var responseBody = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
      throw new InvalidOperationException(
        $"Mailjet request failed ({(int)response.StatusCode}): {responseBody}");

    var result = JsonSerializer.Deserialize<MailjetResponse>(responseBody, JsonOptions);
    var failed = result?.Messages?.FirstOrDefault(
      m => !string.Equals(m.Status, "success", StringComparison.OrdinalIgnoreCase));

    if (result?.Messages is null || failed is not null)
      throw new InvalidOperationException($"Mailjet rejected the message: {responseBody}");
  }

  private sealed record MailjetResponse(
    [property: JsonPropertyName("Messages")] MailjetMessage[]? Messages);

  private sealed record MailjetMessage(
    [property: JsonPropertyName("Status")] string? Status);
}
