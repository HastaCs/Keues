using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Users.HasAdmin;

public class HasAdminHandler
{
  private readonly IApplicationDbContext _context;

  public HasAdminHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<bool> Handle(HasAdminQuery query)
  {
    return await _context.Users.AnyAsync();
  }
}
