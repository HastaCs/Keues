using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Keues.API.Common;

public static class RuntimeConfigStore
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
  };

  public static RuntimeConfig LoadOrCreate(string filePath)
  {
    var raw = string.Empty;
    if (File.Exists(filePath))
    {
      try
      {
        raw = File.ReadAllText(filePath);
        var existing = JsonSerializer.Deserialize<RuntimeConfig>(raw, JsonOptions);

        if (existing is not null && !string.IsNullOrWhiteSpace(existing.JwtKey))
        {
          // Si es un config antiguo sin las secciones nuevas, se amplía con la
          // plantilla para que el usuario pueda rellenarlas a mano.
          if (!Contains(raw, "dashboardUrl") || !Contains(raw, "email"))
          {
            Save(filePath, existing);
          }

          return existing;
        }
      }
      catch (JsonException)
      {
        // Fichero corrupto: se regenera.
      }
    }

    var config = new RuntimeConfig
    {
      JwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
    };

    Save(filePath, config);
    return config;
  }

  private static bool Contains(string json, string property)
  {
    return !string.IsNullOrEmpty(json) && json.Contains($"\"{property}\"");
  }

  public static void Save(string filePath, RuntimeConfig config)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
    File.WriteAllText(filePath, JsonSerializer.Serialize(config, JsonOptions));
  }
}