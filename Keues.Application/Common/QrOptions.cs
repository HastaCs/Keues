namespace Keues.Application.Common;

public sealed class QrOptions
{
  public int SlotSeconds { get; set; } = 300;

  public string PublicBaseUrl { get; set; } = string.Empty;
}
