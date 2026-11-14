namespace Keues.Application.Common;

public record QrTokenPeriod(DateTimeOffset Start, DateTimeOffset End)
{
  public TimeSpan Duration => End - Start;
}
