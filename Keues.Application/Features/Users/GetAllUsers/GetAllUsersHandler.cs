using Keues.Application.Common;
using Keues.Application.Features.Users.GetUser;
using Keues.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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

    // SQLite orders with a binary collation, so accented or lowercase names end up
    // out of place (e.g. "Álvaro" after "Z"). Sort in memory with a culture-aware,
    // case-insensitive comparer to get a real alphabetical order.
    var projected = await users
      .Select(u => new GetUserResult(u.Id, u.Name, u.Email, u.LocationId, u.CreatedAt, u.Enabled))
      .ToListAsync();

    var comparer = StringComparer.Create(CultureInfo.GetCultureInfo("es-ES"), ignoreCase: true);
    var orderedQuery = query.SortOrder == Keues.Domain.Enums.SortOrder.Asc
      ? projected.OrderBy(u => u.Name, comparer)
      : projected.OrderByDescending(u => u.Name, comparer);

    var result = orderedQuery
      .Skip((query.Page - 1) * query.Limit)
      .Take(query.Limit)
      .ToList();

    return new GetAllUsersResult(result, query.Page, query.Limit, total, totalPages);
  }
}