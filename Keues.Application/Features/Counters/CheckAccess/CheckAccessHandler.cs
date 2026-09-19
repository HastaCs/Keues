using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Counters.CheckAccess;

public class CheckAccessHandler
{
  private readonly IApplicationDbContext _context;

  public CheckAccessHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<bool> Handle(CheckAccessQuery request)
  {
    var counterId = request.CounterId;
    var userId = request.UserId;

  var access = await _context.Counters
      .Where(c => c.Id == counterId)
      .Select(c => new
      {
        HasRestrictions = c.AuthorizedUsers.Any() || c.AuthorizedUserGroups.Any(),
        IsAuthorized = userId != null &&
          (c.AuthorizedUsers.Any(u => u.Id == userId && u.Enabled) ||
           c.AuthorizedUserGroups.Any(g => g.Users.Any(u => u.Id == userId && u.Enabled)))
      })
      .FirstOrDefaultAsync();

    if (access == null)
    {
      throw new Exception($"Counter with ID {counterId} not found.");
    }

    return !access.HasRestrictions || access.IsAuthorized;
  }
}