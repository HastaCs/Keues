using Keues.Application.Common;
using Keues.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Users.UpdateUser;

public class UpdateUserHandler
{
  private readonly IApplicationDbContext _context;

  public UpdateUserHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<UpdateUserResult> Handle(UpdateUserCommand command)
  {
    var location = await _context.Locations.FindAsync(command.LocationId);
    if (location == null)
      throw new Exception("Location not found");

    var user = await _context.Users.FindAsync(command.Id);
    if (user == null)
    {
      throw new Exception("User not found");
    }

    if (user.Role == Rol.Admin)
    {
      throw new Exception("You cannot update an admin user");
    }
    var exists = await _context.Users.AnyAsync(x => x.Email == command.Email.ToLower() && x.Id != command.Id);
    if (exists)
    {
      throw new Exception("Email already in use");
    }
    user.LocationId = command.LocationId;
    user.Name = command.Name;
    user.Email = command.Email.ToLower();
    if (!string.IsNullOrEmpty(command.Password))
      user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.Password);

    await _context.SaveChangesAsync();
    return new UpdateUserResult(user.Id, user.Name, user.Email);
  }
}