using Keues.Application.Common;

namespace Keues.Application.Features.UserGroups.DeleteUserGroup;

public class DeleteUserGroupHandler
{
  private readonly IApplicationDbContext _context;

  public DeleteUserGroupHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task Handle(DeleteUserGroupCommand command)
  {
    var userGroup = await _context.UserGroups.FindAsync(command.Id);

    if (userGroup == null)
    {
      throw new Exception("User group not found");
    }

    userGroup.RemovedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
  }
}