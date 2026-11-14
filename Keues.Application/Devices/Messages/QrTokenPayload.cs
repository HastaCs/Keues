namespace Keues.Application.DeviceRegistry.Messages;

public record QrTokenPayload(
  Guid FlowId,
  Guid LocationId,
  string Token,
  DateTimeOffset RotatesAt,
  DateTimeOffset ExpiresAt,
  string Url);
