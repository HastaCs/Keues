using Keues.API.Responses;
using Keues.Application.Features.Tickets;
using Keues.Application.Features.Tickets.GetAllTickets;
using Keues.Application.Features.Tickets.GetTicket;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Keues.API.Controllers
{
  /// <summary>
  /// Query of tickets issued in the queues.
  /// </summary>
  [Route("api/[controller]")]
  [ApiController]
  public class TicketsController(TicketsUseCases ticketsUseCases) : ControllerBase
  {
    /// <summary>
    /// Gets the tickets, filterable by status, date range, code, location, or queue.
    /// </summary>
    /// <param name="command">Query filters.</param>
    /// <returns>List of tickets.</returns>
    /// <response code="200">List of tickets.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(DataResponse<IEnumerable<GetTicketResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get([FromQuery] GetAllTicketsCommand command)
    {
      try
      {
        var tickets =await ticketsUseCases.GetAllTickets.Handle(command);
        return Ok(new DataResponse<IEnumerable<GetTicketResponse>>(tickets));
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Gets a ticket by its identifier.
    /// </summary>
    /// <param name="id">Ticket identifier.</param>
    /// <returns>The requested ticket.</returns>
    /// <response code="200">Ticket found.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetTicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get(Guid id)
    {
      try
      {
        var ticket=await ticketsUseCases.GetTicket.Handle(new GetTicketCommand(id));
        return Ok(ticket);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }
  }
}
