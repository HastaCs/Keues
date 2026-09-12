using Keues.Application.Common;

namespace Keues.Application.Features.Users.EnableDisableUser;

public class EnableDisableUserHandler
{
  private readonly IApplicationDbContext _context;
  
  public EnableDisableUserHandler(IApplicationDbContext context)
  {
    _context = context;
  }
  
  public async Task Handle(EnableDisableUserCommand command)
  {
    var user = await _context.Users.FindAsync(new object?[] { command.Id });
    if (user == null)
    {
      throw new Exception("User not found");
    }

    user.Enabled = command.IsEnabled;
    await _context.SaveChangesAsync();
  }
}