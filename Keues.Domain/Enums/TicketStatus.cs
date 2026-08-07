namespace Keues.Domain.Enums;

public enum TicketStatus
{
  /// <summary>
  /// El ticket esta en espera de ser atendido
  /// </summary>
  Waiting = 0,
    
  /// <summary>
  /// El ticket esta siendo atendido
  /// </summary>
  InProgress = 1,
    
  /// <summary>
  /// El ticket ya fue atendido
  /// </summary>
  Attended = 2,
  
  /// <summary>
  /// El cliente se va, se cierra la tienda, etc
  /// </summary>
  Canceled = 3,
}