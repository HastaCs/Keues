using System.Security.Cryptography;
using System.Text;
using Keues.Application.Common;
using Keues.Infrastructure.Authorization;
using Microsoft.Extensions.Options;

namespace Keues.Infrastructure.Qr;

public sealed class QrTokenService : IQrTokenService
{
  private readonly byte[] _key;
  private readonly QrOptions _options;

  public QrTokenService(IOptions<JwtOptions> jwtOptions, IOptions<QrOptions> qrOptions)
  {
    _options = qrOptions.Value;
    _key = SHA256.HashData(Encoding.UTF8.GetBytes($"keues-qr-v1:{jwtOptions.Value.Key}"));
  }

  public QrTokenPeriod GetPeriod(DateTimeOffset now)
  {
    if (_options.SlotSeconds <= 0)
    {
      throw new InvalidOperationException("Qr:SlotSeconds must be greater than zero.");
    }

    var slotStartUnix = now.ToUnixTimeSeconds() / _options.SlotSeconds * _options.SlotSeconds;
    var start = DateTimeOffset.FromUnixTimeSeconds(slotStartUnix);
    return new QrTokenPeriod(start, start.AddSeconds(_options.SlotSeconds));
  }

  public string Create(Guid flowId, DateTimeOffset now)
  {
    var period = GetPeriod(now);
    return ToBase64Url(CreateForSlot(flowId, period.Start));
  }

  public bool Validate(Guid flowId, string? token, DateTimeOffset now)
  {
    if (string.IsNullOrWhiteSpace(token) || !TryFromBase64Url(token, out var provided))
    {
      return false;
    }

    var current = GetPeriod(now);
    var previous = GetPeriod(current.Start.AddTicks(-1));

    var currentHash = CreateForSlot(flowId, current.Start);
    var previousHash = CreateForSlot(flowId, previous.Start);

    return CryptographicOperations.FixedTimeEquals(provided, currentHash) ||
           CryptographicOperations.FixedTimeEquals(provided, previousHash);
  }

  private byte[] CreateForSlot(Guid flowId, DateTimeOffset slotStart)
  {
    var payload = Encoding.UTF8.GetBytes($"{flowId:N}:{slotStart.ToUnixTimeSeconds()}");
    return HMACSHA256.HashData(_key, payload);
  }

  private static string ToBase64Url(byte[] bytes) =>
    Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

  private static bool TryFromBase64Url(string token, out byte[] bytes)
  {
    var base64 = token.Replace('-', '+').Replace('_', '/');
    switch (base64.Length % 4)
    {
      case 2:
        base64 += "==";
        break;
      case 3:
        base64 += "=";
        break;
      case 1:
        bytes = Array.Empty<byte>();
        return false;
    }

    try
    {
      bytes = Convert.FromBase64String(base64);
      return true;
    }
    catch (FormatException)
    {
      bytes = Array.Empty<byte>();
      return false;
    }
  }
}
