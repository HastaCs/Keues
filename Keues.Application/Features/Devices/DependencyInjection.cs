using Keues.Application.Features.Devices.CreateDevice;
using Keues.Application.Features.Devices.DeleteDevice;
using Keues.Application.Features.Devices.GetDevice;
using Keues.Application.Features.Devices.GetDevices;
using Keues.Application.Features.Devices.IsActiveDevice;
using Microsoft.Extensions.DependencyInjection;

namespace Keues.Application.Features.Devices;

public static class DependencyInjection
{
  public static IServiceCollection AddDeviceUseCases(this IServiceCollection services)
  {
    services.AddScoped<CreateUpdateDeviceHandler>();
    services.AddScoped<GetDevicesHandler>();
    services.AddScoped<DeleteDeviceHandler>();
    services.AddScoped<IsActiveDeviceHandler>();
    services.AddScoped<GetDeviceHandler>();
    services.AddScoped<DeviceUseCases>();
    return services;
  }
}