using Keues.Application.Common;
using Keues.Application.Features.Users.GetUser;
using Keues.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Users.GetAllUsers;

public class GetAllUsersHandler
{
  private readonly IApplicationDbContext _context;

  public GetAllUsersHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<GetAllUsersResult> Handle(GetAllUsersQuery query)
  {
    var users = _context.Users.AsQueryable();

    users = users.Where(u => u.Role != Rol.Admin);
    if (query.LocationId.HasValue)
    {
      users = users.Where(u => u.LocationId == query.LocationId.Value);
    }

    if (!string.IsNullOrEmpty(query.Name))
    {
      users = users.Where(u => u.Name.ToLower().Contains(query.Name.ToLower()));
    }

    if (query.IsActive.HasValue)
    {
      users = users.Where(u => u.Enabled == query.IsActive.Value);
    }


    var total = await users.CountAsync();
    var totalPages = (int)Math.Ceiling((double)total / query.Limit);

    var orderedQuery = query.SortOrder == Keues.Domain.Enums.SortOrder.Asc
      ? users.OrderBy(u => u.Name)
      : users.OrderByDescending(u => u.Name);

    var result = await orderedQuery
      .Skip((query.Page - 1) * query.Limit)
      .Take(query.Limit)
      .Select(u => new GetUserResult(u.Id, u.Name, u.Email, u.LocationId, u.CreatedAt, u.Enabled))
      .ToListAsync();

    return new GetAllUsersResult(result, query.Page, query.Limit, total, totalPages);
  }
}