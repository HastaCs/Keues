using Keues.Application.Features.Devices.CreateDevice;
using Keues.Application.Features.Devices.DeleteDevice;
using Keues.Application.Features.Devices.GetDevices;
using Microsoft.Extensions.DependencyInjection;

namespace Keues.Application.Features.Devices;

public static class DependencyInjection
{
  public static IServiceCollection AddDeviceUseCases(this IServiceCollection services)
  {
    services.AddScoped<CreateUpdateDeviceHandler>();
    services.AddScoped<GetDevicesHandler>();
    services.AddScoped<DeleteDeviceHandler>();
    services.AddScoped<DeviceUseCases>();
    return services;
  }
}