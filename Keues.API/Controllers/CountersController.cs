using System.Configuration;
using Keues.API.Hubs;
using Keues.API.Requests.Counters;
using Keues.API.Responses;
using Keues.Application.DeviceRegistry.Messages;
using Keues.Application.Features.Counters;
using Keues.Application.Features.Counters.AttendTicket;
using Keues.Application.Features.Counters.CallNextTicket;
using Keues.Application.Features.Counters.CancelTicket;
using Keues.Application.Features.Counters.CreateCounter;
using Keues.Application.Features.Counters.DeleteCounter;
using Keues.Application.Features.Counters.GetAllCounters;
using Keues.Application.Features.Counters.GetCounter;
using Keues.Application.Features.Counters.GetQueues;
using Keues.Application.Features.Counters.TransferTicket;
using Keues.Application.Features.Counters.UpdateCounter;
using Keues.Application.Features.Tickets;
using Keues.Application.Features.Tickets.GetTicket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

//TODO Hacer los request en vez de los commands
namespace Keues.API.Controllers
{
  /// <summary>
  /// Management of service desks (counters) and their ticket operations:
  /// call the next ticket, attend, cancel, manual call, and free-desk notification.
  /// </summary>
  [Route("api/[controller]")]
  [ApiController]
  public class CountersController : ControllerBase
  {
    private CounterUseCases _counterUseCases;
    private TicketsUseCases _ticketUseCases;
    private readonly IHubContext<DeviceHub> _hubContext;

    public CountersController(CounterUseCases counterUseCases, TicketsUseCases ticketsUseCases, IHubContext<DeviceHub> hubContext)
    {
      _counterUseCases = counterUseCases;
      _ticketUseCases = ticketsUseCases;
      _hubContext = hubContext;
    }

    /// <summary>
    /// Creates a new service desk.
    /// </summary>
    /// <param name="command">Data of the counter to create.</param>
    /// <returns>The created counter.</returns>
    /// <response code="200">Counter created.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(CounterBaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateCounterCommand command)
    {
      try
      {
        var counter = await _counterUseCases.Create.Handle(command);
        return Ok(counter);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Updates an existing service desk.
    /// </summary>
    /// <param name="id">Identifier of the counter.</param>
    /// <param name="command">Data to update. The body Id is overridden with the one from the path.</param>
    /// <returns>The updated counter.</returns>
    /// <response code="200">Counter updated.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(CounterBaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, UpdateCounterCommand command)
    {
      try
      {
        var commandCopy = command with { Id = id };
        var counter = await _counterUseCases.Update.Handle(commandCopy);
        return Ok(counter);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Deletes a service desk.
    /// </summary>
    /// <param name="id">Identifier of the counter.</param>
    /// <response code="200">Counter deleted.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id)
    {
      try
      {
        await _counterUseCases.Delete.Handle(new DeleteCounterCommand(id));
        return Ok();
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Gets a service desk by its identifier.
    /// </summary>
    /// <param name="id">Identifier of the counter.</param>
    /// <returns>The requested counter.</returns>
    /// <response code="200">Counter found.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CounterBaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get(Guid id)
    {
      try
      {
        var counter = await _counterUseCases.Get.Handle(new GetCounterCommand(id));
        return Ok(counter);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Gets all service desks, optionally filtered by location.
    /// </summary>
    /// <param name="query">Query filters (optional LocationId).</param>
    /// <returns>List of counters.</returns>
    /// <response code="200">List of counters.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(DataResponse<IEnumerable<CounterBaseResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] GetAllCountersCommand query)
    {
      try
      {
        var counters = await _counterUseCases.GetAll.Handle(query);
        return Ok(new DataResponse<IEnumerable<CounterBaseResponse>>(counters));
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Calls the next ticket in the queue for the specified counter and notifies
    /// the group's monitors (SignalR, "TicketCalled" event).
    /// </summary>
    /// <remarks>
    /// If the counter already has a ticket in progress, the same ticket is called again.
    /// If there are no tickets waiting for the counter's queues, returns 200 with null body.
    /// </remarks>
    /// <param name="id">Identifier of the counter making the call.</param>
    /// <param name="request">FlowId used to build the SignalR group.</param>
    /// <returns>The called ticket, or null if there are no tickets waiting.</returns>
    /// <response code="200">Ticket called (may be null).</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpPost("{id:guid}/call-next-ticket")]
    [ProducesResponseType(typeof(CallNextTicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CallNextTicket(Guid id, CallNextTicketRequest request)
    {
      try
      {
        var ticket = await _counterUseCases.CallNextTicket.Handle(new CallNextTicketCommand(id));
        if(ticket==null)
        {
          return new JsonResult(null);
        }
        var counter = await _counterUseCases.Get.Handle(new GetCounterCommand(id));
        var ticketCalled = new TicketCalled(ticket?.TicketId, ticket?.Code, counter.Code);

        //TODO una clase o algo para no escribir esto tan hardcoded.. "ticketcalled" , "locaitonId:type:.".. etc
        var group = $"locationId:{counter.LocationId}:typeDevice:Monitor:flowId:{request.FlowId}";
        await _hubContext.Clients.Group(group).SendAsync("TicketCalled", ticketCalled);

        return Ok(ticket);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Marks a ticket as attended and notifies the group's monitors
    /// (SignalR, "TicketAttended" event).
    /// </summary>
    /// <remarks>
    /// The counter is resolved from the path id (for the SignalR group),
    /// while the command uses the CounterId from the body.
    /// </remarks>
    /// <param name="id">Identifier of the counter that attends.</param>
    /// <param name="request">CounterId, TicketId and FlowId of the operation.</param>
    /// <response code="200">Ticket attended.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpPost("{id:guid}/attend-ticket")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AttendTicket(Guid id, AttendTicketRequest request)
    {
      try
      {
        var command=new AttendTicketCommand(id,request.TicketId);
        await _counterUseCases.AttendTicket.Handle(command);
        var counter = await _counterUseCases.Get.Handle(new GetCounterCommand(id));
        var group = $"locationId:{counter.LocationId}:typeDevice:Monitor:flowId:{request.FlowId}";
        await _hubContext.Clients.Group(group).SendAsync("TicketAttended", new { ticketId = request.TicketId });
        return Ok();
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Cancels a ticket (the customer leaves, closing time, etc.).
    /// </summary>
    /// <remarks>The path id (counter) is not used; the ticket is identified by its TicketId in the body.</remarks>
    /// <param name="id">Identifier of the counter (not used).</param>
    /// <param name="command">TicketId of the ticket to cancel.</param>
    /// <response code="200">Ticket canceled.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpPost("{id:guid}/cancel-ticket")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelTicket(Guid id, CancelTicketRequest command)
    {
      try
      {
        var ticket = await _ticketUseCases.GetTicket.Handle(new GetTicketCommand(command.TicketId));
        if (ticket == null)
          throw new Exception($"No ticket found for {command.TicketId}");
        await _counterUseCases.CancelTicket.Handle(new CancelTicketCommand(command.TicketId,id));
        
        var group = $"locationId:{ticket.LocationId}:typeDevice:Monitor:flowId:{ticket.FlowId}";
        await _hubContext.Clients.Group(group).SendAsync("TicketCancelled", new { ticketId = ticket.Id });
        return Ok();
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Manually calls a ticket/code and notifies the group's monitors
    /// (SignalR, "TicketCalled" event). Flow for manual calls (fishmonger, greengrocer...).
    /// </summary>
    /// <remarks>The path id is not used; counter, location and flow are taken from the body.</remarks>
    /// <param name="request">Code, FlowId, LocationId and CounterId of the manual call.</param>
    /// <response code="200">Ticket manually called.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpPost("{id:guid}/call-manual-ticket")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CallManualTicket([FromBody] CallManualTicketRequest request)
    {
      try
      {
        var counter = await _counterUseCases.Get.Handle(new GetCounterCommand(request.CounterId));
        if (counter == null)
          throw new Exception($"No counter found for {request.CounterId}");
        //TODO una clase o algo para no escribir esto tan hardcoded.. "ticketcalled" , "locaitonId:type:.".. etc
        
        //El codigo de la queue, para ponerla delante del numero
        
        var queues=await _counterUseCases.GetQueues.Handle(new GetQueuesQuery(request.CounterId));
        var code = queues.FirstOrDefault()?.Code;
        var ticketCalled = new TicketCalled(null,$"{code}{request.Code}", counter.Code);
        
        var group = $"locationId:{request.LocationId}:typeDevice:Monitor:flowId:{request.FlowId}";
        await _hubContext.Clients.Group(group).SendAsync("TicketCalled", ticketCalled);
        return Ok();
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Notifies that the service desk is free (Carrefour style) to the group's monitors
    /// (SignalR, "CounterFree" event).
    /// </summary>
    /// <param name="id">Identifier of the counter that becomes free.</param>
    /// <param name="request">FlowId used to build the SignalR group.</param>
    /// <response code="200">Desk marked as free.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpPost("{id:guid}/set-free")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetFree(Guid id, SetFreeRequest request)
    {
      try
      {
        var counter = await _counterUseCases.Get.Handle(new GetCounterCommand(id));
        if (counter == null)
          throw new Exception($"No counter found for {id}");

        var group = $"locationId:{counter.LocationId}:typeDevice:Monitor:flowId:{request.FlowId}";
        await _hubContext.Clients.Group(group).SendAsync("CounterFree", new { counterId = id, counterCode = counter.Code });
        return Ok();
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }
    
    /// <summary>
    /// Transfers a ticket from one queue to another, allowing the customer to change their service type.
    /// THe ticket is put in waiting status in the new queue.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("{id:guid}/transfer-ticket")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TransferTicket(Guid id,TransferTicketRequest request)
    {
      try
      {
        await _counterUseCases.TransferTicket.Handle(new TransferTicketCommand(id, request.TicketId, request.QueueId));
        return Ok();
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }
  }
}
