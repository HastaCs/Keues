using Keues.Application.Features.Users.GetUser;

namespace Keues.Application.Features.Users.GetAllUsers;

public record GetAllUsersResult(IEnumerable<GetUserResult> Users, int Page, int Limit, int Total, int TotalPages);