using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Keues.Application.Common;
using Keues.Application.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Keues.Infrastructure.Http.Webhooks;

public class WebhookSender : IWebhookSender
{
  private readonly IApplicationDbContext _dbContext;

  //Esto se inyecta desde el program.cs, e internamente usa un HttpClientFactory para crear instancias de HttpClient,
  private readonly HttpClient _httpClient;
  private readonly ILogger<WebhookSender> _logger;

  public WebhookSender(IApplicationDbContext dbContext, HttpClient httpClient, ILogger<WebhookSender> logger)
  {
    _dbContext = dbContext;
    _httpClient = httpClient;
    _logger = logger;
  }

  public async Task SendAsync(object payload, CancellationToken cancellationToken = default)
  {
    var config = await _dbContext.WebhooksConfigs.FirstOrDefaultAsync(cancellationToken);
    if (!config.Enabled)
      return;

    var url = config?.Url;
    var key = config?.Key ?? "";

    var json = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web);
    //using var content = new StringContent(json, Encoding.UTF8, "application/json");
    // using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
    //request.Headers.TryAddWithoutValidation("X-Keues-Key", key);
    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Keues-Key", key);
    var res = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
    if (res.IsSuccessStatusCode)
    {
      _logger.LogInformation($"Webhook sent successfully.{Environment.NewLine}{json}");
    }
    else
      _logger.LogError($"Failed to send webhook. Status code: {res.StatusCode} {res.ReasonPhrase}");
  }
}