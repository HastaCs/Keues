namespace Keues.Domain.Entities;

public class WebhooksConfig
{
  public Guid Id { get; set; } = Guid.NewGuid();
  public string Url { get; set; } = string.Empty;
  /// <summary>
  /// Estos eventos son los de la clase EventTypes, y se pueden suscribir a los eventos que se quieran recibir en el webhook.
  /// </summary>
  public List<string> Events { get; set; } = new ();
  
  public bool Enabled { get; set; } = false;
  
  public string Key { get; set; } =string.Empty;
}