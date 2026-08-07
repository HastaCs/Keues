using System.ComponentModel.DataAnnotations;

namespace Keues.Domain.Entities;


//TODO Buscar toso lso ticketTYpes y renombrarlos a queue
/// <summary>
/// Las distintas colas que existen
/// </summary>
/// TODO Renombrar esto a Queue o algo parecido
public class Queue
{
  private Queue() { }
  public static Queue Create(string name, string code, int? maxValue,string description,Guid locationId,int priority,int weight,int agingIntervalMinutes,int maxAgingBonus,string color)
  {
    return new Queue()
    {
      Id = Guid.NewGuid(),
      Name = name,
      Code = code,
      MaxValue = maxValue,
      Description = description,
      LocationId = locationId,
      Priority = priority,
      Weight = weight,
      AgingIntervalMinutes = agingIntervalMinutes,
      MaxAgingBonus = maxAgingBonus,
      Color= color,
    
    };
  }
  
  public Ticket CreateNewTicket(Guid FlowId)
  {
    var number = NextNumber;
    NextNumber++;
    if(MaxValue.HasValue && NextNumber > MaxValue.Value)
    {
      NextNumber = 1;
    }
    return Ticket.Create( Code+number.ToString("000"),Id,FlowId);
  }
  
  [Key]
  public Guid Id { get; set; }

  /// <summary>
  /// Para que el usuario lo identifique en el dashboard
  /// </summary>
  public string Name { get; set; } = "";
  
  
  /// <summary>
  /// Descripcion para el usuario
  /// </summary>
  public string Description { get; set; } = "";
  
  /// <summary>
  /// Para saber cual es el siguiente numero que va a sacar la maquina en el ticket
  /// </summary>
  public int NextNumber { get; set; } = 1;
  
  /// <summary>
  /// EL numero del ticket se resetea a 0 una vez llega a esta valor
  /// </summary>
  public int? MaxValue { get; set; }

  /// <summary>
  /// Texto que va junto al número del ticket
  /// </summary>
  public string Code { get; set; } = "";
  
  public DateTime CreatedAt { get; } = DateTime.UtcNow;
 
  /// <summary>
  /// Para el soft delete y no eliminarlo de la base de datos
  /// </summary>
  public DateTime? RemovedAt { get; set; } = null;
  
  
  public Guid LocationId { get;  set; }
  public Location Location { get;  set; } = null!;
  
  
  //Counters que pueden llamarlo
  public ICollection<Counter> Counters { get; set; } = [];
  
  /// <summary>
  /// Prioridad del ticket
  /// </summary>
  public int Priority { get; set; } = 0;

  /// <summary>
  /// Lo que dice el peso es:
  ///"Por cada ticket de B, intenta atender tres de A."
  /// </summary>
  public int Weight { get; set; } = 0;
  
  /// <summary>
  /// Cada X minutos sube de prioridad
  /// </summary>
  public int AgingIntervalMinutes { get; set; } = 0;

  /// <summary>
  /// La prioridad maxima a la que puede subir
  /// </summary>
  public int MaxAgingBonus { get; set; } = 0;
  
  public string Color { get; set; } = "blue";


}