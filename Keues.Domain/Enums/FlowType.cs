namespace Keues.Domain.Enums;

public enum FlowType
{/// <summary>
 /// El cliente obtiene un numero de ticket desde un terminal
 /// </summary>
  TicketMachine, 
  /// <summary>
  /// No hay tickets, el puesto indica que está libre, estilo Carrefour
  /// </summary>
  SetFree,       
  /// <summary>
  /// LLamada manual como en pescaderia,fruteria,, va subiendo 1 arriba, o 1 abajo,
  /// EL cliente coge su ticket de una maquina de papel o de un terminal
  /// </summary>
  ManualCall     
}