using Keues.API.Requests.Locations;
using Keues.API.Responses;
using Keues.API.Responses.Locations;
using Keues.Application.Features.Locations;
using Keues.Application.Features.Locations.CreateLocation;
using Keues.Application.Features.Locations.DeleteLocation;
using Keues.Application.Features.Locations.UpdateLocation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Keues.API.Controllers
{
  /// <summary>
  /// Management of locations (businesses, clinics, etc.) where service is provided.
  /// </summary>
  [Route("api/[controller]")]
  [ApiController]
  public class LocationsController : ControllerBase
  {
    private readonly LocationUseCases _locationUseCases;

    public LocationsController(LocationUseCases locationUseCases)
    {
      _locationUseCases = locationUseCases;
    }

    /// <summary>
    /// Creates a new location.
    /// </summary>
    /// <param name="request">Data of the location to create.</param>
    /// <returns>The created location.</returns>
    /// <response code="201">Location created.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(LocationRequest request)
    {
      try
      {
        var command = new CreateLocationCommand()
        {
          Name = request.Name,
          Description = request.Description,
          Color = request.Color
        };
        var location = await _locationUseCases.Create.Handle(command);
        var response = LocationResponse.FromLocation(location);
        return CreatedAtAction(nameof(Get), new { id = location.Id }, response);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Updates an existing location.
    /// </summary>
    /// <param name="id">Identifier of the location.</param>
    /// <param name="request">Data to update.</param>
    /// <returns>The updated location.</returns>
    /// <response code="200">Location updated.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, LocationRequest request)
    {
      try
      {
        var command = new UpdateLocationCommand()
        {
          Id = id,
          Name = request.Name,
          Description = request.Description,
          Color = request.Color
        };
        var location = await _locationUseCases.Update.Handle(command);
        var response = LocationResponse.FromLocation(location);
        return Ok(response);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Deletes a location.
    /// </summary>
    /// <param name="id">Identifier of the location.</param>
    /// <response code="204">Location deleted.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id)
    {
      try
      {
        await _locationUseCases.Delete.Handle(new DeleteLocationCommand(id));
        return NoContent();
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Retrieves a location by its identifier.
    /// </summary>
    /// <param name="id">Identifier of the location.</param>
    /// <returns>The requested location.</returns>
    /// <response code="200">Location found.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get(Guid id)
    {
      try
      {
        var location = await _locationUseCases.Get.Handle(id);
        var response = LocationResponse.FromLocation(location);
        return Ok(response);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Retrieves all locations.
    /// </summary>
    /// <returns>List of locations.</returns>
    /// <response code="200">List of locations.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(DataResponse<List<LocationResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll()
    {
      try
      {
        var locations = await _locationUseCases.GetAll.Handle();
        var response = locations.Select(LocationResponse.FromLocation).ToList();
        return Ok(new DataResponse<List<LocationResponse>>(response));
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }
  }
}
