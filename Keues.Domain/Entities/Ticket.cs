using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Keues.Domain.Enums;
using Keues.Domain.Events;

namespace Keues.Domain.Entities;

/// <summary>
/// Los tickets que sacan los clientes de las maquinas
/// </summary>
public class Ticket
{
  public static Ticket Create(string code, Guid queueId, Guid flowId)
  {
    return new Ticket()
    {
      Id = Guid.NewGuid(),
      CreatedAt = DateTime.UtcNow,
      Code = code,
      Status = TicketStatus.Waiting,
      QueueId = queueId,
      FlowId = flowId
    };
  }

  public void Attend()
  {
    Status = TicketStatus.Attended;
    AttendedAt = DateTime.UtcNow; 
  }

  public void Cancel()
  {
    Status = TicketStatus.Canceled;
    CanceledAt = DateTime.UtcNow;
  }

  public void Waiting()
  {
    Status = TicketStatus.Waiting;
    CalledAt = null;
    Counter = null;
    AttendedAt = null;
    CanceledAt = null;
  }

  [Key] public Guid Id { get; set; }

  /// <summary>
  /// Estado en que se encuentra el ticket
  /// </summary>
  public TicketStatus Status { get; set; } = TicketStatus.Waiting;

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public DateTime? CalledAt { get; set; }

  public DateTime? AttendedAt { get; set; }

  public DateTime? CanceledAt { get; set; }

  public Guid QueueId { get; set; }

  public Queue Queue { get; set; }

  /// <summary>
  /// El puesto que gestiona el Ticket
  /// </summary>
  public Counter? Counter { get; set; }

  public Guid? CounterId { get; set; }

  /// <summary>
  /// Código del ticket  ABNumber  TNumber
  /// </summary>
  public string Code { get; set; }

  public Guid FlowId { get; set; }
  public Flow Flow { get; set; }

  public ICollection<TicketHistory> TicketHistories { get; set; } = new List<TicketHistory>();
}