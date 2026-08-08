using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Keues.API.Common;

/// <summary>
/// Serializes every DateTime as UTC with a trailing "Z" and treats
/// Unspecified values as UTC (SQLite stores them without a timezone).
/// </summary>
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
  public override DateTime Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options)
  {
    var value = reader.GetDateTime();

    return value.Kind == DateTimeKind.Unspecified
      ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
      : value.ToUniversalTime();
  }

  public override void Write(
    Utf8JsonWriter writer,
    DateTime value,
    JsonSerializerOptions options)
  {
    var utc = value.Kind switch
    {
      DateTimeKind.Utc => value,
      DateTimeKind.Local => value.ToUniversalTime(),
      _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    writer.WriteStringValue(utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture));
  }
}
