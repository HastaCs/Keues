using Keues.API.Hubs;
using Keues.API.Responses;
using Keues.API.Responses.Devices;
using Keues.Application.DeviceRegistry;
using Keues.Application.Features.Devices;
using Keues.Application.Features.Devices.DeleteDevice;
using Keues.Application.Features.Devices.GetDevice;
using Keues.Application.Features.Devices.GetDevices;
using Keues.Application.Features.Devices.IsActiveDevice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Keues.API.Controllers
{
  /// <summary>
  /// Query of devices connected to the system (ticket machines, counters and monitors).
  /// </summary>
  [Route("api/[controller]")]
  [ApiController]
  public class DevicesController : ControllerBase
  {
    private readonly ConnectedDeviceRegistry _devicesRegistry;
    private readonly DeviceUseCases _deviceUseCases;
    private readonly IHubContext<DeviceHub> _hubContext;

    public DevicesController(ConnectedDeviceRegistry devicesRegistry, DeviceUseCases deviceUseCases,
      IHubContext<DeviceHub> hubContext)
    {
      _devicesRegistry = devicesRegistry;
      _deviceUseCases = deviceUseCases;
      _hubContext = hubContext;
    }

    /// <summary>
    /// Gets the registered devices, including their real-time connection status.
    /// </summary>
    /// <param name="command">Optional filters: LocationId and DeviceType.</param>
    /// <returns>List of devices with connection status.</returns>
    /// <response code="200">List of devices.</response>
    /// <response code="400">Validation or business rule error.</response>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    [ProducesResponseType(typeof(DataResponse<IEnumerable<DeviceResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDevices([FromQuery] GetDevicesCommand command)
    {
      try
      {
        var devices = await _deviceUseCases.GetDevices.Handle(command);
        var deciesWithConnection = devices.Select(d =>
        {
          var isConnected = _devicesRegistry.IsConnected(d.Id);
          return new DeviceResponse(d.Id, d.Name, d.Type, d.LastConnection, isConnected, d.IsActive);
        }).ToList();

        return Ok(new DataResponse<IEnumerable<DeviceResponse>>(deciesWithConnection));
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Deletes a device from the database. Only allowed if it is disconnected.
    /// </summary>
    /// <param name="id">Identifier of the device.</param>
    /// <response code="200">Device deleted.</response>
    /// <response code="400">Validation or business rule error (does not exist or is still connected).</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id)
    {
      try
      {
        await _deviceUseCases.Delete.Handle(new DeleteDeviceCommand(id));
        return Ok();
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Updates the active status of a device. 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isActive"></param>
    /// <returns></returns>
    [HttpPatch("{id:guid}/isActive")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IsActive(Guid id, bool isActive)
    {
      try
      {
        var command = new IsActiveCommand(isActive, id);
        await _deviceUseCases.IsActive.Handle(command);

        var active = new { active = isActive };
        var connectionId = _devicesRegistry.GetConnectionId(id);
        await _hubContext.Clients.Client(connectionId).SendAsync("IsActive", active);
        return Ok();
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    [HttpGet("{id:guid}/isActive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetIsActive(Guid id)
    {
      try
      {
        var device = await _deviceUseCases.GetDevice.Handle(new GetDeviceQuery(id));
        if(device==null)
          return NotFound(new ErrorResponse($"Device with id {id} not found."));   
        return Ok(new { active = device.IsActive });
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }
  }
}