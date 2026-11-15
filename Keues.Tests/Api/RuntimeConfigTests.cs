using Keues.API.Common;
using Xunit;

namespace Keues.Tests.Api;

public class RuntimeConfigTests
{
  [Fact]
  public void ApplyEnvironmentOverrides_uses_qr_url_environment_variable()
  {
    var previous = Environment.GetEnvironmentVariable("KEUES_QR_URL");

    try
    {
      Environment.SetEnvironmentVariable("KEUES_QR_URL", "https://qr.example.com");
      var config = new RuntimeConfig { KeuesQrUrl = "http://config.local" };

      config.ApplyEnvironmentOverrides();

      Assert.Equal("https://qr.example.com", config.KeuesQrUrl);
    }
    finally
    {
      Environment.SetEnvironmentVariable("KEUES_QR_URL", previous);
    }
  }

  [Fact]
  public void ApplyEnvironmentOverrides_keeps_config_value_when_environment_is_empty()
  {
    var previous = Environment.GetEnvironmentVariable("KEUES_QR_URL");

    try
    {
      Environment.SetEnvironmentVariable("KEUES_QR_URL", "");
      var config = new RuntimeConfig { KeuesQrUrl = "http://config.local" };

      config.ApplyEnvironmentOverrides();

      Assert.Equal("http://config.local", config.KeuesQrUrl);
    }
    finally
    {
      Environment.SetEnvironmentVariable("KEUES_QR_URL", previous);
    }
  }

  [Fact]
  public void ResolveQrPublicUrl_prefers_qr_public_url()
  {
    var config = new RuntimeConfig
    {
      DashboardUrl = "https://admin.example.com",
      KeuesQrUrl = "https://qr.example.com"
    };

    Assert.Equal("https://qr.example.com", config.ResolveQrPublicUrl());
  }

  [Fact]
  public void ResolveQrPublicUrl_falls_back_to_dashboard_url()
  {
    var config = new RuntimeConfig
    {
      DashboardUrl = "https://admin.example.com",
      KeuesQrUrl = ""
    };

    Assert.Equal("https://admin.example.com", config.ResolveQrPublicUrl());
  }

  [Fact]
  public void ResolveQrPublicUrl_ignores_loopback_urls()
  {
    var config = new RuntimeConfig
    {
      DashboardUrl = "http://localhost:8080",
      KeuesQrUrl = ""
    };

    Assert.Equal(string.Empty, config.ResolveQrPublicUrl());
  }

  [Fact]
  public void ResolveQrPublicUrl_prefers_an_explicit_loopback_qr_url()
  {
    var config = new RuntimeConfig
    {
      DashboardUrl = "http://192.168.1.50:8080",
      KeuesQrUrl = "http://localhost:8080"
    };

    Assert.Equal("http://localhost:8080", config.ResolveQrPublicUrl());
  }

  [Fact]
  public void LoadOrCreate_adds_qr_public_url_to_a_legacy_config()
  {
    var directory = Path.Combine(
      Path.GetTempPath(),
      "keues-config-tests",
      Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "config.json");

    try
    {
      Directory.CreateDirectory(directory);
      File.WriteAllText(path, LegacyConfigWithoutQrPublicUrl);

      var config = RuntimeConfigStore.LoadOrCreate(path);
      var saved = File.ReadAllText(path);

      Assert.Contains("\"keuesQrUrl\"", saved);
      Assert.Equal("http://dashboard.local", config.DashboardUrl);
    }
    finally
    {
      if (Directory.Exists(directory))
      {
        Directory.Delete(directory, recursive: true);
      }
    }
  }

  [Fact]
  public void LoadOrCreate_preserves_an_existing_qr_public_url()
  {
    var directory = Path.Combine(
      Path.GetTempPath(),
      "keues-config-tests",
      Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "config.json");

    try
    {
      RuntimeConfigStore.Save(path, new RuntimeConfig
      {
        JwtKey = "test-jwt-key",
        DashboardUrl = "http://dashboard.local",
        KeuesQrUrl = "https://qr.example.com"
      });

      var config = RuntimeConfigStore.LoadOrCreate(path);

      Assert.Equal("https://qr.example.com", config.KeuesQrUrl);
    }
    finally
    {
      if (Directory.Exists(directory))
      {
        Directory.Delete(directory, recursive: true);
      }
    }
  }

  private const string LegacyConfigWithoutQrPublicUrl = """
    {
      "jwtKey": "test-jwt-key",
      "dashboardUrl": "http://dashboard.local",
      "email": {
        "provider": "smtp",
        "from": "",
        "fromName": "Keues",
        "smtp": {
          "host": "",
          "port": 587,
          "user": "",
          "password": "",
          "from": "",
          "useTls": true
        },
        "mailjet": {
          "apiKey": "",
          "secretKey": ""
        }
      }
    }
    """;
}
