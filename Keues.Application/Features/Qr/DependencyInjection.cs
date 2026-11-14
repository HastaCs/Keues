using Microsoft.Extensions.DependencyInjection;

namespace Keues.Application.Features.Qr;

public static class DependencyInjection
{
  public static IServiceCollection AddQrUseCases(this IServiceCollection services)
  {
    services.AddScoped<QrFlowResolver>();
    services.AddScoped<QrTokenPayloadFactory>();
    services.AddScoped<QrTokenRotationService>();

    return services;
  }
}
