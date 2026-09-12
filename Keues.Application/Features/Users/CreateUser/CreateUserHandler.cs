using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Users.CreateUser;

public class CreateUserHandler
{
  private readonly IApplicationDbContext _context;

  public CreateUserHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<CreateUserResult> Handle(CreateUserCommand command)
  {
    var location = await _context.Locations.FindAsync(command.LocationId);
    if (location == null)
      throw new Exception("Location not found");
    
    var email = command.Email.ToLower();
    var password = BCrypt.Net.BCrypt.HashPassword(command.Password);
    var exists = await _context.Users.AnyAsync(x => x.Email == email);
    if (exists)
      throw new Exception("User with this email already exists.");
    var newUser = new Keues.Domain.Entities.User()
    {
      Name = command.Name,
      Email = email,
      Role = Keues.Domain.Enums.Rol.User,
      PasswordHash = password,
      LocationId = command.LocationId,
      
    };
    _context.Users.Add(newUser);
    await _context.SaveChangesAsync();

    return new CreateUserResult(newUser.Id, newUser.Name, newUser.Email);
  }
}