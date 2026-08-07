using Keues.API.Responses;
using Keues.API.Responses.Devices;
using Keues.Application.DeviceRegistry;
using Keues.Application.Features.Devices;
using Keues.Application.Features.Devices.DeleteDevice;
using Keues.Application.Features.Devices.GetDevices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        public DevicesController(ConnectedDeviceRegistry devicesRegistry, DeviceUseCases deviceUseCases)
        {
            _devicesRegistry = devicesRegistry;
            _deviceUseCases = deviceUseCases;
        }

        /// <summary>
        /// Gets the registered devices, including their real-time connection status.
        /// </summary>
        /// <param name="command">Optional filters: LocationId and DeviceType.</param>
        /// <returns>List of devices with connection status.</returns>
        /// <response code="200">List of devices.</response>
        /// <response code="400">Validation or business rule error.</response>
        [HttpGet]
        [ProducesResponseType(typeof(DataResponse<IEnumerable<DeviceResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDevices([FromQuery] GetDevicesCommand command)
        {
            try
            {
                var devices = await _deviceUseCases.GetDevices.Handle(command);
                var deciesWithConnection=devices.Select(d =>
                {
                    var isConnected = _devicesRegistry.IsConnected(d.Id);
                    return new DeviceResponse(d.Id, d.Name, d.Type, d.LastConnection,isConnected);
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
    }
}
