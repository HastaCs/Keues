using Keues.API.Requests.Counters;
using Keues.API.Responses.Counters;
using Keues.Application.Features.Counters;
using Keues.Application.Features.Counters.AttendTicket;
using Keues.Application.Features.Counters.CreateCounter;
using Keues.Application.Features.Counters.GetAllCounters;
using Keues.Application.Features.Counters.UpdateCounter;

namespace Keues.API.Mappers;

public static class CounterMapper
{
  public static CreateCounterCommand ToCommand(this CreateCounterRequest request)
  {
    return new CreateCounterCommand
    {
      Code = request.Code,
      Color = request.Color,
      Name = request.Name,
      Description = request.Description,
      LocationId = request.LocationId,
      Queues = request.Queues,
      AuthorizedUsers = request.AuthorizedUsers,
      AuthorizedUserGroups = request.AuthorizedUserGroups
    };
  }
  
  public static CounterResponse ToResponse(this CounterBaseResult command)
  {
    return new CounterResponse
    {
      Id = command.Id,
      Name = command.Name,
      Code = command.Code,
      Description = command.Description,
      Color = command.Color,
      Queues = command.Queues,
      LocationId = command.LocationId,
      CreatedAt = command.CreatedAt,
      AuthorizedUsers = command.AuthorizedUsers.Select(u => new BasicUserResponse { Id = u }),
      AuthorizedUserGroups = command.AuthorizedUserGroups.Select(g => new BasicUserGroupResponse { Id = g })
    };
  }
  
  public static UpdateCounterCommand ToCommand(this UpdateCounterRequest request,Guid id)
  {
    return new UpdateCounterCommand
    {
      Id = id,
      Code = request.Code,
      Color = request.Color,
      Name = request.Name,
      Description = request.Description,
      LocationId = request.LocationId,
      Queues = request.Queues,
      AuthorizedUsers = request.AuthorizedUsers,
      AuthorizedUserGroups = request.AuthorizedUserGroups
    };
  }
  
  
  public static AttendTicketCommand ToCommand(this AttendTicketRequest request,Guid counterId,Guid? userId)
  {
    return new AttendTicketCommand(counterId, request.TicketId, userId);
  }
  
  public static GetAllCountersCommand ToCommand(this GetAllCountersRequest request)
  {
    return new GetAllCountersCommand
    {
      LocationId = request.LocationId
    };
  }
}