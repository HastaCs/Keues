using Keues.Application.Common;

namespace Keues.Application.Features.Users.GetUser;

public class GetUserHandler
{
  private readonly IApplicationDbContext _context;
  
  public GetUserHandler(IApplicationDbContext context)
  {
    _context = context;
  }
  
  public async Task<GetUserResult> Handle(GetUserQuery query)
  {
    var user = await _context.Users.FindAsync(query.Id);
    if (user == null)
    {
      throw new Exception("User not found");
    }

    return new GetUserResult(user.Id, user.Name, user.Email, user.LocationId, user.CreatedAt, user.Enabled);
  }
}