using Keues.Domain.Enums;

namespace Keues.API.Hubs;

public static class DeviceGroups
{
  
  public static string Build(Guid locationId, DeviceType type, Guid flowId) =>
    $"locationId:{locationId}:typeDevice:{type}:flowId:{flowId}";

  public static string TicketMachine(Guid locationId, Guid flowId) =>
    Build(locationId, DeviceType.TicketMachine, flowId);
  
  public static string Monitor(Guid locationId, Guid flowId) =>
    Build(locationId, DeviceType.Monitor, flowId);
}
