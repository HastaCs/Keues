using Keues.API.Responses;
using Keues.Application.Features.Tickets;
using Keues.Application.Features.Tickets.GetAllTickets;
using Keues.Application.Features.Tickets.GetTicket;
using Keues.Application.Features.Tickets.GetTicketHistory;

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
        var result = await ticketsUseCases.GetAllTickets.Handle(command);
        var pagination = new Pagination(result.Page, result.Limit, result.Total, result.TotalPages);
        return Ok(new DataResponse<IEnumerable<GetTicketResponse>>(result.Tickets, pagination));
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
    
    /// <summary>
    /// Gets the history of a ticket by its identifier.
    /// </summary>
    /// <param name="id">Ticket identifier.</param>
    /// <returns>List of ticket history events.</returns>
    /// <response code="200">Ticket history found.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(IEnumerable<GetTicketHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHistory(Guid id)
    {
      try
      {
        var histories = await ticketsUseCases.GetTicketHistory.Handle(new GetTicketRequest(id));
        return Ok(histories);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }
  }
}
