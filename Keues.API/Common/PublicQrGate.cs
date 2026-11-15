using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Keues.API.Common;

public static class PublicQrGate
{
  private static readonly Regex FlowRoute = new(
    @"^/api/flows/[0-9a-fA-F-]{36}$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase);

  private static readonly Regex TicketRoute = new(
    @"^/api/tickets/[0-9a-fA-F-]{36}$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase);

  private static readonly Regex QrTicketRoute = new(
    @"^/api/queues/[0-9a-fA-F-]{36}/qr-new-ticket$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase);

  public static bool IsAllowed(HttpRequest request)
  {
    var path = request.Path.Value ?? string.Empty;

    if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method))
    {
      if (path.Equals("/turno", StringComparison.OrdinalIgnoreCase) ||
          path.StartsWith("/turno/", StringComparison.OrdinalIgnoreCase) ||
          path.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase) ||
          path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }

      return FlowRoute.IsMatch(path) || TicketRoute.IsMatch(path);
    }

    return HttpMethods.IsPost(request.Method) && QrTicketRoute.IsMatch(path);
  }
}
