namespace Keues.Domain.Events;

public static class EventTypes
{
  public static class Ticket
  {
    public const string Created = "Ticket.Created";
    public const string Attended = "Ticket.Attended";
    public const string Canceled = "Ticket.Canceled";
    public const string Transferred = "Ticket.Transferred";
    public const string Called = "Ticket.Called";
  }
}