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
    services.AddScoped<IEmailService>(sp =>
    {
      var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailOptions>>().Value;

      return options.Provider switch
      {
        "smtp" => sp.GetRequiredService<SmtpEmailService>(),
        _ => throw new InvalidOperationException(
          $"Email provider '{options.Provider}' is not supported.")
      };
    });

    return services;
  }
}