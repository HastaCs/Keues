namespace Keues.Application.Common;

public interface IQrTokenService
{
  QrTokenPeriod GetPeriod(DateTimeOffset now);

  string Create(Guid flowId, DateTimeOffset now);

  bool Validate(Guid flowId, string? token, DateTimeOffset now);
}
