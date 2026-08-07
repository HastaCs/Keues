using Keues.API.Requests.Queues;
using Keues.API.Responses;
using Keues.Application.Features.Queues;
using Keues.Application.Features.Queues.CreateNewTicket;
using Keues.Application.Features.Queues.CreateQueue;
using Keues.Application.Features.Queues.DeleteQueue;
using Keues.Application.Features.Queues.GetAllQueues;
using Keues.Application.Features.Queues.GetQueue;
using Keues.Application.Features.Queues.UpdateQueue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Keues.API.Controllers
{
  /// <summary>
  /// Management of queues (ticket types) and issuance of new tickets from a queue.
  /// </summary>
  [Route("api/[controller]")]
  [ApiController]
  public class QueuesController(QueuesUseCases useCases) : ControllerBase
  {
    /// <summary>
    /// Creates a new queue.
    /// </summary>
    /// <param name="command">Data of the queue to create.</param>
    /// <returns>The created queue.</returns>
    /// <response code="200">Queue created.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(QueueBaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateQueueCommand command)
    {
      try
      {
        var ticketType = await useCases.Create.Handle(command);
        return Ok(ticketType);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Updates an existing queue.
    /// </summary>
    /// <param name="id">Identifier of the queue.</param>
    /// <param name="command">Data to update. The Id in the body is overridden by the path one.</param>
    /// <returns>The updated queue.</returns>
    /// <response code="200">Queue updated.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(QueueBaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, UpdateQueueCommand command)
    {
      try
      {
        var commandCopy = command with { Id = id };
        var ticketType = await useCases.Update.Handle(commandCopy);
        return Ok(ticketType);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Deletes a queue.
    /// </summary>
    /// <param name="id">Identifier of the queue.</param>
    /// <response code="200">Queue deleted.</response>
    /// <response code="400">Validation or business rule error.</response>
    // TODO Hacer que los handle devuelvan httpStatuses, mirar libreria de Result<T> de netmentor
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id)
    {
      try
      {
        await useCases.Delete.Handle(new DeleteQueueCommand(id));
        return Ok();
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Retrieves a queue by its identifier.
    /// </summary>
    /// <param name="id">Identifier of the queue.</param>
    /// <returns>The requested queue.</returns>
    /// <response code="200">Queue found.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(QueueBaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get(Guid id)
    {
      try
      {
        var ticketType = await useCases.Get.Handle(new GetQueueCommand(id));
        return Ok(ticketType);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Issues a new ticket in the specified queue. The id of the queue and the flowId of who generates it.
    /// </summary>
    /// <param name="id">Identifier of the queue.</param>
    /// <param name="request">FlowId of the flow that generates the ticket.</param>
    /// <returns>The created ticket.</returns>
    /// <response code="200">Ticket created.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpPost("{id:guid}/new-ticket")]
    [ProducesResponseType(typeof(CreateNewTicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetNewTicket(Guid id, GetNewTicketRequest request)
    {
      try
      {
        var ticket = await useCases.CreateNewTicket.Handle(new CreateNewTicketCommand(id, request.FlowId));
        return Ok(ticket);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Retrieves all queues, optionally filtered by location.
    /// </summary>
    /// <param name="command">Query filters (optional LocationId).</param>
    /// <returns>List of queues.</returns>
    /// <response code="200">List of queues.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(DataResponse<IEnumerable<QueueBaseResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] GetAllQueuesCommand command)
    {
      try
      {
        var ticketTypes = await useCases.GetAll.Handle(command);
        return Ok(new DataResponse<IEnumerable<QueueBaseResponse>>(ticketTypes));
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }
  }
}
