using Keues.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Keues.Infrastructure.Authorization;

public static class DependencyInjection
{
  public static IServiceCollection AddAuthorizationServices(
    this IServiceCollection services,
    IConfiguration configuration)
  {
    services.Configure<JwtOptions>(
      configuration.GetSection("Jwt"));

    services.AddScoped<IJwtService, JwtService>();

    return services;
  }
}