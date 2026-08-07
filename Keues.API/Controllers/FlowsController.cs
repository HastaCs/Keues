
using Keues.API.Responses;
using Keues.Application.Features.Flows;
using Keues.Application.Features.Flows.CreateFlow;
using Keues.Application.Features.Flows.DeleteFlow;
using Keues.Application.Features.Flows.GetAllFlows;
using Keues.Application.Features.Flows.GetFlow;
using Keues.Application.Features.Flows.UpdateFlow;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Keues.API.Controllers
{
  /// <summary>
  /// Management of service flows (how tickets are handled at a location).
  /// </summary>
  [Route("api/[controller]")]
  [ApiController]
  public class FlowsController : ControllerBase
  {
    private  FlowsUseCases _flowsUseCases;

    public FlowsController(FlowsUseCases flowsUseCases)
    {
      _flowsUseCases = flowsUseCases;
    }

    /// <summary>
    /// Retrieves all flows, optionally filtered by location.
    /// </summary>
    /// <param name="locationId">Optional filter by location.</param>
    /// <returns>List of flows.</returns>
    /// <response code="200">List of flows.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(DataResponse<List<FlowBaseResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? locationId)
    {
      try
      {
        var command = new GetAllFlowsCommand(locationId);
        var flow=await _flowsUseCases.GetAll.Handle(command);
        return Ok(new DataResponse<List<FlowBaseResponse>>(flow.ToList()));
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Retrieves a flow by its identifier.
    /// </summary>
    /// <param name="id">Identifier of the flow.</param>
    /// <returns>The requested flow.</returns>
    /// <response code="200">Flow found.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FlowBaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get(Guid id)
    {
      try
      {
        var flow = await _flowsUseCases.Get.Handle(new GetFlowCommand(id));

        return Ok(flow);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Creates a new service flow.
    /// </summary>
    /// <param name="command">Data of the flow to create.</param>
    /// <returns>The created flow.</returns>
    /// <response code="200">Flow created.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(FlowBaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateFlowCommand command)
    {
      try
      {
        var flow = await _flowsUseCases.Create.Handle(command);
        return Ok(flow);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Updates an existing flow.
    /// </summary>
    /// <param name="id">Identifier of the flow.</param>
    /// <param name="command">Data to update. The Id in the body is overridden by the path one.</param>
    /// <returns>The updated flow.</returns>
    /// <response code="200">Flow updated.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UpdateFlowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id,UpdateFlowCommand command)
    {
      try
      {
        var commandCOpy= command with { Id = id };
        var flow = await _flowsUseCases.Update.Handle(commandCOpy);
        return Ok(flow);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Deletes a flow.
    /// </summary>
    /// <param name="id">Identifier of the flow.</param>
    /// <response code="200">Flow deleted.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id)
    {
      try
      {
        await _flowsUseCases.Delete.Handle(new DeleteFlowCommand(id));
        return Ok();
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }
  }
}
