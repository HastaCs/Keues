using Keues.Application.Common;

namespace Keues.Application.Features.Users.Me;

public class GetCurrentUserHandler
{
  private readonly IApplicationDbContext _context;

  public GetCurrentUserHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<MeResponse> Handle(MeQuery query)
  {
    var user = await _context.Users.FindAsync(query.UserId);

    if (user == null)
      throw new Exception($"User {query.UserId} not found");

    return new MeResponse(user.Id, user.Name, user.Email, user.Role);
  }
}
