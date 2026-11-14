using Keues.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Keues.Infrastructure.Qr;

public static class DependencyInjection
{
  public static IServiceCollection AddQrServices(
    this IServiceCollection services,
    string publicBaseUrl)
  {
    services.Configure<QrOptions>(options => options.PublicBaseUrl = publicBaseUrl);
    services.AddSingleton<IQrTokenService, QrTokenService>();

    return services;
  }
}
