using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Keues.API.Common;

public static class QrServerAddress
{
  public static string? TryDetect()
  {
    if (IsRunningInContainer())
    {
      return null;
    }

    var publicPort = int.TryParse(
      Environment.GetEnvironmentVariable("KEUES_PUBLIC_PORT"),
      out var configuredPort)
      ? configuredPort
      : 8081;

    return Build(FindLanIPv4(), publicPort);
  }

  public static string? Build(string? lanIp, int publicPort = 8081)
  {
    if (string.IsNullOrWhiteSpace(lanIp))
    {
      return null;
    }

    return $"http://{lanIp}:{publicPort}";
  }

  public static int? ParsePort(string? urls)
  {
    if (string.IsNullOrWhiteSpace(urls))
    {
      return null;
    }

    var candidates = urls
      .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
      .OrderBy(url => url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? 0 : 1);

    foreach (var candidate in candidates)
    {
      var normalized = candidate
        .Replace("://+", "://localhost", StringComparison.Ordinal)
        .Replace("://*", "://localhost", StringComparison.Ordinal);

      if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri) &&
          (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
      {
        return uri.Port;
      }
    }

    return null;
  }

  private static string? FindLanIPv4()
  {
    var candidates = new List<IPAddress>();

    foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
    {
      if (networkInterface.OperationalStatus != OperationalStatus.Up ||
          networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
          IsVirtualInterface(networkInterface.Name))
      {
        continue;
      }

      foreach (var unicast in networkInterface.GetIPProperties().UnicastAddresses)
      {
        var address = unicast.Address;
        if (address.AddressFamily != AddressFamily.InterNetwork ||
            IPAddress.IsLoopback(address) ||
            address.ToString().StartsWith("169.254.", StringComparison.Ordinal))
        {
          continue;
        }

        candidates.Add(address);
      }
    }

    return candidates
      .OrderBy(address => IsPrivate(address) ? 0 : 1)
      .Select(address => address.ToString())
      .FirstOrDefault();
  }

  private static bool IsPrivate(IPAddress address)
  {
    var bytes = address.GetAddressBytes();
    return bytes[0] == 10 ||
           (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
           (bytes[0] == 192 && bytes[1] == 168);
  }

  private static bool IsVirtualInterface(string name) =>
    name.StartsWith("docker", StringComparison.OrdinalIgnoreCase) ||
    name.StartsWith("veth", StringComparison.OrdinalIgnoreCase) ||
    name.StartsWith("br-", StringComparison.OrdinalIgnoreCase) ||
    name.StartsWith("virbr", StringComparison.OrdinalIgnoreCase) ||
    name.StartsWith("tun", StringComparison.OrdinalIgnoreCase) ||
    name.StartsWith("tap", StringComparison.OrdinalIgnoreCase);

  private static bool IsRunningInContainer() =>
    string.Equals(
      Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
      "true",
      StringComparison.OrdinalIgnoreCase) ||
    File.Exists("/.dockerenv");
}
