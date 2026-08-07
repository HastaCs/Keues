using System.Text.Json;
using System.Text.Json.Serialization;

namespace Keues.Tests.Infrastructure;

internal static class JsonHelper
{
  public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter() }
  };
}
