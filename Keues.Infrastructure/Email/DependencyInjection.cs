using Keues.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Keues.Infrastructure.Email;

public static class DependencyInjection
{
  public static IServiceCollection AddEmailServices(
    this IServiceCollection services,
    IConfiguration configuration)
  {
    services.Configure<EmailOptions>(
      configuration.GetSection("Email"));

    services.AddScoped<SmtpEmailService>();
    services.AddHttpClient<MailjetEmailService>(
      client => client.Timeout = TimeSpan.FromSeconds(15));

    services.AddScoped<IEmailService>(sp =>
    {
      var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailOptions>>().Value;

      return options.Provider.Trim().ToLowerInvariant() switch
      {
        "smtp" => sp.GetRequiredService<SmtpEmailService>(),
        "mailjet" => sp.GetRequiredService<MailjetEmailService>(),
        _ => throw new InvalidOperationException(
          $"Email provider '{options.Provider}' is not supported. Supported providers: smtp, mailjet.")
      };
    });

    return services;
  }
}