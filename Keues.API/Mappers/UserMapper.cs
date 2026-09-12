using Keues.API.Dtos.Requests.Users;
using Keues.Application.Features.Users.CreateUser;
using Keues.Application.Features.Users.UpdateUser;

namespace Keues.API.Mappers;

public static class UserMapper
{
  public static CreateUserCommand ToCommand(this CreateUserRequest request)
  {
    return new CreateUserCommand(request.Name, request.Email, request.Password?.Trim(), request.LocationId);
  }
  public static UpdateUserCommand ToCommand(this UpdateUserRequest request, Guid id)
  {
    return new UpdateUserCommand(id, request.Name, request.Email, request.Password?.Trim(), request.LocationId);
  }
}