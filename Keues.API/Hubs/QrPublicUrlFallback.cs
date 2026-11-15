using Keues.Application.Common;
using Microsoft.AspNetCore.Http;

namespace Keues.API.Hubs;

public static class QrPublicUrlFallback
{
  public static void ApplyFromRequest(QrOptions options, HttpRequest request)
  {
    if (!string.IsNullOrWhiteSpace(options.PublicBaseUrl))
    {
      return;
    }

    var publicPort = int.TryParse(
      Environment.GetEnvironmentVariable("KEUES_PUBLIC_PORT"),
      out var configuredPort)
      ? configuredPort
      : 8081;

    var host = request.Host.Host;
    var port = request.Host.Port;
    if (port is null or 8080)
    {
      port = publicPort;
    }

    var candidate = port is 80 or 443
      ? $"{request.Scheme}://{host}"
      : $"{request.Scheme}://{host}:{port}";

    if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) || uri.IsLoopback)
    {
      return;
    }

    options.PublicBaseUrl = candidate.TrimEnd('/');
  }
}
