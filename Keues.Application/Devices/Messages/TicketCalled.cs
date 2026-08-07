namespace Keues.Application.DeviceRegistry.Messages;


public record TicketCalled(Guid? TicketId,string TicketCode, string CounterCode);