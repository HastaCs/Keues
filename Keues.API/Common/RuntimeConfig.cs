using Microsoft.Extensions.Configuration;

namespace Keues.API.Common;

public class RuntimeConfig
{
  public string JwtKey { get; set; } = string.Empty;

  public string DashboardUrl { get; set; } = string.Empty;

  public EmailConfig Email { get; set; } = new();

  public void ApplyEnvironmentOverrides()
  {
    DashboardUrl = EnvOr("KEUES_DASHBOARD_URL", DashboardUrl);
    JwtKey = EnvOr("KEUES_JWT_KEY", JwtKey);
    Email.Provider = EnvOr("KEUES_EMAIL_PROVIDER", Email.Provider);
    Email.From = EnvOr("KEUES_EMAIL_FROM", Email.From);
    Email.FromName = EnvOr("KEUES_EMAIL_FROM_NAME", Email.FromName);
    Email.Smtp.Host = EnvOr("KEUES_SMTP_HOST", Email.Smtp.Host);
    Email.Smtp.Port = EnvIntOr("KEUES_SMTP_PORT", Email.Smtp.Port);
    Email.Smtp.User = EnvOr("KEUES_SMTP_USER", Email.Smtp.User);
    Email.Smtp.Password = EnvOr("KEUES_SMTP_PASSWORD", Email.Smtp.Password);
    // Kept for backwards compatibility; used as an SMTP-only override.
    Email.Smtp.From = EnvOr("KEUES_SMTP_FROM", Email.Smtp.From);
    Email.Smtp.UseTls = EnvBoolOr("KEUES_SMTP_USE_TLS", Email.Smtp.UseTls);
    Email.Mailjet.ApiKey = EnvOr("KEUES_MAILJET_API_KEY", Email.Mailjet.ApiKey);
    Email.Mailjet.SecretKey = EnvOr("KEUES_MAILJET_SECRET_KEY", Email.Mailjet.SecretKey);
  }

  public void BindToConfiguration(ConfigurationManager configuration)
  {
    configuration["Jwt:Key"] = JwtKey;
    MapToConfiguration(configuration, "Email:Provider", Email.Provider);
    MapToConfiguration(configuration, "Email:From", Email.From);
    MapToConfiguration(configuration, "Email:FromName", Email.FromName);
    MapToConfiguration(configuration, "Email:Smtp:Host", Email.Smtp.Host);
    MapToConfiguration(configuration, "Email:Smtp:Port", Email.Smtp.Port.ToString());
    MapToConfiguration(configuration, "Email:Smtp:User", Email.Smtp.User);
    MapToConfiguration(configuration, "Email:Smtp:Password", Email.Smtp.Password);
    MapToConfiguration(configuration, "Email:Smtp:From", Email.Smtp.From);
    MapToConfiguration(configuration, "Email:Smtp:UseTls", Email.Smtp.UseTls.ToString());
    MapToConfiguration(configuration, "Email:Mailjet:ApiKey", Email.Mailjet.ApiKey);
    MapToConfiguration(configuration, "Email:Mailjet:SecretKey", Email.Mailjet.SecretKey);
  }

  private static string EnvOr(string name, string current) =>
    !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name))
      ? Environment.GetEnvironmentVariable(name)!
      : current;

  private static int EnvIntOr(string name, int current) =>
    int.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : current;

  private static bool EnvBoolOr(string name, bool current) =>
    bool.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : current;

  private static void MapToConfiguration(ConfigurationManager configuration, string key, string value)
  {
    configuration[key] = value;
  }

  public class EmailConfig
  {
    public string Provider { get; set; } = "smtp";

    public string From { get; set; } = string.Empty;

    public string FromName { get; set; } = "Keues";

    public SmtpConfig Smtp { get; set; } = new();

    public MailjetConfig Mailjet { get; set; } = new();
  }

  public class MailjetConfig
  {
    public string ApiKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;
  }

  public class SmtpConfig
  {
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string From { get; set; } = string.Empty;

    public bool UseTls { get; set; } = true;
  }
}
