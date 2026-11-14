using System.Text.Json;

namespace Keues.Application.Features.Flows;

public static class QrFlowJsonParser
{
  public static bool TryGetFirstQueueId(string? flowJson, out Guid queueId)
  {
    queueId = Guid.Empty;

    if (string.IsNullOrWhiteSpace(flowJson))
    {
      return false;
    }

    try
    {
      using var document = JsonDocument.Parse(flowJson);
      var root = document.RootElement;

      JsonElement items;
      if (root.ValueKind == JsonValueKind.Array)
      {
        items = root;
      }
      else if (root.ValueKind == JsonValueKind.Object &&
               root.TryGetProperty("menuItems", out var menuItems) &&
               menuItems.ValueKind == JsonValueKind.Array)
      {
        items = menuItems;
      }
      else
      {
        return false;
      }

      foreach (var item in items.EnumerateArray())
      {
        if (item.ValueKind != JsonValueKind.Object)
        {
          continue;
        }

        if (item.TryGetProperty("nodeType", out var nodeType))
        {
          if (nodeType.ValueKind != JsonValueKind.String ||
              !string.Equals(nodeType.GetString(), "ticket", StringComparison.OrdinalIgnoreCase))
          {
            continue;
          }
        }

        if (!item.TryGetProperty("queueId", out var queueIdElement) ||
            queueIdElement.ValueKind != JsonValueKind.String)
        {
          continue;
        }

        if (Guid.TryParse(queueIdElement.GetString(), out var parsed) && parsed != Guid.Empty)
        {
          queueId = parsed;
          return true;
        }
      }

      return false;
    }
    catch (JsonException)
    {
      return false;
    }
  }
}
