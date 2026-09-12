using Keues.Application.Common;

namespace Keues.Application.Features.Users.DeleteUser;

public class DeleteUserHandler
{
  private readonly IApplicationDbContext _context;

  public DeleteUserHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task Handle(DeleteUserCommand command)
  {
    var user = await _context.Users.FindAsync(command.Id );
    if (user == null)
    {
      throw new Exception("User not found");
    }

    user.RemovedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
  }
}